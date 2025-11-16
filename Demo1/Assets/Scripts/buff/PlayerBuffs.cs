using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBuffs : MonoBehaviour
{
    // ── 你的段數制 ──
    [Header("Segments (stacks)")]
    public int attackSeg = 0, defenceSeg = 0, speedSeg = 0;

    // ── 乘數 / 加值 ──
    [Header("Additive / Multipliers")]
    public float damageTakenMultiplier = 1f;   // <1 減傷, >1 受更多傷
    public float moveSpeedMultiplier   = 1f;   // 與 base speed 相乘
    public float dashCooldownMultiplier= 1f;   // 與 base cd 相乘
    public float dashDurationBonus     = 0f;   // 直接加秒數
    public float jumpForceBonus        = 0f;   // 直接加跳躍力
    public float regenPerSecond        = 0f;   // 每秒回血
    public float knockbackTakenMultiplier = 1f;// <1 更穩
    public float dashDistanceMultiplier = 1f;

    // ── 一次性護盾 ──
    [HideInInspector] public bool oneTimeShield = false;

    // 👉 新增：紀錄玩家已取得的所有 Buff
    [Header("Runtime Buff List")]
    public List<BuffSO> acquiredBuffs = new();

    // 統一的註冊入口（之後面板會來讀這份清單）
    public void RegisterBuff(BuffSO buff)
    {
        if (buff == null) return;
        acquiredBuffs.Add(buff);
    }
    
    // ── 便利方法：把 base 值轉有效值（給控制器讀）──
    public int CurAttack   (int baseAttack)  => baseAttack  + attackSeg  * 10;
    public int CurDefence  (int baseDefence) => baseDefence + defenceSeg * 10;
    public int CurSpeed    (int baseSpeed)   => baseSpeed   + speedSeg   * 20;

    // ── 時效型 Buff（可選擇使用；用 unscaled 計時）──
    private readonly List<Timed> _timed = new();
    private LivingEntity _le;

    private void Awake()
    {
        _le = GetComponent<LivingEntity>();
    }

    private void Update()
    {

        if (_timed.Count == 0) return;

        float dt = Time.unscaledDeltaTime;
        for (int i = _timed.Count - 1; i >= 0; i--)
        {
            _timed[i].remain -= dt;
            if (_timed[i].remain <= 0f)
            {
                _timed[i].onExpire?.Invoke();
                _timed.RemoveAt(i);
            }
        }
    }

    public void AddTimed(float seconds, System.Action onExpire)
        => _timed.Add(new Timed { remain = seconds, onExpire = onExpire });

    private class Timed { public float remain; public System.Action onExpire; }
}
