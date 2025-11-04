using UnityEngine;

/// <summary>
/// 空中巡航 → 射擊 2 次 → 降落發呆 → 起飛回空巡
/// Animator：idle(Loop) + flyattack(Trigger)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlyerShooterArena : LivingEntity
{
    [Header("Refs")]
    public Transform player;
    public Transform shootPoint;
    public Projectile projectilePrefab;

    [Header("Detect & Combat")]
    public float detectRange = 12f;
    public float shootRange  = 10f;
    public LayerMask losBlockMask;
    public float attackCooldown = 1.3f;   // ≥ 射擊動畫長度
    public float projectileSpeed = 10f;
    public int shotsBeforeLand = 2;

    [Header("Air Patrol (Natural Path)")]
    public Transform leftPoint;             // 可不設，會用 fallback
    public Transform rightPoint;
    public float fallbackPatrolHalfWidth = 4f;
    public float cruiseSpeed = 3.5f;        // 水平巡航速度
    public float baseAltitude = 5f;         // 巡航目標高度(Y)
    public float sinAmp = 0.6f;
    public float sinFreq = 1.2f;
    public float perlinAmp = 0.3f;
    public float perlinFreq = 0.4f;
    public float turnSmooth = 8f;           // 轉向平滑

    [Header("Landing / Idle / Takeoff")]
    public LayerMask groundMask;
    public float descendSpeed = 6f;
    public float ascendSpeed  = 6f;
    public float groundIdleDuration = 2.2f;
    public float takeoffDelay = 0.3f;
    public float landOffset = 0.2f;

    [Header("Animator Triggers")]
    public string flyAttackTrigger = "flyattack";  // Animator trigger 名稱
    public string hurtTrigger = "hurt";            // 可選
    public string dieTrigger  = "die";             // 可選

    [Header("Control")]
    public bool requireLineOfSight = true;
    public bool facePlayer = true;
    public bool isActive = true;

    // runtime
    private Animator anim;
    private Rigidbody2D rb;
    private float leftCap, rightCap;
    private int horizDir = 1;               // 1:右 / -1:左
    private float perlinSeed;
    private float nextShootTime;
    private int shotCount;
    private float groundIdleUntil;
    private float takeoffAt;
    private Vector2 landingPoint;
    private bool inAttackAnim;

    private enum State { AirPatrol, Descend, GroundIdle, Takeoff }
    private State state = State.AirPatrol;

    // ───────── Unity ─────────
    // 不要 override，因為 LivingEntity 沒有 Awake()
    protected void Awake()
    {
        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // 飛行由腳本控制
        perlinSeed = Random.Range(0f, 1000f);
    }

    // LivingEntity 有 virtual Start()，這裡要 override 並呼叫 base
    protected override void Start()
    {
        base.Start();

        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc) player = pc.transform;
        }

        // 巡邏邊界
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

    void Update()
    {
        if (!isActive || isDead) { rb.velocity = Vector2.zero; return; }
        if (!player) return;
        // 面向處理
        if (facePlayer)
        {
            // 飛行/升降時以速度決定朝向，其餘以玩家決定
            if (state == State.AirPatrol || state == State.Takeoff || state == State.Descend)
            {
                float dirX = rb.velocity.x;
                if (Mathf.Abs(dirX) > 0.05f) Face(dirX);
            }
            else
            {
                float dir = player.position.x - transform.position.x;
                if (Mathf.Abs(dir) > 0.05f) Face(dir);
            }
        }

        switch (state)
        {
            case State.AirPatrol:
                DoAirPatrol();
                TryShootInAir();
                break;

            case State.Descend:
                DoDescend();
                break;

            case State.GroundIdle:
                DoGroundIdle();
                break;

            case State.Takeoff:
                DoTakeoff();
                break;
        }
        // Animator 一直保持 idle 迴圈，不再設定 int 狀態
    }

    // ───────── 行為邏輯 ─────────
    void DoAirPatrol()
    {
        // 到邊界換向
        if (transform.position.x >= rightCap - 0.1f) horizDir = -1;
        if (transform.position.x <= leftCap  + 0.1f) horizDir =  1;

        // 水平速度（平滑）
        float targetVX = horizDir * cruiseSpeed;
        float vx = Mathf.Lerp(rb.velocity.x, targetVX, Time.deltaTime * turnSmooth);

        // 垂直目標：正弦 + Perlin
        float t = Time.time;
        float sinY   = Mathf.Sin(t * sinFreq * Mathf.PI * 2f) * sinAmp;
        float noiseY = (Mathf.PerlinNoise(perlinSeed, t * perlinFreq) - 0.5f) * 2f * perlinAmp;
        float targetY = baseAltitude + sinY + noiseY;

        float vy = Mathf.Clamp((targetY - transform.position.y) * 6f, -ascendSpeed, ascendSpeed);
        rb.velocity = new Vector2(vx, vy);
    }

    void TryShootInAir()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectRange || dist > shootRange) return;
        if (Time.time < nextShootTime) return;

        if (requireLineOfSight)
        {
            Vector2 from = shootPoint ? (Vector2)shootPoint.position : (Vector2)transform.position;
            if (Physics2D.Linecast(from, player.position, losBlockMask)) return;
        }

        inAttackAnim = true;
        if (!string.IsNullOrEmpty(flyAttackTrigger))
            anim.SetTrigger(flyAttackTrigger);     // 觸發攻擊動畫
        nextShootTime = Time.time + attackCooldown;  // 真正發射在 AnimEvent_Shoot
    }

    void DoDescend()
    {
        // 決定落點（向下 Raycast）
        if (landingPoint == Vector2.zero)
        {
            Vector2 origin = transform.position;
            var hit = Physics2D.Raycast(origin, Vector2.down, 30f, groundMask);
            if (hit.collider) landingPoint = hit.point + Vector2.up * landOffset;
            else landingPoint = new Vector2(transform.position.x, transform.position.y - 3f);
        }

        // 下降（水平速度漸停）
        Vector2 v = new Vector2(Mathf.Lerp(rb.velocity.x, 0f, Time.deltaTime * 4f), -descendSpeed);
        // 接近落點就直接貼上
        if ((landingPoint.y - transform.position.y) >= -0.05f)
        {
            transform.position = landingPoint;
            rb.velocity = Vector2.zero;
            groundIdleUntil = Time.time + groundIdleDuration;
            takeoffAt = groundIdleUntil + takeoffDelay;
            state = State.GroundIdle;
            return;
        }
        rb.velocity = v;
    }

    void DoGroundIdle()
    {
        rb.velocity = Vector2.zero;

        if (anim != null)
            anim.Play("idle");

        // 保證 takeoffAt 被設定
        if (takeoffAt <= 0f)
        {
            takeoffAt = Time.time + groundIdleDuration + takeoffDelay;
            Debug.LogWarning($"[Flyer] takeoffAt 未設定，自動補：{takeoffAt:F2}");
        }

        Debug.Log($"[Flyer] GroundIdle... Time={Time.time:F2}, takeoffAt={takeoffAt:F2}");

        // 時間到 → 起飛
        if (Time.time >= takeoffAt)
        {
            Debug.Log("[Flyer] 🛫 起飛條件成立，切換到 Takeoff 狀態！");
            landingPoint = Vector2.zero;
            state = State.Takeoff;
        }
    }


    void DoTakeoff()
    {
        if (anim != null)
            anim.Play("idle"); // 起飛時保持 idle 動畫

        float targetY = baseAltitude;
        float vy = Mathf.Clamp((targetY - transform.position.y) * 6f, 2f, ascendSpeed);
        rb.velocity = new Vector2(
            Mathf.Lerp(rb.velocity.x, horizDir * cruiseSpeed, Time.deltaTime * 2f),
            vy
        );

        Debug.Log($"[Flyer] Takeoff... Y={transform.position.y:F2}, targetY={targetY}");

        // 高度達成 → 回巡航
        if (transform.position.y >= targetY - 0.1f)
        {
            Debug.Log("[Flyer] 🌀 回到 AirPatrol 狀態！");
            rb.velocity = new Vector2(horizDir * cruiseSpeed, 0f);
            shotCount = 0;
            nextShootTime = Time.time + 0.4f;
            state = State.AirPatrol;
        }
    }


    void Face(float dirX)
    {
        float sign = Mathf.Sign(dirX);
        var s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
    }

    // ───────── 動畫事件 ─────────
    // 在 flyattack 動畫關鍵幀呼叫
    public void AnimEvent_Shoot()
    {
        if (!inAttackAnim || isDead || !isActive) return;
        if (!player || projectilePrefab == null) return;

        Vector2 from = shootPoint ? (Vector2)shootPoint.position : (Vector2)transform.position;
        Vector2 dir  = ((Vector2)player.position - from).normalized;

        var p = Instantiate(projectilePrefab);
        p.targetTag = "Player";
        p.groundMask = groundMask;
        p.Fire(from, dir);

        shotCount++;
        inAttackAnim = false;

        // 連射達上限 → 降落
        if (shotCount >= shotsBeforeLand && state == State.AirPatrol)
        {
            state = State.Descend;
            landingPoint = Vector2.zero;
        }
    }

    public void AnimEvent_ShootEnd() { inAttackAnim = false; }

    // ───────── 可選：受傷/死亡動畫對接 ─────────
    public override void TakeDamage(float damage)
    {
        if (isDead) return;
        base.TakeDamage(damage);                 // 扣血 & 觸發 Die()（若歸零）
        if (!isDead && !string.IsNullOrEmpty(hurtTrigger))
            anim.SetTrigger(hurtTrigger);
    }

    protected override void OnDeath()
    {
        // 播死亡動畫並停止移動；動畫結束後由動畫事件或計時 Destroy
        if (!string.IsNullOrEmpty(dieTrigger))
            anim.SetTrigger(dieTrigger);

        isActive = false;
        rb.velocity = Vector2.zero;
        // 若你想立即刪除，改回 base.OnDeath();
        // 這裡改為延遲銷毀，若你有動畫事件可在事件裡 Destroy(gameObject)
        Destroy(gameObject, 2.0f);
    }

    // ───────── Gizmos（除錯用） ─────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, shootRange);
        if (leftPoint && rightPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(leftPoint.position.x, baseAltitude, 0),
                            new Vector3(rightPoint.position.x, baseAltitude, 0));
        }
    }
}
