using UnityEngine;
using Ink.Runtime;
using System.Collections.Generic;
using System;

public class InkVariableUpdater : MonoBehaviour
{
    private Story currentStory;
    private Dictionary<string, bool> tempVariables = new Dictionary<string, bool>(); // 暫存變數

    private void Start()
    {
        DialogueManager dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager != null)
        {
            currentStory = dialogueManager.currentStory;
        }
        else
        {
            Debug.LogError("無法找到 DialogueManager！");
        }
    }

    public void UpdateVariable(string variableName, bool value)
    {
        if (currentStory == null)
        {
            // 📝 劇情未開始，先存到暫存區
            tempVariables[variableName] = value;
            //Debug.Log($"{variableName} 尚未能更新，暫存起來！目前暫存區內容：{string.Join(", ", tempVariables)}");
            return;
        }

        // 劇情已開始，直接更新
        currentStory.variablesState[variableName] = value;
        //Debug.Log($"直接更新 Ink 變數：{variableName} = {value}");
    }

    public void ApplyTempVariables()
    {
        //Debug.Log($"ApplyTempVariables() 被呼叫！目前暫存區大小：{tempVariables.Count}");
        if (currentStory == null)
        {
            Debug.LogWarning("❌ ApplyTempVariables() 執行時，currentStory 仍為 null！");
            return;
        }

        foreach (var entry in tempVariables)
        {
            currentStory.variablesState[entry.Key] = entry.Value;
            Debug.Log($"同步暫存變數到 Ink：{entry.Key} = {entry.Value}");
        }
        tempVariables.Clear(); // 清空暫存變數
    }

    public void SetCurrentStory(Story story)
    {
        currentStory = story;
        Debug.Log("currentStory 已成功設定！");
    }
}
