using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private InkVariableUpdater inkUpdater;

    [Header("Inventory Data")]
    [SerializeField] private List<string> collectedItems = new List<string>();
    public IReadOnlyList<string> CollectedItems => collectedItems;

    private void Awake()
    {
        // 單例設計
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 嘗試自動尋找 PlayerController
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
                Debug.Log("✅ 自動找到 PlayerController");
            else
                Debug.LogWarning("⚠️ 場景中找不到 PlayerController，部分道具效果可能無法應用");
        }

        // 嘗試自動尋找 InkVariableUpdater
        if (inkUpdater == null)
        {
            inkUpdater = FindObjectOfType<InkVariableUpdater>();
            if (inkUpdater != null)
                Debug.Log("✅ 自動找到 InkVariableUpdater");
            else
                Debug.LogWarning("⚠️ 場景中找不到 InkVariableUpdater，將無法更新 Ink 變數");
        }
    }

    public void AddItem(Loot lootData)
    {
        if (lootData == null)
        {
            Debug.LogError("❌ LootData 為 null，無法添加物品！");
            return;
        }

        Debug.Log($"✅ 獲得物品：{lootData.lootName}");
        collectedItems.Add(lootData.lootName);

        // 更新 Ink 變數
        if (inkUpdater != null)
        {
            inkUpdater.UpdateVariable($"has_{lootData.lootName}", true);
            Debug.Log($"📝 更新 Ink 變數 has_{lootData.lootName}");
        }
        else
        {
            Debug.LogWarning("⚠️ InkVariableUpdater 為 null，暫時無法更新 Ink 變數");
        }

        // 應用物品效果
        ApplyLootEffects(lootData);
    }

    private void ApplyLootEffects(Loot lootData)
    {
        if (playerController == null)
        {
            Debug.LogWarning("⚠️ PlayerController 為 null，無法應用道具效果！");
            return;
        }

        switch (lootData.effectType)
        {
            case LootEffectType.Attack:
                playerController.attackseg++;
                Debug.Log($"⚔️ 攻擊力提升 → {playerController.curattack}");
                break;

            case LootEffectType.Defense:
                playerController.defenceseg++;
                Debug.Log($"🛡️ 防禦力提升 → {playerController.curdefence}");
                break;

            case LootEffectType.Speed:
                playerController.speedseg++;
                Debug.Log($"⚡ 速度提升 → {playerController.curspeed}");
                break;

            default:
                Debug.LogWarning($"❓ 未知效果類型：{lootData.effectType}");
                break;
        }
    }

    public bool HasItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }
}
