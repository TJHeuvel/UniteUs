using System;
using UnityEngine;
using UnityEngine.UI;

class HUD_Task_Overview : MonoBehaviour 
{
    [SerializeField] private Slider sldrCompletedTasks;
    void OnEnable()
    {
        TaskManager.Instance.TotalTaskCount.OnValueChanged += onTasksChanged;
        TaskManager.Instance.CompletedTaskCount.OnValueChanged += onTasksChanged;
    }
    void OnDisable()
    {
        TaskManager.Instance.TotalTaskCount.OnValueChanged -= onTasksChanged;
        TaskManager.Instance.CompletedTaskCount.OnValueChanged -= onTasksChanged;
    }

    private void onTasksChanged(int previousValue, int newValue)
    {
        sldrCompletedTasks.maxValue = TaskManager.Instance.TotalTaskCount.Value;
        sldrCompletedTasks.value = TaskManager.Instance.CompletedTaskCount.Value;
    }
}