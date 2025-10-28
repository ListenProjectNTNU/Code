using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene2Controller : MonoBehaviour, ISceneController
{
    public Transform monster;
    public Transform player;

    [Header("Monster Approach Anim")]
    public float moveDistance = 2f;
    public float moveSpeed = 0.5f;
    public Transform monsterTargetPoint;
    public Transform playerTargetPoint;
    private bool isRunningEvent = false;

    [Header("Audio Settings")]
    public AudioSource audioSource; // 指定 AudioSource
    public AudioClip monsterWhisperClip; // 怪物竊語
    public AudioClip headphoneClip; // 🎧 耳機音效 (請在 Inspector 指定)

    [Header("Dialogue Knots")]
    public TextAsset inkJSON; // 指向整個 Ink 檔案
    public string knotBattleBefore = "battle_before";
    public string knotBattleAfter = "battle_after";
    public string knotBeforePortal = "before_portal";

    private DialogueManager dialogueManager;
    private bool hasTriggeredBattleAfter = false; // 防止重複觸發 battle_after

    private enemy_cow monsterScript;

    [Header("全域 Volume 控制")]
    public GlobalVolumeController globalVolume; 

    private void Start()
    {
        globalVolume.SetVignette();
        // 🔹 取得怪物腳本，初始化 SC 控制
        monsterScript = monster.GetComponent<enemy_cow>();
        if (monsterScript != null)
        {
            monsterScript.isActive = false;       // SC 控制怪物不受 enemy_cow 自由模式控制
            monsterScript.controlledBySC = true;  // 🔹 標記由 SC 完全控制
            monsterScript.SetCanMove(false);      // 🔹 SC 控制開始時暫停移動
        }

        monster.GetComponent<Animator>().Play("idle");

        // 取得 LivingEntity 並訂閱死亡事件
        var livingEntity = monster.GetComponent<LivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDeathEvent += HandleMonsterDeath; 
        }

        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<PlayerController>().FaceLeft();

        // 取得 DialogueManager
        dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager == null)
            Debug.LogError("❌ DialogueManager 尚未建立於場景中！");

        // 自動播放 battle_before
        if (dialogueManager != null && inkJSON != null)
        {
            Debug.Log("🎬 自動進入 battle_before 對話");
            dialogueManager.inkJSON = inkJSON; // 指定 Ink JSON
            dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
        }
    }

    // SC 原本的 tag 處理
    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "Fade_In":
                globalVolume.Fade_In();
                break;
            case "monster_whisper":
                PlaySceneAudio(tagValue);
                break;

            case "monster_approach":
                if (!isRunningEvent)
                    StartCoroutine(MonsterApproachEvent());
                break;

            case "stop_all_for_headphone":
                StopAllAudioForHeadphone();
                break;

            case "resume_monster_movement": // 🔹 新增 SC tag：允許怪物移動
                if (monsterScript != null)
                {
                    monsterScript.SetCanMove(true);
                    Debug.Log("🔹 SC 指令 → 怪物可以開始移動");
                }
                break;

            case "pause_monster_movement": // 🔹 新增 SC tag：暫停怪物移動
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
        switch (sceneTag)
        {
            case "monster_whisper":
                clipToPlay = monsterWhisperClip;
                break;
        }

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
            Debug.Log($"🎵 播放音效: {sceneTag}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 無法播放音效: {sceneTag}，請檢查 AudioSource 或 AudioClip 是否指定");
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

    private IEnumerator MonsterApproachEvent()
    {
        isRunningEvent = true;
        Debug.Log("怪物移動");

        Animator monsterAnim = monster.GetComponent<Animator>();
        monsterAnim.Play("run");

        Animator playerAnim = player.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.Play("walk");

        float duration = 2f;
        float interval = 0.1f;
        float elapsed = 0f;

        Vector3 monsterStart = monster.position;
        Vector3 playerStart = player.position;

        float monsterStartX = monsterStart.x;
        float monsterTargetX = monsterTargetPoint.position.x;
        float playerStartX = playerStart.x;
        float playerTargetX = playerTargetPoint.position.x;

        while (elapsed < duration)
        {
            elapsed += interval;
            float t = elapsed / duration;
            float newMonsterX = Mathf.Lerp(monsterStartX, monsterTargetX, t);
            float newPlayerX = Mathf.Lerp(playerStartX, playerTargetX, t);

            monster.position = new Vector3(newMonsterX, monsterStart.y, monsterStart.z);
            player.position = new Vector3(newPlayerX, playerStart.y, playerStart.z);

            yield return new WaitForSeconds(interval);
        }

        monster.position = new Vector3(monsterTargetX, monsterStart.y, monsterStart.z);
        player.position = new Vector3(playerTargetX, playerStart.y, playerStart.z);

        monsterAnim.Play("idle");
        if (playerAnim != null) playerAnim.Play("idle");

        // 🔹 注意：SC 控制怪物不回歸自由模式，不自動巡邏
    }

    // ✅ LivingEntity 版本的死亡事件
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

    // 可以在未來 trigger 傳送門時呼叫
    public void TriggerPortalDialogue()
    {
        if (dialogueManager != null && inkJSON != null)
        {
            dialogueManager.EnterDialogueModeFromKnot(knotBeforePortal);
        }
    }
}
