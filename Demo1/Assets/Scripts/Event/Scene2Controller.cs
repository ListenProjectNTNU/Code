using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene2Controller : MonoBehaviour, ISceneController
{
    public Transform monster;
    public PlayerController pc;       // ← 玩家 PlayerController
    public string animatorLayerName = "Base Layer";  // 欲啟用的 Animator Layer

    [Header("Monster Approach Anim")]
    public float moveDistance = 2f;
    public float moveSpeed = 0.5f;
    public Transform monsterTargetPoint;
    public Transform playerTargetPoint;
    private bool isRunningEvent = false;
    private Coroutine approachRoutine;     // ← 追蹤協程，方便中途停止

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip monsterWhisperClip;
    public AudioClip headphoneClip;

    [Header("Dialogue Knots")]
    public TextAsset inkJSON;
    public string knotBattleBefore = "battle_before";
    public string knotBattleAfter = "battle_after";
    public string knotBeforePortal = "before_portal";

    private DialogueManager dialogueManager;
    private bool hasTriggeredBattleAfter = false;

    private enemy_cow monsterScript;
    private Animator monsterAnim;          // ← 快取 Animator
    private Animator playerAnim;           // ← 快取 Animator

    [Header("全域 Volume 控制")]
    public GlobalVolumeController globalVolume;

    void Start()
    {
        if (globalVolume) globalVolume.SetVignette();

        // 如果玩家已經存在，直接抓
        if (pc == null && PlayerController.Instance != null)
        {
            HandlePlayerReady(PlayerController.Instance);
        }
        
        // 取得怪物腳本與 Animator（盡早快取）
        if (monster)
        {
            monsterScript = monster.GetComponent<enemy_cow>();
            monster.TryGetComponent(out monsterAnim);

            if (monsterScript != null)
            {
                monsterScript.isActive = false;
                monsterScript.controlledBySC = true;
                monsterScript.SetCanMove(false);
            }

            if (monsterAnim) monsterAnim.Play("idle");

            var livingEntity = monster.GetComponent<LivingEntity>();
            if (livingEntity != null)
                livingEntity.OnDeathEvent += HandleMonsterDeath;
        }
        else
        {
            Debug.LogWarning("[Scene2Controller] monster 未指定。");
        }

        // 設定玩家使用指定的 Animator Layer
        if (pc != null)
        {
            pc.ActiveLayerName = animatorLayerName;
        }
        else
        {
            Debug.LogWarning("PlayerController 未指定，也找不到 Instance！");
        }

        // 快取玩家 Animator
        if (pc != null)
        {
            pc.TryGetComponent(out playerAnim);
            pc.enabled = false;
            pc.FaceLeft();
        }

        dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager == null)
            Debug.LogError("❌ DialogueManager 尚未建立於場景中！");

        if (dialogueManager != null && inkJSON != null)
        {
            Debug.Log("🎬 自動進入 battle_before 對話");
            dialogueManager.inkJSON = inkJSON;
            dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
        }
    }

    void OnEnable()
    {
        PlayerController.OnPlayerReady += HandlePlayerReady;
    }

    void OnDisable()
    {
        // 解除訂閱，避免關卡切換殘留
        if (monster)
        {
            var le = monster.GetComponent<LivingEntity>();
            if (le != null) le.OnDeathEvent -= HandleMonsterDeath;
        }

        PlayerController.OnPlayerReady -= HandlePlayerReady;
    }

    private void HandlePlayerReady(PlayerController player)
    {
        // 🔹 同步 PlayerController
        pc = player;

        // 快取 Animator
        if (player.TryGetComponent(out Animator anim))
            playerAnim = anim;

        pc.enabled = false;
        pc.FaceLeft();
    }

    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "Fade_In":
                if (globalVolume) globalVolume.Fade_In();
                break;

            case "monster_whisper":
                PlaySceneAudio(tagValue);
                break;

            case "monster_approach":
                if (!isRunningEvent && monster && pc != null)
                    approachRoutine = StartCoroutine(MonsterApproachEvent());
                break;

            case "stop_all_for_headphone":
                StopAllAudioForHeadphone();
                break;

            case "resume_monster_movement":
                if (monsterScript != null)
                {
                    monsterScript.SetCanMove(true);
                    Debug.Log("🔹 SC 指令 → 怪物可以開始移動");
                }
                break;

            case "pause_monster_movement":
                if (monsterScript != null)
                {
                    monsterScript.SetCanMove(false);
                    Debug.Log("🔹 SC 指令 → 怪物暫停移動");
                }
                break;
        }
    }

    private void PlaySceneAudio(string sceneTag)
    {
        AudioClip clipToPlay = null;
        if (sceneTag == "monster_whisper") clipToPlay = monsterWhisperClip;

        if (clipToPlay != null && audioSource != null)
            audioSource.PlayOneShot(clipToPlay);
        else
            Debug.LogWarning($"⚠️ 無法播放音效: {sceneTag}，請檢查 AudioSource 或 AudioClip 是否指定");
    }

    private void StopAllAudioForHeadphone()
    {
        Debug.Log("🛑 停止所有音效，播放耳機音效");

        foreach (var source in FindObjectsOfType<AudioSource>())
            source.Stop();

        if (audioSource != null && headphoneClip != null)
            audioSource.PlayOneShot(headphoneClip);
        else
            Debug.LogWarning("⚠️ 耳機音效未設定或 AudioSource 為空");
    }

    private IEnumerator MonsterApproachEvent()
    {
        isRunningEvent = true;
        try
        {
            // —— 任何時間點都可能被刪除，先檢查 —— 
            if (!monster || pc == null) yield break;

            // 快取起始位置（避免多次取值）
            Vector3 monsterStart = monster.position;
            Vector3 playerStart  = pc.transform.position;

            float monsterStartX  = monsterStart.x;
            float monsterTargetX = monsterTargetPoint ? monsterTargetPoint.position.x : monsterStart.x + moveDistance;
            float playerStartX   = playerStart.x;
            float playerTargetX  = playerTargetPoint ? playerTargetPoint.position.x : playerStart.x - moveDistance;

            // 播放動畫（使用已快取的 Animator，且判空）
            if (monsterAnim) monsterAnim.Play("run");
            if (playerAnim)  playerAnim.Play("walk");

            float duration = 2f;
            float interval = 0.1f;
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                // 每一輪都檢查是否還存在
                if (!monster || pc == null) yield break;

                elapsed += interval;
                float t = Mathf.Clamp01(elapsed / duration);

                float newMonsterX = Mathf.Lerp(monsterStartX, monsterTargetX, t);
                float newPlayerX  = Mathf.Lerp(playerStartX,  playerTargetX,  t);

                // 設定位置前再判一次
                if (monster) monster.position = new Vector3(newMonsterX, monsterStart.y, monsterStart.z);
                if (pc != null) pc.transform.position  = new Vector3(newPlayerX, playerStart.y, playerStart.z);

                yield return new WaitForSeconds(interval);
            }

            // 最終落位（仍需判空）
            if (monster) monster.position = new Vector3(monsterTargetX, monsterStart.y, monsterStart.z);
            if (pc != null) pc.transform.position  = new Vector3(playerTargetX, playerStart.y, playerStart.z);

            if (monsterAnim) monsterAnim.Play("idle");
            if (playerAnim)  playerAnim.Play("idle");
        }
        finally
        {
            isRunningEvent = false; // 確保狀態回復
        }
    }

    // LivingEntity 的死亡事件
    private void HandleMonsterDeath(LivingEntity entity)
    {
        // 一旦怪物死了，立刻停止接近事件，避免協程再碰已銷毀的 Transform
        if (approachRoutine != null)
        {
            StopCoroutine(approachRoutine);
            approachRoutine = null;
        }
        isRunningEvent = false;

        if (hasTriggeredBattleAfter) return;
        hasTriggeredBattleAfter = true;

        Debug.Log("💀 怪物死亡，觸發 battle_after 對話");
        if (dialogueManager != null && inkJSON != null)
            dialogueManager.EnterDialogueModeFromKnot(knotBattleAfter);
    }

    public void TriggerPortalDialogue()
    {
        if (dialogueManager != null && inkJSON != null)
            dialogueManager.EnterDialogueModeFromKnot(knotBeforePortal);
    }
}
