using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LootEffectType
{
    None,       // 無效果（僅影響對話）
    Attack,     // 提升攻擊力
    Speed,// 提升攻速
    Defense     // 提升防禦
}

[CreateAssetMenu]
public class Loot : ScriptableObject
{
    public Sprite lootSprite;
    public string lootName;
    public int dropChance;

    public LootEffectType effectType; // 屬性類型
    public float effectValue;         // 效果數值

    public Loot(string lootName, int dropChance, LootEffectType effectType, float effectValue)
    {
        this.lootName = lootName;
        this.dropChance = dropChance;
        this.effectType = effectType;
        this.effectValue = effectValue;
    }
}
