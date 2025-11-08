using UnityEngine;

public enum BuffEffectType
{
    AttackSegDelta, DefenceSegDelta, SpeedSegDelta,
    DamageTakenMultiplier, MoveSpeedMultiplier,
    DashCooldownMultiplier, DashDurationBonusSeconds,
    JumpForceBonus, RegenPerSecondAdd,
    KnockbackTakenMultiplier, OneTimeShield, InstantHeal
}

[CreateAssetMenu(menuName = "Arena/Buff", fileName = "Buff_")]
public class BuffSO : ScriptableObject
{
    [Header("Display")]
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public BuffEffectType effect;
    public int   intValue   = 0;    // 給 *SegDelta 類型
    public float floatValue = 0f;   // 給乘數/加值類型

    public void Apply(GameObject playerGO)
    {
        if (!playerGO) return;

        var buffs = playerGO.GetComponent<PlayerBuffs>();
        var le    = playerGO.GetComponent<LivingEntity>();
        if (!buffs) return;

        switch (effect)
        {
            case BuffEffectType.AttackSegDelta:
                buffs.attackSeg += intValue; break;
            case BuffEffectType.DefenceSegDelta:
                buffs.defenceSeg += intValue; break;
            case BuffEffectType.SpeedSegDelta:
                buffs.speedSeg += intValue; break;

            case BuffEffectType.DamageTakenMultiplier:
                buffs.damageTakenMultiplier *= Mathf.Max(0.01f, floatValue); break;
            case BuffEffectType.MoveSpeedMultiplier:
                buffs.moveSpeedMultiplier   += floatValue; break;
            case BuffEffectType.DashCooldownMultiplier:
                buffs.dashCooldownMultiplier*= Mathf.Max(0.2f, floatValue); break;
            case BuffEffectType.DashDurationBonusSeconds:
                buffs.dashDurationBonus     += Mathf.Max(0f, floatValue); break;
            case BuffEffectType.JumpForceBonus:
                buffs.jumpForceBonus        += floatValue; break;
            case BuffEffectType.RegenPerSecondAdd:
                buffs.regenPerSecond        += Mathf.Max(0f, floatValue); break;
            case BuffEffectType.KnockbackTakenMultiplier:
                buffs.knockbackTakenMultiplier *= Mathf.Max(0.1f, floatValue); break;
            case BuffEffectType.OneTimeShield:
                buffs.oneTimeShield = true; break;
        }
    }
}
