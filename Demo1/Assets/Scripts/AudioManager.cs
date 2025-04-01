using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip bgmBoss;
    public AudioClip SeShoot;
    public AudioClip SeDamage;
    public AudioClip SeDestroy;

    private List<AudioSource> audios = new List<AudioSource>();

    private void Awake()
    {
        for (int i = 0; i < 3; i++)
        {
            var audio = gameObject.AddComponent<AudioSource>();
            audio.volume = 1.0f; // 確保音量正常
            audios.Add(audio);
        }
    }

    void Start()
    {
        Play(0, "bgmBoss", true); // 確保開場播放音樂
    }

    public void Play(int index, string name, bool isLoop)
    {
        if (index >= audios.Count)
        {
            Debug.LogError("Audio index out of range: " + index);
            return;
        }

        var clip = GetAudioClip(name);
        if (clip != null)
        {
            var audio = audios[index];
            audio.clip = clip;
            audio.loop = isLoop;
            audio.Play();
            Debug.Log("播放音樂：" + name);
        }
        else
        {
            Debug.LogError("未找到音效：" + name);
        }
    }

    private AudioClip GetAudioClip(string name)
    {
        switch (name)
        {
            case "bgmBoss":
                return bgmBoss;
            case "SeShoot":
                return SeShoot;
            case "SeDamage":
                return SeDamage;
            case "SeDestroy":
                return SeDestroy;
            default:
                return null;
        }
    }
}