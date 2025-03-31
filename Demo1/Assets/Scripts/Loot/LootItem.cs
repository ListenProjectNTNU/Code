using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    public Loot lootData; // 記錄該物品是哪個 Loot

    private void Start()
    {
        if (lootData == null)
        {
            Debug.LogError($"❌ {gameObject.name} 的 lootData 為 null，請確認是否有正確指派 Loot 資產！");
        }
    }
}
