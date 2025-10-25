using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossController : LivingEntity
{
    [Header("Refs")]
    public Transform player;
    public Collider2D attackHitbox;      // 建議設成 IsTrigger
    public LayerMask groundMask;         // 僅勾地面/平台圖層

    [Header("Stats")]
    public int contactDamage = 25;
    public float moveSpeed = 2.5f;
    public float chaseRange = 15f;
    public float attackRangeX = 3.5f;    // 與玩家 X 軸距離門檻
    public float attackCooldown = 3f;

    [Header("Drop Attack Tunings")]
    public float riseHeightAbovePlayer = 4.0f; // 無天花板時的上方高度基準
    public float dropSpeed = 20f;              // 下砸初速度（向下）
    public float gravityDuringDrop = 5f;       // 下砸時重力（暫時覆蓋 rb.gravityScale）
    public float postLandPause = 0.35f;        // 落地後僵直

    [Header("Teleport & Telegraph")]
    public float ceilingProbeDistance = 1f;    // 往上找天花板距離（視場景高度可調大到 8~12）
    public float ceilingClearance = 0.2f;      // 與天花板保留的縫
    public float telegraphTime = 10f;          // 瞬移到位後「預告」等待時間
    public bool  freezeXDuringDrop = true;     // 下砸期間鎖住 X

    [Header("Teleport Collision Safety")]
    public LayerMask playerMask;               // 只勾 Player 的 Layer
    public float headClearance = 0.35f;        // 玩家頭頂與 Boss 腳底要留的安全距離
    public float sideOffset = 1.2f;            // 正上方不行時，左右嘗試的水平偏移量
    public int sideSweepSteps = 2;             // 左右各試幾格（1~3）

    [Header("Ground Check (Optional)")]
    public float groundCheckRadius = 0.15f;
    public Transform groundCheck;

    [Header("Misc")]
    [SerializeField] float hurtStun = 0.5f;
    [SerializeField] bool disableColliderOnDeath = true;

    // runtime
    Rigidbody2D rb;
    Animator anim;
    Collider2D col;
    float nextAttackTime;
    bool isHurting;
    bool isAttacking;

    // 落地旗標（由碰撞事件或偵測設定）
    bool landed;
    bool inTelegraph;
    Vector2 telegraphAnchor;

    // 保存原始剛體限制，方便恢復
    RigidbodyConstraints2D originalConstraints;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (attackHitbox) attackHitbox.enabled = false;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        originalConstraints = rb.constraints; // 例如通常會有 FreezeRotation
    }

    void Update()
    {
        if (isDead) return;

        // 偵錯
        float dx = player ? player.position.x - transform.position.x : 999f;
        float ax = Mathf.Abs(dx);
        bool g = IsGrounded();
        bool cd = Time.time >= nextAttackTime;
        if (Time.frameCount % 20 == 0)
            Debug.Log($"[BossDebug] ax={ax:F2} grounded={g} cd={cd} isAttacking={isAttacking}");

        // Animator 參數
        anim.SetFloat("SpeedX", Mathf.Abs(rb.velocity.x));
        int moveDir = rb.velocity.x > 0.05f ? 1 : (rb.velocity.x < -0.05f ? -1 : (transform.localScale.x >= 0 ? 1 : -1));
        anim.SetInteger("MoveDir", moveDir);

        if (isAttacking || player == null) return;

        // 追擊
        if (Vector2.Distance(player.position, transform.position) <= chaseRange)
        {
            int dir = dx > 0 ? 1 : -1;
            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
            FaceTowards(player.position.x);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // 攻擊判定（確認 OK 後想再加上 g 也行）
        if (ax <= attackRangeX && cd /* && g */)
        {
            StartCoroutine(Co_DropAttack());
        }
    }

    IEnumerator Co_DropAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;
        anim.SetTrigger("DoAttack");

        // 1) 暫停物理，安全瞬移
        bool oldSim = rb.simulated;
        rb.simulated = false;

        // 2) 計算瞬移點（安全版：避免與玩家/地形重疊，必要時左右偏移）
        Vector2 tp = GetTeleportPointAbovePlayer();

        // 3) 瞬移到位（關主體碰撞器再傳送，避免瞬間觸發）
        bool colWasEnabled = col.enabled;
        col.enabled = false;
        transform.position = tp;
        col.enabled = colWasEnabled;

        // 4) Telegraph：鎖死位置，給玩家反應時間
        telegraphAnchor = transform.position;
        inTelegraph = true;

        yield return new WaitForSeconds(telegraphTime);

        // 開始下砸前解除 telegraph 鎖
        inTelegraph = false;

        // 5) 恢復物理，開始垂直下砸（鎖 X，直直往下）
        rb.simulated = oldSim;
        float oldGravity = rb.gravityScale;
        rb.gravityScale = gravityDuringDrop;

        if (freezeXDuringDrop)
        {
            originalConstraints = rb.constraints;
            rb.constraints = originalConstraints | RigidbodyConstraints2D.FreezePositionX;
            rb.velocity = Vector2.zero; // 確保一開始 X=0
        }

        // 垂直向下
        rb.velocity = new Vector2(0f, -Mathf.Abs(dropSpeed));

        if (attackHitbox) attackHitbox.enabled = true;

        // 6) 等到落地（碰撞事件 + 容錯）
        yield return WaitForLanding();

        // 7) 收尾：關 Hitbox、恢復重力與剛體限制、清空速度、落地硬直
        if (attackHitbox) attackHitbox.enabled = false;
        rb.gravityScale = oldGravity;

        // 恢復原本剛體限制
        if (freezeXDuringDrop)
            rb.constraints = originalConstraints;

        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(postLandPause);

        // 8) 冷卻與結束攻擊
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    void LateUpdate()
    {
        // Telegraph 期間，每幀強制把位置拉回錨點，避免任何動畫/其他腳本造成抖動
        if (inTelegraph)
            transform.position = telegraphAnchor;

        // 測試鍵：手動觸發
        if (Input.GetKeyDown(KeyCode.T))
            StartCoroutine(Co_DropAttack());
    }

    // === 工具方法 ===
    void FaceTowards(float targetX)
    {
        var s = transform.localScale;
        s.x = targetX >= transform.position.x ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // —— 安全版：取得玩家頭上可用的瞬移位置（帶側向補位） ——
    Vector2 GetTeleportPointAbovePlayer()
    {
        if (!col) col = GetComponent<Collider2D>();

        // 取得玩家 Collider 與頭頂高度
        var pCol = player ? player.GetComponent<Collider2D>() : null;
        float playerTopY = pCol ? pCol.bounds.max.y : player.position.y;

        // Boss 尺寸（用目前主體 Collider 尺寸）
        Bounds bossB = col.bounds;
        float bossHalfH = bossB.extents.y;
        float bossHalfW = bossB.extents.x;

        // 理想「正上方」中心 Y：玩家頭頂 + 安全距離 + Boss 半高
        float desiredCenterY = playerTopY + headClearance + bossHalfH;

        // 往上找天花板，把 Y 限制在天花板下方
        Vector2 upOrigin = new Vector2(player.position.x, playerTopY);
        RaycastHit2D ceil = Physics2D.Raycast(upOrigin, Vector2.up, ceilingProbeDistance, groundMask);

        // 在天花板下方能放的最高中心 Y
        float maxCenterY = (ceil.collider != null)
            ? ceil.point.y - ceilingClearance - bossHalfH
            : player.position.y + riseHeightAbovePlayer;

        // 取能放得下的實際中心 Y（至少 desired，不超過 max）
        float centerY = Mathf.Min(Mathf.Max(desiredCenterY, player.position.y + riseHeightAbovePlayer), maxCenterY);

        // 產生候選點：正上方 → 右 → 左 → 更右 → 更左 ...
        Vector2[] candidateCenters = BuildCandidateCenters(player.position, centerY, bossHalfW);

        // 逐一測試「此位置是否與玩家/地形重疊」，選第一個可用的
        foreach (var c in candidateCenters)
        {
            if (IsPlacementClear(c, bossB.size, playerMask) && IsPlacementClear(c, bossB.size, groundMask))
                return c;
        }

        // 如果全都不清，退而求其次：回傳正上方（至少不會拋例外）
        return new Vector2(player.position.x, centerY);
    }

    // 產生候選中心點：正上方 → 左右掃描
    Vector2[] BuildCandidateCenters(Vector3 playerPos, float centerY, float bossHalfW)
    {
        int count = 1 + sideSweepSteps * 2;
        Vector2[] arr = new Vector2[count];
        arr[0] = new Vector2(playerPos.x, centerY);

        int idx = 1;
        for (int s = 1; s <= sideSweepSteps; s++)
        {
            float dx = sideOffset * s + bossHalfW * 0.2f; // 讓側移量略大於半寬，避免邊緣相切
            arr[idx++] = new Vector2(playerPos.x + dx, centerY); // 右
            arr[idx++] = new Vector2(playerPos.x - dx, centerY); // 左
        }
        return arr;
    }

    // 檢查以「Boss 大小」放在 center 時，是否會與 mask 中任何 Collider 重疊
    bool IsPlacementClear(Vector2 center, Vector2 size, LayerMask mask)
    {
        var hit = Physics2D.OverlapBox(center, size * 0.98f, 0f, mask); // 0.98f 給一點鬆緊
        return hit == null;
    }

    // —— 穩定版地面偵測（BoxCast + IsTouchingLayers） ——
    bool IsGrounded()
    {
        if (!col) col = GetComponent<Collider2D>();

        Bounds b = col.bounds;
        Vector2 boxSize   = new Vector2(b.size.x * 0.9f, 0.08f);
        Vector2 boxCenter = new Vector2(b.center.x, b.min.y + boxSize.y * 0.5f + 0.01f);
        float   castDist  = 0.35f;

        Debug.DrawRay(new Vector2(b.center.x, b.min.y), Vector2.down * castDist, Color.green, 0.05f);

        var hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, castDist, groundMask);
        if (hit.collider) return true;

        return col.IsTouchingLayers(groundMask);
    }

    // ==== 傷害 / 死亡：覆寫基底 ====
    public override void TakeDamage(float damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

        if (!isDead && !isHurting)
            StartCoroutine(Co_HurtStagger());
    }

    protected override void OnDeath()
    {
        CancelInvoke();
        StopAllCoroutines();

        if (attackHitbox) attackHitbox.enabled = false;

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        if (disableColliderOnDeath && col) col.enabled = false;

        anim.SetBool("IsDying", true);
    }

    IEnumerator Co_HurtStagger()
    {
        isHurting = true;
        anim.SetTrigger("hurt");
        rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(hurtStun);
        isHurting = false;
    }

    // —— 等待落地：由碰撞事件主導，附帶逾時 & 速度近零容錯 ——
    IEnumerator WaitForLanding()
    {
        landed = false;

        float start   = Time.time;
        float timeout = 1.8f;
        int nearZeroFrames = 0;

        while (Time.time - start < timeout)
        {
            if (landed) break;         // 碰撞事件已告知落地
            if (IsGrounded()) break;   // 次要依據：感測到地面

            float vy = rb.velocity.y;
            if (Mathf.Abs(vy) < 0.05f) nearZeroFrames++;
            else nearZeroFrames = 0;

            if (nearZeroFrames >= 3) break;

            yield return new WaitForFixedUpdate();
        }
    }

    // —— 透過碰撞事件判定「真的落地」 ——
    void OnCollisionEnter2D(Collision2D c)
    {
        if (rb.velocity.y <= 0f && LayerInMask(c.collider.gameObject.layer, groundMask))
        {
            landed = true;
            // Debug.Log($"[Boss] Landed on {c.collider.name}");
        }
    }
    void OnCollisionStay2D(Collision2D c)
    {
        if (rb.velocity.y <= 0f && LayerInMask(c.collider.gameObject.layer, groundMask))
            landed = true;
    }

    // 小工具：層是否在遮罩內
    bool LayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // 可視化
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
