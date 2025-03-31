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
    private PlayerController playerController;

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
        playerController = FindObjectOfType<PlayerController>();
        
        Debug.Log($"🔍 inkUpdater 是否為 null？{inkUpdater == null}");
        Debug.Log($"🔍 playerController 是否為 null？{playerController == null}");
    }

    public void AddItem(Loot lootData)
    {
        if (lootData == null)
        {
            Debug.LogError("❌ LootData 為 null，無法添加物品！");
            return;
        }
        
        collectedItems.Add(lootData.lootName);
        Debug.Log($"✅ 獲得物品：{lootData.lootName}");

        // 更新對應的 Ink 變數
        if (inkUpdater != null)
        {
            Debug.Log($"📝 更新 Ink 變數：has_{lootData.lootName}");
            inkUpdater.UpdateVariable($"has_{lootData.lootName}", true);
        }
        else
        {
            Debug.LogError("⚠️ 無法更新 Ink 變數，InkVariableUpdater 為 null！");
        }
        
        // 應用物品效果
        //ApplyLootEffects(lootData);
    }

    public void ApplyLootEffects(Loot lootData)
    {
        Debug.Log("ApplyLootEffects()被執行");
        if (playerController == null)
        {
            Debug.LogError("❌ PlayerController 為 null，無法應用道具效果！");
            return;
        }
        
        switch (lootData.effectType)
        {
            case LootEffectType.Attack:
                playerController.attackseg++;
                Debug.Log($"⚔️ 攻擊力提升！當前攻擊段數：{playerController.attackseg}");
                break;
            case LootEffectType.Defense:
                playerController.defenceseg++;
                Debug.Log($"🛡️ 防禦力提升！當前防禦段數：{playerController.defenceseg}");
                break;
            case LootEffectType.Speed:
                playerController.speedseg++;
                Debug.Log($"⚡ 速度提升！當前速度段數：{playerController.speedseg}");
                break;
            default:
                Debug.LogWarning($"⚠️ 未知的 Loot 類型：{lootData.effectType}");
                break;
        }
    }

    public bool HasItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }

    public void AddItem(string itemName)
    {
        if (!string.IsNullOrEmpty(itemName))
        {
            collectedItems.Add(itemName);
            Debug.Log($"✅ 撿起物品：{itemName}");
        }
        else
        {
            Debug.LogError("❌ 嘗試添加的物品名稱為空！");
        }
    }
}