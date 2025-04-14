using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    public PlayerData playerData = new PlayerData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ✅ 保留資料
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
