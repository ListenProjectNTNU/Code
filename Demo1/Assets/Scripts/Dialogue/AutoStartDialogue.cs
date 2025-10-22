using UnityEngine;

public class AutoStartDialogue : MonoBehaviour
{
    [Header("Ink JSON（起始對話檔）")]
    public TextAsset inkJSON;

    private void Start()
    {
        // 確保 DialogueManager 存在
        if (DialogueManager.GetInstance() == null)
        {
            Debug.LogError("❌ DialogueManager 尚未在場景中建立！");
            return;
        }

        if (inkJSON == null)
        {
            Debug.LogError("❌ 尚未指定 Ink JSON 檔案！");
            return;
        }

        // 停用玩家控制，並進入對話
        Debug.Log("🎬 遊戲開始，自動進入對話");
        DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
    }
}
