using UnityEngine;

public class Scene4Controller : MonoBehaviour, ISceneController

{
    [Header("Ink JSON（起始對話檔）")]
    public TextAsset inkJSON;

    public GameObject senpai; // 學姊物件
    public GameObject player; // 主角物件

    [Header("動畫 Canvas")]
    public Animator animCanva;  // 直接拖 AnimCanva 的 Animator

     [Header("全域 Volume 控制")]
    public GlobalVolumeController globalVolume; 

    [Header("Audio Settings")]
    public AudioSource audioSource; // 指定 AudioSource
    public AudioClip headphoneClip; // 🎧 耳機音效 (請在 Inspector 指定)

    void Start()
    {
        Debug.Log("Scene1Controller 啟動，玩家狀態：" + (player != null ? player.activeInHierarchy.ToString() : "player為null"));
        globalVolume.ResetVignette();

        // **修改點：禁用整個 Canvas 物件**
        if (animCanva != null)
        {
            // 取得 Animator 所在的 GameObject 並禁用它
            animCanva.gameObject.SetActive(false); 
        }
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


    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "corridor_withDoor":
                AudioHeadphone();
                break;

            case "fox_appear":
                PlayAnimation("Flash_White");
                senpai.SetActive(true);
                Debug.Log("學姊出現！");
                Debug.Log("主角轉身");
                FlipPlayer(true);
                break;

            case "player_turn":
                Debug.Log("主角轉身");
                FlipPlayer(true);
                break;

            case "player_turnBack":
                FlipPlayer(false); // 主角轉回右邊
                PlayAnimation("Flash_Red");
                break;

            case "SetBlur":
                globalVolume.SetBlur();
                break;

            case "ResetBlur":
                globalVolume.ResetBlur();
                break;
        }
    }
    // 🔹 統一播放動畫函式
    private void PlayAnimation(string clipName)
    {
        if (animCanva != null)
        {
            animCanva.Play(clipName, 0, 0f); // 從頭播放
            Debug.Log("播放動畫: " + clipName);
        }
        else
        {
            Debug.LogWarning("animCanva 尚未設定：" + clipName);
        }
    }
    
   public void PlayEndAnimation()
    {
        if(animCanva != null)
        {
            // **修改點：啟用整個 Canvas 物件**
            animCanva.gameObject.SetActive(true); // 必須先啟用物件才能播放動畫
            animCanva.Play("theEnd_Start"); 
            
            Debug.Log("播放動畫: theEnd_Start");
        }
    }

    private void AudioHeadphone()
    {
        if (audioSource != null && headphoneClip != null)
        {
            audioSource.PlayOneShot(headphoneClip);
            Debug.Log("🎧 播放耳機音效");
        }
        else
        {
            Debug.LogWarning("⚠️ 耳機音效未設定或 AudioSource 為空");
        }
    }

    void FlipPlayer(bool faceLeft) //之後看要不要整理playerController裡
    {
        Vector3 scale = player.transform.localScale;
        if (faceLeft)
            scale.x = Mathf.Abs(scale.x) * -1; // 左
        else
            scale.x = Mathf.Abs(scale.x);      // 右
        player.transform.localScale = scale;
    }

    public void TriggerPortalDialogue()
    {
        // 這個場景沒有傳送門對話功能，所以留空
        Debug.Log("Scene1Controller：TriggerPortalDialogue() 被呼叫，但此場景無需處理。");
    }
}