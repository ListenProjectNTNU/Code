using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class Death : LivingEntity
{
    [Header("Patrol / Chase")]
    public Transform leftPoint, rightPoint;
    public float fallbackPatrolHalfWidth = 3.5f;
    public float patrolSpeed = 1.6f;
    public float chaseSpeed = 2.6f;
    public float detectRange = 7.0f;
    public float stopChaseRange = 10f;
    public float stopDistance = 1.5f;

    [Header("Layers")]
    public LayerMask groundMask;
    public LayerMask playerMask;
    public LayerMask specialBlockMask;

    [Header("Melee")]
    public Transform meleePoint;
    public float meleeRange = 0.6f;
    public int meleeDamage = 20;
    public float meleeCooldown = 1.6f;

    [Header("Special (Animation-Driven)")]
    public Transform specialPoint;          // ← 生成遠端手臂的定位點（放在角色身上 or 任何你要的位置）
    public GameObject remoteHandPrefab;     // ← 手臂 Prefab（下面有腳本）
    public float specialCooldown = 5.5f;
    public float specialMinDistance = 3.2f; // 與玩家距離夠遠才會用特殊

    [Header("Refs")]
    public Transform target;
    public SpriteRenderer sr;

    // runtime
    private Rigidbody2D rb;
    private Animator anim;
    private float leftCap, rightCap;
    private bool patrolToRight = true;
    private bool inAttackAnim = false;
    private float nextMeleeTime = 0f;
    private float nextSpecialTime = 0f;
    private bool dead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (leftPoint && rightPoint)
        {
            leftCap = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            rightCap = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
        }
        else
        {
            leftCap = transform.position.x - fallbackPatrolHalfWidth;
            rightCap = transform.position.x + fallbackPatrolHalfWidth;
        }

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    private void Update()
    {
        if (dead) { anim.SetFloat("Speed", 0f); return; }
        if (!target) { Patrol(); return; }

        float dist = Vector2.Distance(transform.position, target.position);

        if (inAttackAnim)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            anim.SetFloat("Speed", 0f);
            FaceTo(target.position.x - transform.position.x);
            return;
        }

        // 優先：特殊 > 近戰 > 追擊 > 巡邏
        if (Time.time >= nextSpecialTime && dist >= specialMinDistance && dist <= stopChaseRange)
        {
            StartSpecial();
            return;
        }
        if (Time.time >= nextMeleeTime && dist <= meleeRange + 0.15f)
        {
            StartMelee();
            return;
        }
        if (dist <= detectRange) Chase(dist); else Patrol();
    }

    private void FaceTo(float dx)
    {
        if (!sr) return;
        if (Mathf.Abs(dx) < 0.01f) return;
        sr.flipX = dx < 0f;
    }

    private void Patrol()
    {
        float speed = patrolSpeed * (patrolToRight ? 1f : -1f);
        rb.velocity = new Vector2(speed, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(speed));
        FaceTo(speed);
        if (patrolToRight && transform.position.x >= rightCap) patrolToRight = false;
        else if (!patrolToRight && transform.position.x <= leftCap) patrolToRight = true;
    }

    private void Chase(float dist)
    {
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        float vx = dist > stopDistance ? (dir * chaseSpeed) : 0f;
        rb.velocity = new Vector2(vx, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(vx));
        FaceTo(dir);
    }

    // ── Melee ───────────────────────────────────────────────────────────────
    private void StartMelee()
    {
        inAttackAnim = true;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        anim.SetFloat("Speed", 0f);
        anim.ResetTrigger("Special");
        anim.SetTrigger("Melee");
    }

    public void Anim_MeleeHit()  // 動畫事件（出招幀）
    {
        nextMeleeTime = Time.time + meleeCooldown;
        if (!meleePoint) meleePoint = transform;
        var hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, playerMask);
        foreach (var h in hits)
            if (h.TryGetComponent<LivingEntity>(out var le))
                le.TakeDamage(meleeDamage);
    }

    public void Anim_AttackEnd() // 動畫事件（尾）
    {
        inAttackAnim = false;
    }

    // ── Special (Animation-driven) ──────────────────────────────────────────
    private void StartSpecial()
    {
        inAttackAnim = true;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        anim.SetFloat("Speed", 0f);
        anim.ResetTrigger("Melee");
        anim.SetTrigger("Special");
    }

    /// <summary>
    /// 特殊攻擊動畫「最後一幀」事件：生成手臂 Prefab（在 specialPoint）
    /// </summary>
    public void Anim_SpecialSpawnHand()
    {
        nextSpecialTime = Time.time + specialCooldown;

        if (!remoteHandPrefab) return;

        // 📍 直接用玩家當下位置生成
        Vector3 pos = target ? target.position : (specialPoint ? specialPoint.position : transform.position);

        var go = Instantiate(remoteHandPrefab, pos, Quaternion.identity);
        if (go.TryGetComponent<RemoteVoidArm>(out var arm))
        {
            arm.Init(playerMask, meleeDamage, target ? target.gameObject : null);
        }
    }

    /// <summary>
    /// 特殊攻擊動畫「最後一幀」事件：解鎖回 AI
    /// </summary>
    public void Anim_SpecialEnd()
    {
        inAttackAnim = false;
    }

    // ── Hurt / Die ──────────────────────────────────────────────────────────
    public void PlayHurt()
    {
        if (dead) return;
        anim.SetTrigger("Hurt");
    }

    protected override void Die()  // 注意：沿用父類的 protected
    {
        if (dead) return;
        dead = true;

        anim.SetBool("IsDead", true);
        anim.SetTrigger("Die");

        rb.velocity = Vector2.zero;
        rb.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    public void Anim_DeathEnd() // 動畫最後一幀
    {
        Destroy(gameObject);
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (meleePoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
        }
        if (specialPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(specialPoint.position, 0.15f);
        }
    }
}
