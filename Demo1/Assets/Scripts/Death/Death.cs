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

    [Header("Special (Cast)")]
    public Transform specialPoint;           // ★生成點（可放在敵人身上，或空物件）
    public GameObject remoteHandPrefab;      // ★手臂 Prefab（下方有腳本）
    public float specialCooldown = 5.0f;     // ★冷卻
    public float specialMinDistance = 3.0f;  // ★太近不施放
    public float specialMaxDistance = 10.0f; // ★太遠不施放（可用 loseAggroRange 也行）
    public bool  lockOnDuringCast = false;   // ★施法過程是否鎖定 specialPoint 跟著玩家

    private float nextSpecialTime = 0f;      // ★
    private bool  inSpecial = false;         // ★
    public float specialPostCastDelay = 1.0f;   // Cast 動畫結束後，等待幾秒才生成手臂
    private SpecialPointFollower spFollower;   // special_point 的跟隨器

    [Header("Arena Scaling")]
    public float extraHpPerWave = 20f;         // 每波 +20 HP
    public float extraDamagePerWave = 2f;      // 每波 +2 傷害
    public float extraSpeedPerWave = 0.1f;     // 每波 +0.1 移動速度

    public int harderFromWave = 6;             // 第 6 波開始進入困難模式
    public float hardHpMultiplier = 1.5f;      // 血量 ×1.5
    public float hardDamageMultiplier = 1.3f;  // 傷害 ×1.3
    public float hardSpeedMultiplier = 1.15f;  // 速度 ×1.15

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
        if (specialPoint)
        spFollower = specialPoint.GetComponent<SpecialPointFollower>();
    }

    private void Update()
    {
        if (dead) { anim.SetFloat("Speed", 0); return; }
        if (!target) return;

        float dist = Vector2.Distance(transform.position, target.position);

        // ✅ 先更新仇恨狀態（放最前面！）
        if (!hasAggro && dist <= detectRange) hasAggro = true;
        if (hasAggro && dist >= loseAggroRange) hasAggro = false;

        // ★特殊攻擊優先（進入施法距離 + 冷卻好 + 正在仇恨）
        if (!inAttackAnim && !inSpecial && hasAggro &&
            Time.time >= nextSpecialTime &&
            dist >= specialMinDistance && dist <= specialMaxDistance &&
            remoteHandPrefab != null)
        {
            StartSpecial();
            return;
        }

        // 攻擊期間：鎖腳並面向玩家
        if (inAttackAnim)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("Speed", 0);
            FaceTo(target.position.x - transform.position.x);
            return;
        }

        // 近戰或移動邏輯
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

        /// <summary>
        /// 由 ArenaManager 生成時呼叫，根據 wave 調整數值。
        /// </summary>
    public void OnArenaScale(int wave)
    {
        // ---------- 1) 基礎血量成長 ----------
        float newHp = maxHealth + (wave - 1) * extraHpPerWave;

        // ---------- 2) 基礎傷害成長 ----------
        float newDmg = meleeDamage + (wave - 1) * extraDamagePerWave;

        // ---------- 3) 移動速度成長 ----------
        float newPatrolSpeed = patrolSpeed + (wave - 1) * extraSpeedPerWave;
        float newChaseSpeed  = chaseSpeed  + (wave - 1) * extraSpeedPerWave;

        // ---------- 4) 進入困難模式（第 6 波起） ----------
        if (wave >= harderFromWave)
        {
            newHp          = Mathf.RoundToInt(newHp * hardHpMultiplier);
            newDmg         = Mathf.RoundToInt(newDmg * hardDamageMultiplier);
            newPatrolSpeed = newPatrolSpeed * hardSpeedMultiplier;
            newChaseSpeed  = newChaseSpeed  * hardSpeedMultiplier;
        }

        // ---------- 5) 回寫到怪物 ----------
        maxHealth = newHp;
        currentHealth = maxHealth;      // 新生成怪 → 滿血
        meleeDamage = Mathf.RoundToInt(newDmg);
        patrolSpeed = newPatrolSpeed;
        chaseSpeed  = newChaseSpeed;

        // 如果想讓特殊攻擊跟著加強，也可以寫：
        // specialDamage = meleeDamage;
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

    // ★進入施法動畫
    private void StartSpecial()
    {
        if (Time.time < nextSpecialTime) return;
        if (!target) return;

        inAttackAnim = true;   // ← 鎖住移動（Update 會把 vx 設 0）
        inSpecial    = true;

        nextSpecialTime = Time.time + specialCooldown; // 進入冷卻（關鍵！）

        rb.velocity = new Vector2(0, rb.velocity.y);
        anim.SetFloat("Speed", 0);
        anim.ResetTrigger("attack");
        anim.SetTrigger("cast");
        FaceTo(target.position.x - transform.position.x);

        // 施法期間讓 special_point 追蹤玩家（可關閉 lockOnDuringCast）
        if (lockOnDuringCast && spFollower)
        {
            spFollower.Bind(target);
            spFollower.StartFollow();
        }
    }

    // ★（可選）施法期間鎖定 specialPoint 追蹤玩家
    private System.Collections.IEnumerator SpecialLockOnRoutine(float duration)
    {
        float t = 0f;
        while (t < duration && inSpecial && target && specialPoint)
        {
            specialPoint.position = target.position; // 讓手臂生成點黏著玩家
            t += Time.deltaTime;
            yield return null;
        }
    }

    public void Anim_SpecialSpawnHand()
    {
        // ★ 保險：確保至少有 CD
        if (Time.time + 0.1f > nextSpecialTime)
            nextSpecialTime = Time.time + specialCooldown;

        if (!remoteHandPrefab) return;

        Vector3 spawnPos = specialPoint ? specialPoint.position
                        : (target ? target.position : transform.position);

        var go = Instantiate(remoteHandPrefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent<RemoteVoidArm>(out var arm))
            arm.Init(playerMask, meleeDamage);
}

    // ★動畫事件：Cast 結束，解鎖腳，回到 AI
    public void Anim_SpecialEnd()
    {
        StartCoroutine(SpawnArmAfterDelay(specialPostCastDelay));
    }

    private System.Collections.IEnumerator SpawnArmAfterDelay(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            // 持續鎖腳，不能移動
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat("Speed", 0);
            t += Time.deltaTime;
            yield return null;
        }

        // 生成點：若有 special_point 用它；否則用玩家位置
        Vector3 spawnPos = specialPoint ? specialPoint.position
                            : (target ? target.position : transform.position);

        if (remoteHandPrefab)
        {
            var go = Instantiate(remoteHandPrefab, spawnPos, Quaternion.identity);
            if (go.TryGetComponent<RemoteVoidArm>(out var arm))
                arm.Init(playerMask, meleeDamage); // 或改為 specialDamage
        }

        // 關閉跟隨
        if (spFollower) spFollower.StopFollow();

        // 解鎖：回到 AI 流程
        inSpecial    = false;
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
