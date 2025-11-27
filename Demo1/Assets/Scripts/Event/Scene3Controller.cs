using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene3Controller : MonoBehaviour, ISceneController
{//
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
    public string knotBattleAfter  = "battle_after";
    private bool hasTriggeredBattleAfter = false; // 防止重複觸發 battle_after

    private DialogueManager dialogueManager;
    private bool deathSubscribed = false;

    [Header("全域 Volume 控制")]
    public GlobalVolumeController globalVolume;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SafeUnsubscribeBossDeath();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 切場後可能引用掉線，延後一幀重綁
        StartCoroutine(BindRefsNextFrame());
    }

    System.Collections.IEnumerator BindRefsNextFrame()
    {
        yield return null; // 等一幀讓 DDOL / Spawner 準備好

        // 補 Player
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // 補 DialogueManager + 指派目前的 SC
        if (!dialogueManager) dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager)
        {
            dialogueManager.currentSceneController = this.gameObject;
            if (inkJSON && !dialogueManager.dialogueIsPlaying)
            {
                dialogueManager.inkJSON = inkJSON;
                dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
            }
        }

        // 相機保險：如果相機需要 follow 玩家且此時沒有 boss，就先綁玩家
        if (cameraController && !boss)
        {
            if (player) cameraController.SetTarget(player);
        }

        // 🧩 確保 Player 的剛體狀態正確，防止跨場後物理異常
        if (player)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                Debug.Log("[Scene3Controller] Player Rigidbody 已重設。");
            }
        }

        // 🧩 若 Boss 存在，將其設為 Kinematic 以免被玩家推走
        if (boss && boss.rb)
        {
            boss.rb.bodyType = RigidbodyType2D.Kinematic;
            boss.rb.simulated = true;
            boss.rb.gravityScale = 0;
            Debug.Log("[Scene3Controller] Boss Rigidbody 設為 Kinematic。");
        }

        // 🧩 層碰撞忽略設定（避免 Player 與 Boss 互推）
        int playerLayer = LayerMask.NameToLayer("Player");
        int bossLayer   = LayerMask.NameToLayer("Enemy");
        if (playerLayer >= 0 && bossLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, bossLayer, true);
            Debug.Log("[Scene3Controller] 已忽略 Player 與 Enemy 層碰撞。");
        }
    }

    private void Start()
    {
        // 視覺效果保險
        if (globalVolume)
            globalVolume.SetVignette();

        // DialogueManager
        dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager)
        {
            dialogueManager.currentSceneController = this.gameObject;
        }

        // Boss 初始狀態（防呆）
        if (boss)
        {
            // 關閉行為，避免未到演出就動作
            SafeDeactivateBoss(boss);

            // 訂閱死亡事件（判空）
            var le = boss.GetComponent<LivingEntity>();
            if (le != null)
            {
                le.OnDeathEvent += HandleMonsterDeath;
                deathSubscribed = true;
            }
        }
        else
        {
            Debug.LogWarning("[Scene3Controller] boss 未指定。");
        }

        // 對話：進場就 battle_before（保險判空）
        if (dialogueManager && inkJSON)
        {
            // 若對話尚未開始才進入，避免重複進入
            if (!dialogueManager.dialogueIsPlaying)
            {
                dialogueManager.inkJSON = inkJSON;
                dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
            }
        }
    }

    public void HandleTag(string tag)
    {
        switch (tag)
        {
            case "enter_scene3":
                // 這裡如果要暫停玩家操作，可在 DialogueManager 內部處理；此處不做以免切場邊緣 NRE
                break;

            case "appear_boss":
                if (!boss)
                {
                    Debug.LogWarning("[Scene3Controller] 無法出現 Boss：boss 參考為空。");
                    return;
                }

                SafeActivateBoss(boss);

                SafePlayOneShot(audioSource, bossAwakenClip);

                // 相機保險
                if (cameraController)
                {
                    // 先把相機跳到 Boss，再跟隨
                    var camPos = cameraController.transform.position;
                    var bpos   = boss.transform.position;
                    cameraController.transform.position = new Vector3(bpos.x, bpos.y, camPos.z);
                    cameraController.SetTarget(boss.transform);
                    Debug.Log("Camera now follows Boss");
                }
                break;

            case "stop_all_for_headphone":
                StopAllAudioForHeadphone();
                CheckBattleEnd();
                break;

            default:
                Debug.Log($"[Scene3Controller] 未識別的 tag：{tag}");
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

        // 死亡之後相機可切回玩家（若存在）
        if (cameraController && player)
            cameraController.SetTarget(player);
    }

    private void StopAllAudioForHeadphone()
    {
        Debug.Log("🛑 停止所有音效，播放耳機音效");
        var allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in allAudioSources)
        {
            if (source) source.Stop();
        }

        SafePlayOneShot(audioSource, headphoneClip);
    }

    // 開門保險
    private void CheckBattleEnd()
    {
        if (battleDoor)
            battleDoor.ActivateDoor();
        else
            Debug.LogWarning("[Scene3Controller] battleDoor 未指定，無法開啟門。");
    }

    // ===== Helper / 防呆工具 =====

    private void SafeActivateBoss(BossController b)
    {
        if (!b) return;

        var go = b.gameObject;
        if (!go.activeSelf) go.SetActive(true);

        // 啟用腳本（保證 Update 會跑）
        if (!b.enabled) b.enabled = true;

        // 保險處理剛體
        if (b.rb)
        {
            b.rb.isKinematic = false;
            b.rb.velocity = Vector2.zero;
        }

        // 確保死亡訂閱存在
        if (!deathSubscribed)
        {
            var le = b.GetComponent<LivingEntity>();
            if (le != null)
            {
                le.OnDeathEvent += HandleMonsterDeath;
                deathSubscribed = true;
            }
        }
    }

    private void SafeDeactivateBoss(BossController b)
    {
        if (!b) return;

        if (b.rb)
        {
            b.rb.velocity = Vector2.zero;
            b.rb.isKinematic = true; // 避免未演出前受到物理影響
        }

        b.enabled = false;
        if (b.gameObject.activeSelf) b.gameObject.SetActive(false);
    }

    private void SafeUnsubscribeBossDeath()
    {
        if (!deathSubscribed || !boss) return;

        var le = boss.GetComponent<LivingEntity>();
        if (le != null)
            le.OnDeathEvent -= HandleMonsterDeath;

        deathSubscribed = false;
    }

    private void SafePlayOneShot(AudioSource src, AudioClip clip)
    {
        if (src != null && clip != null)
            src.PlayOneShot(clip);
        else if (clip == null)
            Debug.LogWarning("[Scene3Controller] 要播放的 AudioClip 為空。");
        else
            Debug.LogWarning("[Scene3Controller] AudioSource 未指定，無法播放音效。");
    }
    public void TriggerPortalDialogue()
    {
        // 依你的需求觸發傳送門前對話；沒有就先放空也可
        if (dialogueManager != null && inkJSON != null)
        {
            const string knotBeforePortal = "before_portal"; // 或用你的變數
            dialogueManager.EnterDialogueModeFromKnot(knotBeforePortal);
        }
        else
        {
            Debug.LogWarning("[Scene3Controller] TriggerPortalDialogue 無法執行：dialogueManager 或 inkJSON 未設定。");
        }
    }
}
