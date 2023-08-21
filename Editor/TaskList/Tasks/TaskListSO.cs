using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarimZoPr0.Tasks
{
    [Serializable]
    public struct TaskList
    {
        public List<string> contents;
        public List<TaskStatus> status;
    }

    public enum TaskStatus
    {
        Todo,
        InProgress,
        Completed
    }
    
    [CreateAssetMenu(fileName = "Task List", menuName = "New Task List")]
    public class TaskListSO : ScriptableObject
    {
        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Fields
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */

        [SerializeField] private TaskList taskList;
        
        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Properties
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */
        
        

        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Events
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */
        
        

        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Unity Methods
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */



        /* ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
        |   Methods
        ───────────────────────────────────────────────────────────────────────────────────────────────────────────── */

        public List<string> GetTasksContents() => taskList.contents;
        public List<TaskStatus> GetTasksStatus() => taskList.status;

        public void AddTasks(List<string> savedTasks)
        {
            taskList.contents.Clear();
            taskList.contents = savedTasks;
        }
        
        public void AddTask(string savedTask)
        {
            taskList.contents.Add(savedTask);
            taskList.status.Add(TaskStatus.Todo);
        }

        public void DeleteTask(int task)
        {
            taskList.contents.RemoveAt(task);
            taskList.status.RemoveAt(task);
        }

        public int GetIndexOfTask(string taskText) => taskList.contents.IndexOf(taskText);

        public void SetTaskStatus(int index, TaskStatus status) => taskList.status[index] = status;

        public TaskList GetTaskList() => taskList;
    }
}
