using UnityEngine;
using Ink.Runtime;
using System.Collections.Generic;

public class InkVariableUpdater : MonoBehaviour
{
    private Story currentStory;
    private Dictionary<string, bool> tempVariables = new Dictionary<string, bool>();

    private static InkVariableUpdater instance;
    public static InkVariableUpdater Instance => instance;

    private void Awake()
    {
        // 單例保護
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        DialogueManager dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager != null)
        {
            currentStory = dialogueManager.currentStory;
            Debug.Log("✅ InkVariableUpdater 已連接到 DialogueManager");
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到 DialogueManager，Ink 變數將暫存直到劇情開始");
        }
    }

    public void SetCurrentStory(Story story)
    {
        currentStory = story;
        Debug.Log("📘 currentStory 已設定");

        // 同步暫存變數
        ApplyTempVariables();
    }

    public void UpdateVariable(string variableName, bool value)
    {
        if (currentStory == null)
        {
            // 劇情未開始 → 暫存
            tempVariables[variableName] = value;
            Debug.Log($"🕐 劇情尚未啟動，暫存 Ink 變數 {variableName} = {value}");
            return;
        }

        currentStory.variablesState[variableName] = value;
        Debug.Log($"✅ 立即更新 Ink 變數：{variableName} = {value}");
    }

    public void ApplyTempVariables()
    {
        if (currentStory == null) return;

        foreach (var entry in tempVariables)
        {
            currentStory.variablesState[entry.Key] = entry.Value;
            Debug.Log($"🔄 同步暫存變數 → Ink：{entry.Key} = {entry.Value}");
        }

        tempVariables.Clear();
    }

    public void ApplyInventoryVariables(List<string> collectedItems)
    {
        foreach (string item in collectedItems)
        {
            string variableName = $"has_{item}";
            UpdateVariable(variableName, true);
        }
    }
}
