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

    // 這個當作「後備值」（找不到玩家時用），平時會被玩家的最終攻擊覆蓋
    public int attackDamage = 30;

    public float knockbackForce = 5f;

    // 內部狀態
    private readonly HashSet<GameObject> hitEnemiesThisAttack = new HashSet<GameObject>();
    private bool inAttackWindow = false;
    private bool inputLocked = false;

    private static readonly Collider2D[] _hits = new Collider2D[16];

    private static readonly int KickHash  = Animator.StringToHash("kick");
    private static readonly int Kick2Hash = Animator.StringToHash("kick2");
    private static readonly int PunchHash = Animator.StringToHash("punch");

    // 🔹 快取玩家與（可選）Buff
    private ArenaPlayerController player;
    private PlayerBuffs buffs;

    private void Awake()
    {
        player = ArenaPlayerController.Instance;
        if (player) buffs = player.GetComponent<PlayerBuffs>();
    }
    private void Start()
    {
        // 在這時抓，一定能拿到 Instance
        player = ArenaPlayerController.Instance;
        if (player) buffs = player.GetComponent<PlayerBuffs>();
    }

    void Update()
    {
        if (inputLocked) return;

        if (Input.GetKeyDown(KeyCode.Q)) animator.SetTrigger(KickHash);
        else if (Input.GetKeyDown(KeyCode.C)) animator.SetTrigger(Kick2Hash);
        else if (Input.GetKeyDown(KeyCode.R)) animator.SetTrigger(PunchHash);
    }

    // 動畫事件：開啟可命中窗口
    public void OpenHit()
    {
        inAttackWindow = true;
        inputLocked = true;
        hitEnemiesThisAttack.Clear();
    }

    // 動畫事件：真正命中那一幀
    public void DoHit()
    {
        if (!inAttackWindow || attackPoint == null) return;

        int count = Physics2D.OverlapCircleNonAlloc(
            attackPoint.position, attackRange, _hits, enemyLayers
        );

        // ⬇️ 這裡「一次」讀出本次出手要用的最終攻擊力
        //    player.curattack 已經把 buff/seg 都算進去了
        int finalAttack =
            (player != null) ? player.curattack : attackDamage;

        // （可選）若你在 PlayerBuffs 另外做了攻擊乘數/暴擊，也可在這裡一起處理
        // float atkMul = (buffs ? Mathf.Max(0.01f, buffs.attackMultiplier) : 1f);
        // finalAttack = Mathf.RoundToInt(finalAttack * atkMul);

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            var go = col.gameObject;
            if (hitEnemiesThisAttack.Contains(go)) continue;
            hitEnemiesThisAttack.Add(go);

            var target = go.GetComponent<LivingEntity>();
            if (target != null)
            {
                target.TakeDamage(finalAttack);
                // Debug.Log($"[MELEE] deal {finalAttack} to {target.name}");
            }

            // 擊退（可選把「給出去的擊退」也吃 buff 乘數）
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float kb = knockbackForce;
                // if (buffs) kb *= buffs.knockbackDealtMultiplier;
                Vector2 dir = (go.transform.position - transform.position).normalized;
                rb.AddForce(dir * kb, ForceMode2D.Impulse);
            }

            _hits[i] = null; // 清掉引用，保險
        }
    }

    // 動畫事件：關閉可命中窗口
    public void CloseHit()
    {
        inAttackWindow = false;
        inputLocked = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
