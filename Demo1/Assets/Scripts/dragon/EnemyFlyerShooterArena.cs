using UnityEngine;

/// <summary>
/// 空中巡航 → 射擊 N 次 → 降落（以 Ground Layer 觸地判定）→ 地面發呆 → 起飛回空巡
/// Animator：idle(Loop) + flyattack(Trigger)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
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
    public float baseAltitude = 5f;         // 巡航目標高度(Y)（相對於降落點）
    public float sinAmp = 0.6f;
    public float sinFreq = 1.2f;
    public float perlinAmp = 0.3f;
    public float perlinFreq = 0.4f;
    public float turnSmooth = 8f;           // 轉向平滑

    [Header("Landing / Idle / Takeoff")]
    public LayerMask groundMask;
    public float descendSpeed = 6f;
    public float ascendSpeed  = 6f;
    public float groundIdleDuration = 2.2f; // 地面停留秒數
    public float landOffset = 0.2f;         // 角色底部與地面的保險距

    [Header("Animator Triggers")]
    public string flyAttackTrigger = "flyattack";  // Animator trigger 名稱
    public string hurtTrigger = "hurt";            // 可選
    public string dieTrigger  = "die";             // 可選

    [Header("Control")]
    public bool requireLineOfSight = true;
    public bool facePlayer = true;
    public bool isActive = true;

    // ───────── Takeoff 安全參數（新增） ─────────
    [Header("Takeoff Safety")]
    [SerializeField] private float ceilingMargin = 0.25f;  // 與天花板保留距離
    [SerializeField] private float maxTakeoffTime = 3.0f;  // 起飛最長嘗試秒數（保底）

    // runtime
    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D coll;

    private float leftCap, rightCap;
    private int horizDir = 1;               // 1:右 / -1:左
    private float perlinSeed;
    private float nextShootTime;
    private int shotCount;
    private Vector2 landingPoint;           // 降落時的地面參考（由觸地時計算）
    private bool inAttackAnim;
    private float cruiseBaseY; 

    // 地面/起飛輔助旗標
    private bool hasLanded = false;
    private float groundIdleRemain = 0f;
    private bool enteredGroundIdle = false;
    private bool enteredTakeoff = false;
    private float takeoffTimer = 0f;

    private enum State { AirPatrol, Descend, GroundIdle, Takeoff }
    private State state = State.AirPatrol;

    // ───────── Unity ─────────
    protected void Awake()
    {
        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();

        rb.gravityScale = 0f;               // 飛行由腳本控制
        perlinSeed = Random.Range(0f, 1000f);
    }

    // LivingEntity 有 virtual Start()，這裡要 override 並呼叫 base
    protected override void Start()
    {
        base.Start();

        if (player == null)
        {
            var apc = FindObjectOfType<ArenaPlayerController>();
            if (apc) player = apc.transform;
            else
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc) player = pc.transform;
                else
                {
                    var go = GameObject.FindGameObjectWithTag("Player");
                    if (go) player = go.transform;
                }
            }
        }

        // 巡邏邊界
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
        cruiseBaseY = transform.position.y;
    }

    void Update()
    {
        if (!isActive || isDead) { rb.velocity = Vector2.zero; return; }
        if (!player) return;

        // 面向處理
        if (facePlayer)
        {
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
        // Animator 維持 idle 迴圈；觸發攻擊時用 trigger
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
        float targetY = cruiseBaseY + baseAltitude + sinY + noiseY;

        float vy = Mathf.Clamp((targetY - transform.position.y) * 6f, -ascendSpeed, ascendSpeed);
        rb.velocity = new Vector2(vx, vy);
    }

    void TryShootInAir()
    {
        if (state != State.AirPatrol) return; // 只在空巡時射擊

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
        // 單純往下掉；是否到地面，交給 OnTrigger/OnCollision 事件處理
        if (!hasLanded)
        {
            Vector2 v = new Vector2(Mathf.Lerp(rb.velocity.x, 0f, Time.deltaTime * 4f), -descendSpeed);
            rb.velocity = v;
        }
        else
        {
            rb.velocity = Vector2.zero; // 已觸地，確保速度歸零
        }
    }

    void DoGroundIdle()
    {
        rb.velocity = Vector2.zero;

        if (!enteredGroundIdle)
        {
            if (anim != null) anim.Play("idle"); // 入場播一次
            enteredGroundIdle = true;
            //Debug.Log("[Flyer] GroundIdle 進場，開始倒數");
        }

        // 用不受 timeScale 影響的時間流逝，避免暫停卡死
        groundIdleRemain -= Time.unscaledDeltaTime;

        if (groundIdleRemain <= 0f)
        {
            //Debug.Log("[Flyer] ⏰ 倒數完畢，開始起飛！");
            enteredTakeoff = false;
            state = State.Takeoff;
        }
    }

    void DoTakeoff()
    {
        if (!enteredTakeoff)
        {
            if (anim != null) anim.Play("idle"); // 入場播一次（避免每幀重置動畫）
            enteredTakeoff = true;
            takeoffTimer = 0f;
            //Debug.Log($"[Flyer] 起飛階段開始，landingY={landingPoint.y:F2}, baseAlt={baseAltitude:F2}");
        }

        // 目標高度：預設 = 降落點 + 基礎高度
        float desiredY = landingPoint.y + baseAltitude;

        // 天花板偵測：往上 Raycast，若很近就取「天花板下緣 - 邊距」
        float probeDist = Mathf.Max(baseAltitude + 2f, 8f);
        var hitUp = Physics2D.Raycast(transform.position, Vector2.up, probeDist, groundMask);
        if (hitUp.collider != null)
        {
            float ceilingY = hitUp.point.y;
            desiredY = Mathf.Min(desiredY, ceilingY - ceilingMargin);
        }

        // 推進速度：水平回到巡航、垂直朝上加速
        float vy = Mathf.Lerp(rb.velocity.y, ascendSpeed, Time.deltaTime * 2f);
        float vx = Mathf.Lerp(rb.velocity.x, horizDir * cruiseSpeed, Time.deltaTime * 2f);
        rb.velocity = new Vector2(vx, vy);

        // 到達判定（寬鬆一些）
        if (transform.position.y >= desiredY - 0.05f)
        {
            //Debug.Log("[Flyer] 🌀 到達(可行)巡航高度，回到 AirPatrol！");
            rb.velocity = new Vector2(horizDir * cruiseSpeed, 0f);
            shotCount = 0;
            nextShootTime = Time.time + 0.4f;
            cruiseBaseY = landingPoint.y;
            state = State.AirPatrol;

            // 重設旗標
            hasLanded = false;
            landingPoint = Vector2.zero;
            enteredGroundIdle = false;
            enteredTakeoff   = false;
            takeoffTimer = 0f;
            return;
        }

        // 超時保底：例如 3 秒內還上不去，直接恢復空巡（以當前高度為準）
        takeoffTimer += Time.unscaledDeltaTime; // 不受暫停影響
        if (takeoffTimer >= maxTakeoffTime)
        {
            Debug.LogWarning("[Flyer] ⚠ 起飛超時，強制回到 AirPatrol（可能被天花板或碰撞卡住）");
            rb.velocity = new Vector2(horizDir * cruiseSpeed, 0f);
            shotCount = 0;
            nextShootTime = Time.time + 0.4f;
            state = State.AirPatrol;

            hasLanded = false;
            landingPoint = Vector2.zero;
            enteredGroundIdle = false;
            enteredTakeoff   = false;
            takeoffTimer = 0f;
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
        if (!player || projectilePrefab == null) { inAttackAnim = false; return; }
        if (state != State.AirPatrol) { inAttackAnim = false; return; }

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
            hasLanded = false;
            // 嘗試做一次下射線（純除錯資訊，不作為判定）
            var hit = Physics2D.Raycast(transform.position, Vector2.down, 30f, groundMask);
            if (hit.collider != null)
            {
                var lp = hit.point + Vector2.up * landOffset;
                //Debug.Log($"[Flyer] 找到地面 {lp}");
            }
            else
            {
                //Debug.LogWarning("[Flyer] 找不到地面（除錯訊息），實際落地由碰撞事件判定");
            }
        }
    }

    public void AnimEvent_ShootEnd() { inAttackAnim = false; }

    // ───────── 觸地判定（核心改動） ─────────
    // 使用 Trigger 方式：讓 Flyer 的 Collider2D 設為 IsTrigger = true
    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != State.Descend) return;
        int mask = groundMask.value;
        if (((1 << other.gameObject.layer) & mask) == 0) return;

        // 以對方碰撞器的頂端當作地面高度
        float groundY = other.bounds.max.y;
        // 將本體中心對齊到：地面 + 自身半高 + 安全邊距
        float centerY = groundY + (coll ? coll.bounds.extents.y : 0f) + Mathf.Max(0f, landOffset - 0.001f);
        transform.position = new Vector3(transform.position.x, centerY, transform.position.z);

        rb.velocity = Vector2.zero;
        hasLanded = true;

        landingPoint = new Vector2(transform.position.x, groundY + Mathf.Max(0.0f, landOffset)); // 作為起飛基準
        //Debug.Log("[Flyer] ✅ 觸地成功，進入 GroundIdle");

        groundIdleRemain = groundIdleDuration;
        enteredGroundIdle = false;
        state = State.GroundIdle;
    }

    // 若你不使用 Trigger，而是實際碰撞（Collider IsTrigger=false），可用此法：
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Descend) return;
        int mask = groundMask.value;
        if (((1 << collision.gameObject.layer) & mask) == 0) return;

        // 取接觸點中最高的點當地面高度（以防斜坡）
        float groundY = float.NegativeInfinity;
        foreach (var cp in collision.contacts)
            groundY = Mathf.Max(groundY, cp.point.y);

        if (!float.IsNegativeInfinity(groundY))
        {
            float centerY = groundY + (coll ? coll.bounds.extents.y : 0f) + Mathf.Max(0f, landOffset - 0.001f);
            transform.position = new Vector3(transform.position.x, centerY, transform.position.z);
        }

        rb.velocity = Vector2.zero;
        hasLanded = true;

        landingPoint = new Vector2(transform.position.x, groundY + Mathf.Max(0.0f, landOffset));
        //Debug.Log("[Flyer] ✅ 碰撞觸地成功，進入 GroundIdle");

        groundIdleRemain = groundIdleDuration;
        enteredGroundIdle = false;
        state = State.GroundIdle;
    }

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
        Destroy(gameObject, 2.0f);
        // 若你想立即刪除，改回 base.OnDeath();
    }

    // ───────── Gizmos（除錯用） ─────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, shootRange);
        if (leftPoint && rightPoint)
        {
            Gizmos.color = Color.cyan;
            float y = (landingPoint == Vector2.zero ? transform.position.y : landingPoint.y) + baseAltitude;
            Gizmos.DrawLine(new Vector3(leftPoint.position.x, y, 0),
                            new Vector3(rightPoint.position.x, y, 0));
        }
    }
}
