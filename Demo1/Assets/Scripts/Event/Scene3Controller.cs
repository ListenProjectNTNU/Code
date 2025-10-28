using UnityEngine;

public class Scene3Controller : MonoBehaviour, ISceneController
{
    [Header("Scene References")]
    public BossController boss;
    public Transform player;
    public CameraController cameraController; // 指定 Inspector
    [SerializeField] private DoorTrigger battleDoor;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip bossAwakenClip;
    public AudioClip headphoneClip; // 🎧 耳機音效 (請在 Inspector 指定)

    [Header("Dialogue Knots")]
    public TextAsset inkJSON;
    public string knotBattleBefore = "battle_before";
    public string knotBattleAfter = "battle_after";
    private bool hasTriggeredBattleAfter = false; // 防止重複觸發 battle_after

    private DialogueManager dialogueManager;

    private void Start()
    {
        dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager == null) return;

        if (boss != null)
        {
            boss.gameObject.SetActive(false); // 初始隱藏
            boss.enabled = false;             // 暫停 Update 行為
            boss.rb.velocity = Vector2.zero;
        }

        // 取得 LivingEntity 並訂閱死亡事件
        var livingEntity = boss.GetComponent<LivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDeathEvent += HandleMonsterDeath;
        }
        
        // 一進場就進入 battle_before
        if (inkJSON != null)
        {
            dialogueManager.inkJSON = inkJSON;
            dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
        }
    }

    public void HandleTag(string tag)
    {
        switch (tag)
        {
            case "enter_scene3":
                // 進入場景，玩家操作自動禁用
                break;

            case "appear_boss":
                boss.gameObject.SetActive(true);
                boss.enabled = true;
                audioSource.PlayOneShot(bossAwakenClip);

                if (cameraController != null)
                {
                    // 先瞬間跳到 Boss
                    cameraController.transform.position = new Vector3(boss.transform.position.x, boss.transform.position.y, cameraController.transform.position.z);
                    cameraController.SetTarget(boss.transform);
                    Debug.Log("Camera now follows Boss");
                }
                break;
            case "stop_all_for_headphone":
                StopAllAudioForHeadphone();
                CheckBattleEnd();
                break;
        }
    }

    private void HandleMonsterDeath(LivingEntity entity)
    {
        if (hasTriggeredBattleAfter) return;
        hasTriggeredBattleAfter = true;

        Debug.Log("💀 怪物死亡，觸發 battle_after 對話");
        if (dialogueManager != null && inkJSON != null)
        {
            dialogueManager.EnterDialogueModeFromKnot(knotBattleAfter);
        }
    }

    private void StopAllAudioForHeadphone()
    {
        Debug.Log("🛑 停止所有音效，播放耳機音效");

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }

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

    // 可以在未來 trigger 傳送門時呼叫
    public void TriggerPortalDialogue()
    {
        if (dialogueManager != null && inkJSON != null)
        {
            //dialogueManager.EnterDialogueModeFromKnot(knotBeforePortal);
        }
    }
    private void CheckBattleEnd()
    {
        battleDoor.ActivateDoor(); // ✅ 直接開啟門
    }
}
