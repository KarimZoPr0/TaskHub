using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace KarimZoPr0.Tasks
{
    [Flags]
    public enum TaskType
    {
        Art = 1, 
        Audio = 2, 
        Script = 4, 
        UI = 8, 
    }

    public enum NotificationType
    {
        Success,
        Warning,
        Error
    }
    public class TaskListEditor : EditorWindow
    {
        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Fields
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */

        private const string Path = "Packages/com.karimzopro.taskhub/Editor/TaskList/EditorWindow/";

        private VisualElement container;
        private ObjectField savedTasksObjectField;
        private TextField taskTextField;
        private Button addTaskButton;
        private ScrollView taskListScrollView;
        private Button saveProgressButton;
        private ProgressBar taskProgressBar;
        private ToolbarSearchField searchBox;
        private Label notificationLabel;
        private EnumFlagsField taskType;

        private TaskListSO taskListSO;

        private int currentStatus;
        private readonly string[] taskStatusStyle = 
        {
            "To-Do",
            "In-Progress",
            "todo",
            "inprogress",
        };

        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Unity Methods
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */

        public void CreateGUI()
        {
            container = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path + "TaskListEditor.uxml");
            container.Add(visualTree.Instantiate());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(Path + "TaskListEditor.uss");
            container.styleSheets.Add(styleSheet);
            
            savedTasksObjectField = container.Q<ObjectField>("savedTasksObjectField");
            savedTasksObjectField.objectType = typeof(TaskListSO);
            savedTasksObjectField.RegisterValueChangedCallback(UpdateSavedTasks);
            
            taskTextField = container.Q<TextField>("taskText");
            taskTextField.RegisterCallback<KeyDownEvent>(AddTask);

            addTaskButton = container.Q<Button>("addTaskButton");
            addTaskButton.clicked += AddTask;

            taskListScrollView = container.Q<ScrollView>("taskList");

            saveProgressButton = container.Q<Button>("saveProgressButton");
            saveProgressButton.clicked += SaveProgress;

            taskProgressBar = container.Q<ProgressBar>("taskProgressBar");

            searchBox = container.Q<ToolbarSearchField>("searchBox");
            searchBox.RegisterValueChangedCallback(OnSearchTextChanged);

            notificationLabel = container.Q<Label>("notificationLabel");

            taskType = container.Q<EnumFlagsField>("taskType");
            taskType.RegisterValueChangedCallback(OnTaskTypeChanged);

            if (taskListSO)
            {
                savedTasksObjectField.value = taskListSO;
            }
            
            LoadTasks();
        }

        // private void OnDisable()
        // {
        //     saveProgressButton.clicked -= SaveProgress;
        //     addTaskButton.clicked -= AddTask;
        //     taskTextField.UnregisterCallback<KeyDownEvent>(AddTask);
        // }


        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Methods
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */

        [MenuItem("Tools/Task List")]
        public static void ShowWindow()
        {
            var window = GetWindow<TaskListEditor>();
            window.titleContent = new GUIContent("Task List");
        }
        
        private void UpdateSavedTasks(ChangeEvent<Object> evt) => LoadTasks();
        
        private void LoadTasks()
        {
            taskListSO = savedTasksObjectField.value as TaskListSO;
            if (taskListSO == null)
            {
                taskListScrollView.Clear();
                UpdateNotification("Please load a task list to continue.", NotificationType.Error);
                return;
            }
            
            var tasksContents = taskListSO.GetTasksContents();
            var tasksCompletions = taskListSO.GetTasksStatus();
            for (var i = 0; i < tasksContents.Count; i++)
            {
                taskListScrollView.Add(CreateTask(tasksContents[i], tasksCompletions[i]));
            }

            UpdateScrollView();
            UpdateProgress();
            UpdateNotification($"{taskListSO.name} Successfully loaded.", NotificationType.Success);
        }
        
        private void AddTask()
        {
            if (string.IsNullOrEmpty(taskTextField.text))
            {
                UpdateNotification("Please name your Task!", NotificationType.Error);
            }
            if (taskListSO == null)
            {
                UpdateNotification("Please load a task list to continue.", NotificationType.Error);
                return;
            }
            
            if (string.IsNullOrEmpty(taskTextField.value)) return;
            
            var selectedTaskType = (TaskType)taskType.value;
            if (!selectedTaskType.IsSingleFlag())
            {
                UpdateNotification("Please select only one task type!", NotificationType.Error);
                return;
            }
            
            var taskTypePrefix = Enum.GetName(typeof(TaskType), selectedTaskType);
            var taskText = taskTextField.text.Insert(0, taskTypePrefix + ": ");
            
            taskListScrollView.Add(CreateTask(taskText, TaskStatus.Todo));
            SaveTask(taskText);
            
            taskTextField.value = string.Empty;
            taskTextField.Focus();
            
            UpdateScrollView();
            UpdateNotification("Task Added Successfully.", NotificationType.Success);
        }

        private void AddTask(KeyDownEvent evt)
        {
            if (!Event.current.Equals(Event.KeyboardEvent("Return"))) return;
            AddTask();
        }

        private Toggle CreateTask(string taskText, TaskStatus taskStatus)
        {
            var taskItem = new Toggle();
            taskItem.text = taskText;
            taskItem.value = taskStatus == TaskStatus.Completed;
            
            taskItem.AddToClassList("toggle");

            var taskStatusLabel = new Label();
            
            UpdateTaskStatusUI(taskStatus, ref taskStatusLabel);
            
            var taskIndex = taskListSO.GetIndexOfTask(taskItem.text);
            

            taskItem.RegisterValueChangedCallback(evt =>
            {
                var chosenTaskStatus = taskItem.value ? TaskStatus.Completed : TaskStatus.Todo;
                taskListSO.SetTaskStatus(taskIndex, chosenTaskStatus);
                UpdateTaskStatusUI(chosenTaskStatus, ref taskStatusLabel);
                UpdateProgress();
            });
            

            taskItem.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                var chosenTaskStatus = taskListSO.GetTasksStatus()[taskIndex];
                
                if (chosenTaskStatus == TaskStatus.Completed) return;
                var nextTaskStatus = chosenTaskStatus == TaskStatus.Todo ? TaskStatus.InProgress : TaskStatus.Todo;
                taskListSO.SetTaskStatus(taskIndex, nextTaskStatus);
                UpdateTaskStatusUI(nextTaskStatus, ref taskStatusLabel);
            });

            var deleteButton = new Button();
            deleteButton.text = string.Empty;
            deleteButton.AddToClassList("delete");
            deleteButton.clicked += () => { OnDeleteTask(deleteButton); };

            taskItem.Add(taskStatusLabel);
            taskItem.Add(deleteButton);

            return taskItem;
        }

        private static void UpdateTaskStatusUI(TaskStatus taskStatus, ref Label taskStatusLabel)
        {
            taskStatusLabel.ClearClassList();
            switch (taskStatus)
            {
                case TaskStatus.Todo:
                    taskStatusLabel.text = "To-Do";
                    taskStatusLabel.AddToClassList("todo");
                    break;
                case TaskStatus.InProgress:
                    taskStatusLabel.text = "In-Progress";
                    taskStatusLabel.AddToClassList("inprogress");
                    break;
                case TaskStatus.Completed:
                    taskStatusLabel.text = "Completed";
                    taskStatusLabel.AddToClassList("completed");
                    break;
            }
        }

        private void OnDeleteTask(VisualElement deleteButton)
        {
            if (deleteButton.parent is not Toggle task) return;
            var index = taskListSO.GetIndexOfTask(task.text);
            if (index < 0) return;

            taskListSO.DeleteTask(index);
            taskListScrollView.RemoveAt(index);
            ReloadTaskList();
            
            UpdateNotification("Task Deleted Successfully.", NotificationType.Success);
        }

        private void SaveTask(string task)
        {
            if (taskListSO == null) return;
            taskListSO.AddTask(task);
            ReloadTaskList();
        }

        private void SaveProgress()
        {
            if (taskListSO == null) return;
            
            List<string> tasks = new();
            
            foreach (var visualElement in taskListScrollView.Children())
            {
                if (visualElement is Toggle { value: false } toggle)
                {
                    tasks.Add(toggle.text);
                }
                
            }

            taskListSO.AddTasks(tasks);
            LoadTasks();
            ReloadTaskList();
            UpdateNotification("Save successful.", NotificationType.Success);
        }
        
        private void ReloadTaskList()
        {
            UpdateProgress();
            
            EditorUtility.SetDirty(taskListSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void UpdateProgress()
        {
            var count = taskListScrollView.childCount;
            var completed = taskListSO.GetTasksStatus().Count(status => status == TaskStatus.Completed);
            taskProgressBar.value = count > 0 ? completed / (float)count : 1;
            taskProgressBar.title = $"{taskProgressBar.value * 100:F0} %";
        }
        
        private void UpdateProgress(ChangeEvent<bool> evt)
        {
            UpdateProgress();
            UpdateNotification("Progress updated. Don't forget to save!", NotificationType.Warning);
        }

        private void OnSearchTextChanged(ChangeEvent<string> changeEvent)
        {
            var searchText = changeEvent.newValue.ToLower();
            var temp = taskListScrollView;

            foreach (var visualElement in taskListScrollView.Children())
            {
                if (visualElement is Toggle task)
                {
                    var taskText = task.text.ToLower();
                    if (!string.IsNullOrEmpty(searchText) && taskText.Contains(searchText))
                    {
                        task.AddToClassList("highlight");
                    }
                    else
                    {
                        task.RemoveFromClassList("highlight");
                    }
                }
            }
        }

        private void UpdateNotification(string text, NotificationType notificationType)
        {
            notificationLabel.text = text;
            notificationLabel.ClearClassList();

            switch (notificationType)
            {
                case NotificationType.Success:
                    notificationLabel.AddToClassList("success");
                    break;
                case NotificationType.Warning:
                    notificationLabel.AddToClassList("warning");
                    break;
                case NotificationType.Error:
                    notificationLabel.AddToClassList("error");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            notificationLabel.AddToClassList("");
        }

        private void OnTaskTypeChanged(ChangeEvent<Enum> evt)
        {
            UpdateScrollView();
        }

        private void UpdateScrollView()
        {
            taskListScrollView.Clear();
            var selectedTaskType = (TaskType)taskType.value;

            if (taskListSO == null) return;
            var tasksContents = taskListSO.GetTasksContents();
            var tasksCompletions = taskListSO.GetTasksStatus();

            for (var i = 0; i < tasksContents.Count; i++)
            {
                var taskTypePrefix = tasksContents[i].Split(':').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(taskTypePrefix) && Enum.IsDefined(typeof(TaskType), taskTypePrefix))
                {
                    var taskTypeOfTask = (TaskType)Enum.Parse(typeof(TaskType), taskTypePrefix);
                    if ((selectedTaskType & taskTypeOfTask) != 0)
                    {
                        taskListScrollView.Add(CreateTask(tasksContents[i], tasksCompletions[i]));
                    }
                }
            }

            SortTaskList();
            UpdateProgress();
            
            var notification = selectedTaskType == 0 ? "Nothing" : selectedTaskType < 0 ? "Everything" : selectedTaskType.ToString();
            UpdateNotification($"Tasks filtered by {notification}.", NotificationType.Success);
        }

        private void SortTaskList()
        {
            var sortedTasks = taskListScrollView.Children()
                .OrderBy(child => child is Toggle toggle ? toggle.text : null, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var task in sortedTasks)
            {
                taskListScrollView.Add(task);
            }
        }
    }

    public static class EnumFlagExtension
    {
        public static bool IsSingleFlag(this TaskType taskType) => taskType != 0 && (taskType & (taskType - 1)) == 0;
    }
}