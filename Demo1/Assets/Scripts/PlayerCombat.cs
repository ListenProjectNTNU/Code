using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 30;
    public float knockbackForce = 5f;

    // 內部狀態
    private readonly HashSet<GameObject> hitEnemiesThisAttack = new HashSet<GameObject>();
    private bool inAttackWindow = false;   // 由動畫事件開關
    private bool inputLocked = false;      // 攻擊期間鎖輸入（動畫事件結束時解除）

    // 非配置：避免 GC 的暫存陣列（依你的最多同時命中數量調整）
    private static readonly Collider2D[] _hits = new Collider2D[16];

    // 預先算好的 Trigger Hash（效能小優化）
    private static readonly int KickHash  = Animator.StringToHash("kick");
    private static readonly int Kick2Hash = Animator.StringToHash("kick2");
    private static readonly int PunchHash = Animator.StringToHash("punch");

    void Update()
    {
        if (inputLocked) return;

        if (Input.GetKeyDown(KeyCode.Q)) animator.SetTrigger(KickHash);
        else if (Input.GetKeyDown(KeyCode.C)) animator.SetTrigger(Kick2Hash);
        else if (Input.GetKeyDown(KeyCode.R)) animator.SetTrigger(PunchHash);
    }

    // ===== 動畫事件用：在打擊前 2~3 幀呼叫 =====
    // 開啟本次攻擊的「可命中窗口」，並清空已命中名單
    public void OpenHit()
    {
        inAttackWindow = true;
        inputLocked = true;
        hitEnemiesThisAttack.Clear();
    }

    // ===== 動畫事件用：剛好在武器/腳掌接觸畫面那一幀呼叫 =====
    // 單次取樣做傷害（想做多段 hit 可在動畫放多個 DoHit 事件）
    public void DoHit()
    {
        if (!inAttackWindow || attackPoint == null) return;

        int count = Physics2D.OverlapCircleNonAlloc(
            attackPoint.position, attackRange, _hits, enemyLayers
        );

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            var go = col.gameObject;
            if (hitEnemiesThisAttack.Contains(go)) continue;
            hitEnemiesThisAttack.Add(go);

            // 傷害
            var target = go.GetComponent<LivingEntity>();
            if (target != null)
                target.TakeDamage(attackDamage);

            // 擊退
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (go.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }

            _hits[i] = null; // 清掉引用，保險
        }
    }

    // ===== 動畫事件用：在打擊窗口結束時呼叫 =====
    public void CloseHit()
    {
        inAttackWindow = false;
        inputLocked = false;
        // 可選：hitEnemiesThisAttack.Clear(); // 下次 OpenHit 也會清
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
