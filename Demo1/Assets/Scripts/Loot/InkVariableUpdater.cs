using UnityEngine;
using Ink.Runtime;

public class InkVariableUpdater : MonoBehaviour
{
    private Story currentStory;

    private void Start()
    {
        DialogueManager dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager != null)
        {
            currentStory = dialogueManager.currentStory;
        }
        else
        {
            Debug.LogError("❌ 無法找到 DialogueManager！");
        }
    }

    public void UpdateVariable(string variableName, bool value)
    {
        if (currentStory == null)
        {
            Debug.LogWarning($"❌ Ink 劇情尚未開始，無法更新變數 {variableName}！");
            return;
        }

        currentStory.variablesState[variableName] = value;
        Debug.Log($"✅ 更新 Ink 變數：{variableName} = {value}");
    }
}