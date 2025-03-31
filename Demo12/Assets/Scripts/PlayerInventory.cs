using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using System;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private List<string> collectedItems = new List<string>();
    private InkVariableUpdater inkUpdater;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        inkUpdater = FindObjectOfType<InkVariableUpdater>();
        Debug.Log($"🔍 inkUpdater 是否為 null？{inkUpdater == null}");
    }

    public void AddItem(string itemName)
    {
        collectedItems.Add(itemName);
        Debug.Log($"獲得物品：{itemName}");

        // 更新對應的 Ink 變數
        if (inkUpdater != null)
        {
            Debug.Log($"更新對應的 Ink 變數：has_{itemName}");
            inkUpdater.UpdateVariable($"has_{itemName}", true);
        }
    }

    public bool HasItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }

    internal void AddItem(Loot lootData)
    {
        throw new NotImplementedException();
    }
}
