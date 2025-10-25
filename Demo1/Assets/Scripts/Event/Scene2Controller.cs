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
    public Transform monsterTargetPoint; // 怪物目標
    public Transform playerTargetPoint;  // 玩家目標
    private bool isRunningEvent = false;

    [Header("Audio Settings")]
    public AudioSource audioSource; // 指定 AudioSource
    public AudioClip monsterWhisperClip; // 怪物竊語
    public AudioClip headphoneClip; // 🎧 耳機音效 (請在 Inspector 指定)

    private void Start()
    {
        monster.GetComponent<enemy_cow>().isActive = false;
        monster.GetComponent<Animator>().Play("idle");

        player.GetComponent<PlayerController>().canControl = false;
        player.GetComponent<PlayerController>().FaceLeft();
    }

    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
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

    // 🎧 停止所有音效並播放耳機音效
    private void StopAllAudioForHeadphone()
    {
        Debug.Log("🛑 停止所有音效，播放耳機音效");

        // 停止目前所有音效
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }

        // 播放耳機音效
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
    }
}
