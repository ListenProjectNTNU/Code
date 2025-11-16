using UnityEngine;
using UnityEngine.Rendering; 
using UnityEngine.Rendering.Universal; 
using UnityEngine.Rendering.PostProcessing;
public class Scene1Controller : MonoBehaviour, ISceneController
{
    public LoopingBackground loopingBG;
    public GameObject senpai; // 學姊物件
    public GameObject player; // 主角物件

    [Header("動畫 Canvas")]
    public Animator animCanva;  // 直接拖 AnimCanva 的 Animator

    [Header("全域 Volume 控制")]
    public GlobalVolumeController globalVolume; 


    [Header("Audio Settings")]
    public AudioSource audioSource; // 指定 AudioSource
    public AudioClip headphoneClip; // 🎧 耳機音效 (請在 Inspector 指定)

    [Header("Ink JSON（起始對話檔）")]
    public TextAsset inkJSON;

    

    void Start()
    {
        Debug.Log("Scene1Controller 啟動，玩家狀態：" + (player != null ? player.activeInHierarchy.ToString() : "player為null"));
        globalVolume.ResetVignette();

        // 確保 Volume 組件已經賦值
        if (globalVolume != null)
        {
            Debug.Log("成功存取 Global Volume。");
        }

        //自動開啟對話
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


    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "start":
                PlayPlayerAnimation("walk");
                break;

            case "corridor_withDoor":
                loopingBG.SwitchToNextBGOpen();
                AudioHeadphone();
                break;

            case "fox_appear":
                globalVolume.FlashWhite();
                senpai.SetActive(true);
                Debug.Log("學姊出現！");
                Debug.Log("主角轉身");
                FlipPlayer(true);
                break;

            case "player_turn":
                Debug.Log("主角轉身");

                break;

            case "player_turnBack":
                FlipPlayer(false); // 主角轉回右邊
                globalVolume?.FlashRed();
                break;

            case "ClassRoom_Start":
                PlayAnimation("ClassRoom_Start");
                globalVolume?.ClassRoom_Start();
                break;

            case "ClassRoom_End":
                PlayAnimation("ClassRoom_End");
                FlipPlayer(true);
                globalVolume?.FlashRed();
                break;

            case "fade_out":
                globalVolume?.Fade_Out();
                break;
        }
    }

    private void PlayPlayerAnimation(string animName)
    {
        if (player == null)
        {
            Debug.LogWarning("⚠️ player 為 null，無法播放動畫：" + animName);
            return;
        }

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play(animName, 0, 0f);
            Debug.Log("🎬 播放玩家動畫：" + animName);
        }
        else
        {
            Debug.LogWarning("⚠️ 找不到玩家的 Animator 組件");
        }
    }
    
    public void playIdle()
    {
        PlayPlayerAnimation("idle");
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