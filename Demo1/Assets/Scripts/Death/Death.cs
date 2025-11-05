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
    public float loseAggroRange = 11f;
    public float stopDistance = 1.5f;

    [Header("Layers")]
    public LayerMask groundMask;
    public LayerMask playerMask;

    [Header("Melee")]
    public Transform meleePoint;
    public float meleeRange = 0.6f;
    public int meleeDamage = 20;
    public float meleeCooldown = 1.6f;

    [Header("Refs")]
    public Transform target;
    public SpriteRenderer sr;

    [Header("Sprite Facing")]
    [Tooltip("素材預設是否面向右？你的素材是面向左，請保持 false")]
    public bool spriteFacesRight = false;   // ← 你的素材朝左，這裡預設 false

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D myCol;
    private Collider2D playerCol;

    private bool patrolToRight = true;
    private bool inAttackAnim = false;
    private bool dead = false;
    private bool hasAggro = false;
    private float nextMeleeTime = 0f;

    private float leftCap, rightCap;
    private float minStopDist = 0.8f;
    private float vxSmoothVel = 0f;
    private float nextDirSwitchTime = 0f;
    private int lastMoveDir = 0;

    [Header("Chase Smooth")]
    public float desiredOffset = 0f;
    public float approachBuffer = 0.25f;
    public float dirSwitchMinInterval = 0.2f;
    public float accel = 14f;
    public float decel = 18f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        myCol = GetComponent<Collider2D>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // 目標
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target) playerCol = target.GetComponent<Collider2D>();

        // 舒適距離（半寬相加）
        if (myCol && playerCol)
            minStopDist = myCol.bounds.extents.x + playerCol.bounds.extents.x + 0.15f;

        // 巡邏邊界（可缺）
        if (leftPoint && rightPoint)
        {
            leftCap  = Mathf.Min(leftPoint.position.x, rightPoint.position.x);
            rightCap = Mathf.Max(leftPoint.position.x, rightPoint.position.x);
        }
        else
        {
            leftCap  = transform.position.x - fallbackPatrolHalfWidth;
            rightCap = transform.position.x + fallbackPatrolHalfWidth;
        }
    }

    private void Update()
    {
        if (dead) { anim.SetFloat("Speed", 0); return; }
        if (!target) return;

        float dist = Vector2.Distance(transform.position, target.position);

        // 黏性追擊
        if (!hasAggro && dist <= detectRange) hasAggro = true;
        if ( hasAggro && dist >= loseAggroRange) hasAggro = false;

        // 攻擊期間：鎖腳並面向玩家
        if (inAttackAnim)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("Speed", 0);
            FaceTo(target.position.x - transform.position.x);
            return;
        }

        // 先攻擊，後位移
        if (Time.time >= nextMeleeTime && dist <= Mathf.Max(meleeRange, minStopDist + 0.05f))
        {
            StartMelee();
        }
        else if (hasAggro)
        {
            ChaseKeepDistance(dist);
        }
        else
        {
            Patrol();
        }
    }

    // ───────────────────────── Movement / Facing ─────────────────────────
    private void FaceTo(float dxTowardTarget)
    {
        if (!sr) return;
        if (Mathf.Abs(dxTowardTarget) < 0.01f) return;

        // wantFaceLeft = 目標在左
        bool wantFaceLeft = dxTowardTarget < 0f;

        // 若素材預設朝右：面向左時 flipX=true；若素材預設朝左：邏輯顛倒
        sr.flipX = spriteFacesRight ? wantFaceLeft : !wantFaceLeft;
    }

    private void Patrol()
    {
        float dir = patrolToRight ? 1f : -1f;
        float vx = dir * patrolSpeed;

        // 位移
        rb.velocity = new Vector2(vx, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(vx));

        // 巡邏看移動方向
        FaceTo(dir);

        // 觸邊換向
        if (patrolToRight && transform.position.x >= rightCap) patrolToRight = false;
        else if (!patrolToRight && transform.position.x <= leftCap) patrolToRight = true;
    }

    private void ChaseKeepDistance(float dist)
    {
        float desired  = Mathf.Max(minStopDist, meleeRange) + desiredOffset;
        float outer    = desired + approachBuffer; // 超過外圈才前進
        float inner    = desired - approachBuffer; // 進入內圈才後退
        float toPlayer = Mathf.Sign(target.position.x - transform.position.x);

        int wantDir = 0;
        if (dist > outer)      wantDir = +1;
        else if (dist < inner) wantDir = -1;

        if (Time.time >= nextDirSwitchTime)
        {
            if (wantDir != lastMoveDir)
            {
                lastMoveDir = wantDir;
                nextDirSwitchTime = Time.time + dirSwitchMinInterval;
            }
        }

        float targetVx = lastMoveDir * chaseSpeed;
        float smoothTime = (Mathf.Abs(targetVx) < 0.01f) ? (1f / decel) : (1f / accel);
        float newVx = Mathf.SmoothDamp(rb.velocity.x, targetVx, ref vxSmoothVel, smoothTime);

        rb.velocity = new Vector2(newVx, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(newVx));

        // 追擊時看玩家方向
        FaceTo(toPlayer);
    }

    // ───────────────────────── Melee ─────────────────────────
    private void StartMelee()
    {
        inAttackAnim = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        anim.SetFloat("Speed", 0);
        anim.SetTrigger("attack");

        // 攻擊開始即對準玩家（避免剛轉身晚一幀）
        FaceTo(target.position.x - transform.position.x);
    }

    public void Anim_MeleeHit()
    {
        nextMeleeTime = Time.time + meleeCooldown;
        if (!meleePoint) meleePoint = transform;

        var hits = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, playerMask);
        foreach (var h in hits)
            if (h.TryGetComponent<LivingEntity>(out var le))
                le.TakeDamage(meleeDamage);
    }

    public void Anim_AttackEnd()
    {
        inAttackAnim = false;
    }

    // ───────────────────────── Hurt / Die ─────────────────────────
    public void PlayHurt()
    {
        if (dead) return;
        anim.ResetTrigger("attack");
        anim.SetTrigger("hurt");
        rb.velocity = Vector2.zero;
        inAttackAnim = false;
    }

    protected override void Die()
    {
        if (dead) return;
        dead = true;

        anim.ResetTrigger("attack");
        anim.SetTrigger("die");
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    public void Anim_DeathEnd()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // ===== 1️⃣ 實際攻擊命中圈（紅色） =====
        if (meleePoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
        }

        // ===== 2️⃣ 攻擊觸發距離（綠色） =====
        // 公式：Mathf.Max(meleeRange, minStopDist + 0.05f)
        // 因為 minStopDist 是執行時才算出的，我們在編輯器暫時估一個近似值
        float approxStopDist = 0.6f; // 給個平均寬度估值（不用太精確）
        float attackTriggerRange = Mathf.Max(meleeRange, approxStopDist + 0.05f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackTriggerRange);

        // ===== 3️⃣ 偵測與追擊距離（藍色 / 青色） =====
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);

        // ===== 4️⃣ 巡邏邊界（淡灰線） =====
        Gizmos.color = Color.gray;
        if (leftPoint && rightPoint)
        {
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
        }
        else
        {
            Gizmos.DrawLine(new Vector3(transform.position.x - fallbackPatrolHalfWidth, transform.position.y),
                            new Vector3(transform.position.x + fallbackPatrolHalfWidth, transform.position.y));
        }
    }
}
