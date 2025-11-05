using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoStartDialogue : MonoBehaviour
{
    [Header("Ink JSON（起始對話檔）")]
    public TextAsset inkJSON;

    private void Start()
    {
        // 若是競技場場景，直接略過
        if (SceneManager.GetActiveScene().name == "BATTLE")
        {
            Debug.Log("🏟️ 競技場場景，跳過 AutoStartDialogue");
            return;
        }

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

        Debug.Log("🎬 遊戲開始，自動進入對話");
        DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
    }
}
