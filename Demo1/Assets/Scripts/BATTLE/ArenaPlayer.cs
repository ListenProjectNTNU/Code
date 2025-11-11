using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class ArenaPlayerController : LivingEntity
{
    public static ArenaPlayerController Instance { get; private set; }

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;

    [Header("Layers")]
    public LayerMask ground;
    public LayerMask wallLayer;

    [Header("角色數值")]
    public float jumpForce = 3f;
    public float hurtForce = 3f;
    public int   speed     = 5;
    public int   attackDamage = 20;
    public int   defence      = 15;

    public int attackseg = 0, defenceseg = 0, speedseg = 0;
    public int curdefence => defence + defenceseg * 10;
    public int curattack  => attackDamage + attackseg * 10;
    public int curspeed   => speed + speedseg * 20;

    [Header("Animator Layer Control")]
    public string[] AnimatorLayerNames = { "Base Layer" };
    [SerializeField] private string activeLayerName = "Base Layer";

    public enum State { idle, jump, fall, hurt, dying };
    private State state = State.idle;

    // ───────── Dash ─────────
    [Header("Dash Settings")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.9f;

    [Tooltip("Player 的 Physics2D Layer 名稱")]
    [SerializeField] private string playerPhysicsLayer = "Player";
    [Tooltip("會對玩家造成傷害的 Layer（i-frame 時忽略）")]
    [SerializeField] private string[] harmPhysicsLayers = new string[] { "Enemy", "EnemyProjectile" };

    [Header("碰撞選項")]
    [Tooltip("若勾選：永遠忽略 Player 與 Enemy 的『身體碰撞』（建議用 Trigger 當攻擊判定）。")]
    public bool ignoreEnemyBodyCollision = false;
    [SerializeField] private string enemyBodyLayer = "Enemy"; // 與 harm 可相同

    private bool  isDashing = false;
    private float lastDashTime = -999f;
    private int   _playerLayer;
    private int[] _harmLayers;
    private int   _enemyBodyLayer = -1;
    private float _origGravity = 1f;
    private Coroutine dashCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;

        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        if (tag != "Player") tag = "Player";

        // ★ 防止把別人撞到旋轉／自己也不被扭：凍結 Z 旋轉
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        InitPhysicsLayerCache();

        // 可選：直接忽略 Player↔Enemy 之間的「實體碰撞」（保留 Trigger）
        if (ignoreEnemyBodyCollision && _playerLayer >= 0 && _enemyBodyLayer >= 0)
            Physics2D.IgnoreLayerCollision(_playerLayer, _enemyBodyLayer, true);
    }

    protected override void Start()
    {
        base.Start();
        UpdateAnimatorLayerWeight();
        ToggleIFrames(false);
        _origGravity = rb.gravityScale; // 記下初始重力
        if (_origGravity <= 0f) _origGravity = 1f; // 安全值
    }

    private void Update()
    {
        if (!enabled) return;

        // Dash 輸入
        if (Input.GetKeyDown(dashKey)) TryDash();

        // Dash 期間由協程控制位移；否則進行一般移動
        if (!isDashing)
        {
            Movement();
            AnimationState();
            anim.SetInteger("state", (int)state);
        }

        // ★ Dash 安全超時保險（避免因例外未回復重力而上飄）
        if (isDashing && Time.time > lastDashTime + dashDuration + 0.2f)
        {
            EndDashSafely();
        }

        // 掉出場景防呆
        if (transform.position.y < -20f)
        {
            Debug.Log("玩家掉落過低，重置位置並扣血！");
            transform.position = Vector3.zero;
            base.TakeDamage(9999);
        }
    }

    private void OnDisable()
    {
        // 腳本被停用時一定收尾 Dash
        EndDashSafely();
    }

    private void OnDestroy()
    {
        // 物件銷毀時也要保險收尾
        EndDashSafely();
        if (Instance == this)
            Instance = null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("trap"))
        {
            anim.SetTrigger("hurt");
            PlayerUtils.ApplyKnockback(rb, hurtForce, collision.transform, transform);
        }
    }

    // ───────── 戰鬥 / 受傷 / 死亡 ─────────
    public override void TakeDamage(float damage)
    {
        if (isDashing || isDead) return;
        base.TakeDamage(damage);
        if (!isDead) anim.SetTrigger("hurt");
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        EndDashSafely();
        anim.SetTrigger("die");
        if (rb) rb.velocity = Vector2.zero;
        this.enabled = false;

        var arena = FindObjectOfType<ArenaManager>();
        if (arena != null) arena.OnPlayerDeath();
        Debug.Log("【Arena】玩家死亡 → 交由 ArenaManager 結算/顯示 UI");
    }

    // ───────── 移動 / 跳躍 / 動畫 ─────────
    public void Movement()
    {
        float h = Input.GetAxis("Horizontal");
        Vector3 s = transform.localScale;

        bool hittingLeft  = Physics2D.Raycast(transform.position, Vector2.left,  0.6f, wallLayer);
        bool hittingRight = Physics2D.Raycast(transform.position, Vector2.right, 0.6f, wallLayer);
        bool intoLeft  = h < 0 && hittingLeft;
        bool intoRight = h > 0 && hittingRight;

        if (intoLeft || intoRight)
            rb.velocity = new Vector2(0, rb.velocity.y);

        if (h < 0)
        {
            rb.velocity = new Vector2(-curspeed, rb.velocity.y);
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }
        else if (h > 0)
        {
            rb.velocity = new Vector2(curspeed, rb.velocity.y);
            s.x =  Mathf.Abs(s.x);
            transform.localScale = s;
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        if (Input.GetButtonDown("Jump") && coll.IsTouchingLayers(ground))
            Jump();

        float moveSpeed = Mathf.Abs(rb.velocity.x) / Mathf.Max(1f, curspeed);
        anim.SetFloat("speed", moveSpeed);
    }

    public void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        state = State.jump;
    }

    public void AnimationState()
    {
        if (state == State.jump)
        {
            if (rb.velocity.y < .1f) state = State.fall;
        }
        else if (state == State.fall)
        {
            if (coll.IsTouchingLayers(ground)) state = State.idle;
        }
        else if (state == State.dying)
        {
            state = State.dying;
        }
    }

    // ───────── Dash（含 i-frame）─────────
    private void TryDash()
    {
        if (isDashing) return;
        if (Time.time < lastDashTime + dashCooldown) return;
        dashCo = StartCoroutine(DoDash());
    }

    private IEnumerator DoDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        ToggleIFrames(true);

        _origGravity = Mathf.Max(0.01f, rb.gravityScale);
        rb.gravityScale = 0f;

        float dir = transform.localScale.x >= 0 ? 1f : -1f;
        rb.velocity = new Vector2(dashSpeed * dir, 0f);

        yield return new WaitForSeconds(dashDuration);

        EndDashSafely(); // 正常收尾
    }

    private void EndDashSafely()
    {
        if (!isDashing)
        {
            // 仍確保重力正確
            if (rb != null && rb.gravityScale <= 0f) rb.gravityScale = Mathf.Max(1f, _origGravity);
            return;
        }

        isDashing = false;

        if (dashCo != null)
        {
            StopCoroutine(dashCo);
            dashCo = null;
        }

        if (rb != null)
        {
            rb.gravityScale = Mathf.Max(1f, _origGravity);
            // 不強制歸零速度，避免空中硬煞導致怪異；若需要可打開下一行
            // rb.velocity = new Vector2(rb.velocity.x * 0.6f, rb.velocity.y);
        }

        ToggleIFrames(false);
    }

    // ───────── Physics Layer / i-frames ─────────
    private void InitPhysicsLayerCache()
    {
        _playerLayer = LayerMask.NameToLayer(playerPhysicsLayer);
        if (_playerLayer < 0)
            Debug.LogWarning($"[Dash] Player layer '{playerPhysicsLayer}' not found. 請到 Project Settings > Tags and Layers。");

        var list = new List<int>();
        foreach (var s in harmPhysicsLayers)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            int l = LayerMask.NameToLayer(s);
            if (l >= 0) list.Add(l);
        }
        _harmLayers = list.ToArray();

        if (!string.IsNullOrEmpty(enemyBodyLayer))
            _enemyBodyLayer = LayerMask.NameToLayer(enemyBodyLayer);
    }

    private void ToggleIFrames(bool on)
    {
        if (_playerLayer < 0) return;
        for (int i = 0; i < _harmLayers.Length; i++)
        {
            if (_harmLayers[i] < 0) continue;
            Physics2D.IgnoreLayerCollision(_playerLayer, _harmLayers[i], on);
        }
    }

    // ───────── Animator Layer ─────────
    public string ActiveLayerName
    {
        get => activeLayerName;
        set
        {
            if (activeLayerName != value)
            {
                activeLayerName = value;
                UpdateAnimatorLayerWeight();
            }
        }
    }

    public void UpdateAnimatorLayerWeight()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogWarning("Animator is missing on " + gameObject.name);
                return;
            }
        }

        if (AnimatorLayerNames == null || AnimatorLayerNames.Length == 0)
        {
            Debug.LogWarning("AnimatorLayerNames is empty. Please define layers in the Inspector.");
            return;
        }

        for (int i = 0; i < anim.layerCount; i++)
        {
            string layerName = anim.GetLayerName(i);
            anim.SetLayerWeight(i, layerName == ActiveLayerName ? 1f : 0f);
        }
    }

    public void FaceLeft()
    {
        Vector3 s = transform.localScale;
        s.x = -Mathf.Abs(s.x);
        transform.localScale = s;
    }
}
