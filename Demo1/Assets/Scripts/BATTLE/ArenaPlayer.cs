using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArenaPlayerController : LivingEntity
{
    // ─────────────────────────────────────────────────────────
    // Singleton + 事件 + DDOL
    // ─────────────────────────────────────────────────────────
    public static ArenaPlayerController Instance { get; private set; }
    public static event System.Action<ArenaPlayerController> OnPlayerReady;

    private Rigidbody2D rb;
    public Animator anim;
    private Collider2D coll;
    public LayerMask wallLayer;

    // ☆ 這三個舊欄位保留，但實際計算一律走 PlayerBuffs
    public int attackseg = 0;
    public int defenceseg = 0;
    public int speedseg = 0;

    [SerializeField] private string playerID = "player";

    // FSM
    public enum State { idle, jump, fall, hurt, dying };
    private State state = State.idle;

    // Inspector variable
    public LayerMask ground;

    [Header("角色數值（基礎值，Buff 會在此基礎上加成）")]
    public float jumpForce = 3f;
    public float hurtForce = 3f;

    public int speed = 5;
    public int attackDamage = 20;
    public int defence = 2;

    // ★ 有效數值：先用 PlayerBuffs，退而求其次用舊 seg 欄位
    public int curdefence => buffs ? buffs.CurDefence(defence) : defence + defenceseg * 10;
    public int curattack  => buffs ? buffs.CurAttack(attackDamage) : attackDamage + attackseg * 10;
    public int curspeed   => buffs ? buffs.CurSpeed(speed) : speed + speedseg * 20;

    [Header("UI")]
    public GameObject deathMenu;

    // ==========================================================
    // Animator Layer 管理
    // ==========================================================
    [Header("Animator Layer Control")]
    [Tooltip("請輸入 Animator Controller 中所有 Layer 的名稱 (如 Base Layer, CutsceneAnimation)。")]
    public string[] AnimatorLayerNames = { "Base Layer" }; // 預設 Base Layer

    [Tooltip("設定當前要啟用 (權重為 1) 的 Layer 名稱。")]
    [SerializeField] private string activeLayerName = "Base Layer";

    [Header("Arena Mode")]
    [Tooltip("在獨立競技場場景打勾，將關閉劇情/存檔/切場相關行為")]
    public bool arenaMode = false;

    [Header("Dash Settings")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 14f;        // 衝刺速度
    [SerializeField] private float dashDuration = 0.18f;   // 衝刺時間（基礎）
    [SerializeField] private float dashCooldown = 0.9f;    // 冷卻時間（基礎）
    [SerializeField] private string playerPhysicsLayer = "Player";
    [SerializeField] private string[] harmPhysicsLayers = new string[] { "Enemy" };

    private bool isDashing = false;
    private float lastDashTime = -999f;
    private int _playerLayer;
    private int[] _harmLayers;
    private float _origGravity;

    // ★ Buff 引用
    private PlayerBuffs buffs;

    // ─────────────────────────────────────────────────────────
    // 新增：跌落過低判定
    // ─────────────────────────────────────────────────────────
    [Header("Fall / Out-of-bounds")]
    [Tooltip("若玩家 Y 軸低於此值，會直接觸發死亡。")]
    [SerializeField] private float fallDeathY = -20f;
    // 防止在同一段時間內重複處理跌落事件
    private bool _fallenHandled = false;

    // ─────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────
    private void Awake()
    {
        // 單例防重複
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 常駐跨場景（競技場模式不常駐）
        if (!arenaMode)
            DontDestroyOnLoad(gameObject);

        // 保險：Tag = Player
        if (tag != "Player") tag = "Player";

        // 基本元件
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        InitDashLayerCache();

        // ★ Buff 取得（沒有就自動加上）
        buffs = GetComponent<PlayerBuffs>();
        if (!buffs) buffs = gameObject.AddComponent<PlayerBuffs>();
    }
    
    protected override void Start()
    {
        base.Start();

        // 確保初始 Layer 權重
        UpdateAnimatorLayerWeight();

        // 開場確保 i-frame 關閉（避免殘留）
        ToggleIFrames(false);
    }

    private void OnEnable()
    {
        if (!arenaMode)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        if (!arenaMode)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (arenaMode) return;
        StartCoroutine(RebindAfterSceneLoad());
    }

    // Dash：圖層快取初始化
    private void InitDashLayerCache()
    {
        _playerLayer = LayerMask.NameToLayer(playerPhysicsLayer);
        if (_playerLayer < 0)
            Debug.LogWarning($"[Dash] Player layer '{playerPhysicsLayer}' not found. Check Project Settings > Tags and Layers.");

        var list = new List<int>();
        foreach (var s in harmPhysicsLayers)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            int l = LayerMask.NameToLayer(s);
            list.Add(l);
        }
        _harmLayers = list.ToArray();
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        if (arenaMode) yield break;
        yield return null;
        yield return new WaitForEndOfFrame();

        TryMoveToSpawnPoint();
        OnPlayerReady?.Invoke(this);
    }

    // ─────────────────────────────────────────────────────────
    // Update / 互動
    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        // 死亡終止
        if (PlayerUtils.CheckDeath(healthBar))
        {
            state = State.dying;
            anim.SetInteger("state", (int)State.dying);
            Debug.Log("Player is dead!");
            if (rb) rb.velocity = Vector2.zero;
            this.enabled = false;
            if (deathMenu) deathMenu.SetActive(true);
            return;
        }

        // --- 跌落過低自動死亡檢查 ---
        if (!_fallenHandled && transform.position.y < fallDeathY)
        {
            _fallenHandled = true;

            // 直接觸發死亡（跳過 dash 無敵或其他短路）
            Die();

            // 如果你想要先播放 hurt 再死，可以改成呼叫 TakeDamage 或在這裡做額外處理
            // 例如：anim.SetTrigger("hurt"); StartCoroutine(DelayedDie(0.5f));
            return;
        }

        // Dash 輸入
        if (Input.GetKeyDown(dashKey))
            TryDash();

        if (!isDashing)
        {
            Movement();
            AnimationState();
            anim.SetInteger("state", (int)state);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectable"))
        {
            LootItem lootItem = collision.GetComponent<LootItem>();
            if (lootItem != null)
            {
                PlayerInventory.Instance.AddItem(lootItem.lootData);
                Destroy(collision.gameObject);
            }
        }
        else if (collision.CompareTag("trap"))
        {
            anim.SetTrigger("hurt");
            // ★ 受擊退力吃 Buff 衰減
            float kb = hurtForce * (buffs ? buffs.knockbackTakenMultiplier : 1f);
            PlayerUtils.ApplyKnockback(rb, kb, collision.transform, transform);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 死亡 / 復活
    // ─────────────────────────────────────────────────────────
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        // 設定跌落處理旗標，避免重複
        _fallenHandled = true;

        anim.SetTrigger("die");
        rb.velocity = Vector2.zero;
        this.enabled = false;

        if (arenaMode)
        {
            var arena = FindObjectOfType<ArenaManager>();
            if (arena != null) arena.OnPlayerDeath();
        }
        else
        {
            if (deathMenu != null) deathMenu.SetActive(true);
        }

        Debug.Log("玩家死亡 → 顯示死亡選單，不 Destroy 玩家物件");
    }

    public void RevivePlayer()
    {
        if (arenaMode)
        {
            var arena = FindObjectOfType<ArenaManager>();
            if (arena != null) arena.Restart();
            return;
        }

        if (anim == null) anim = GetComponent<Animator>();

        Debug.Log("RevivePlayer() 被執行！");

        healthBar.SetHealth(healthBar.maxHP);
        transform.position = Vector3.zero;
        state = State.idle;
        anim.SetInteger("state", (int)state);
        rb.velocity = Vector2.zero;

        this.enabled = true;

        if (deathMenu) deathMenu.SetActive(false);

        // 復活時重置跌落旗標
        _fallenHandled = false;

        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ★ 受傷：一次性護盾 + 減傷乘數
    public override void TakeDamage(float damage)
    {
        if (isDashing || isDead) return;

        // 單次護盾（來自 Buff）
        if (buffs && buffs.oneTimeShield)
        {
            buffs.oneTimeShield = false;
            anim.SetTrigger("hurt"); // 視覺回饋但不扣血
            return;
        }

        float mul = (buffs ? Mathf.Max(0.01f, buffs.damageTakenMultiplier) : 1f);
        float mitigatedDamage = Mathf.Max(0f, damage - curdefence);
        float finalDamage = mitigatedDamage * mul;
        base.TakeDamage(finalDamage);

        if (!isDead && finalDamage > 0f)
        {
            anim.SetTrigger("hurt");
        }
    }

    // ─────────────────────────────────────────────────────────
    // 出生點 / 場景定位
    // ─────────────────────────────────────────────────────────
    bool TryMoveToSpawnPoint()
    {
        if (arenaMode) return false;
        var gm = GameManager.instance ?? GameManager.I;
        if (gm == null) return false;

        gm.player = gameObject;

        string nextSpawnId = string.IsNullOrEmpty(gm.NextSpawnId) ? "default" : gm.NextSpawnId;
        PlayerSpawnPoint fallback = null;

        foreach (var point in FindObjectsOfType<PlayerSpawnPoint>())
        {
            if (point.spawnId == nextSpawnId)
            {
                transform.position = point.transform.position;
                gm.NextSpawnId = "default";
                return true;
            }

            if (fallback == null && point.spawnId == "default")
                fallback = point;
        }

        if (fallback != null)
        {
            transform.position = fallback.transform.position;
            gm.NextSpawnId = "default";
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────
    // 移動 / 動畫
    // ─────────────────────────────────────────────────────────
    public void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal");
        Vector3 scale = transform.localScale;

        bool touchingWallLeft  = Physics2D.Raycast(transform.position, Vector2.left,  0.6f, wallLayer);
        bool touchingWallRight = Physics2D.Raycast(transform.position, Vector2.right, 0.6f, wallLayer);

        bool movingIntoLeftWall  = hDirection < 0 && touchingWallLeft;
        bool movingIntoRightWall = hDirection > 0 && touchingWallRight;

        if (movingIntoLeftWall || movingIntoRightWall)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // ★ 速度吃 Buff 乘數
        float moveMul = (buffs ? Mathf.Max(0.1f, buffs.moveSpeedMultiplier) : 1f);
        float effectiveSpeed = curspeed * moveMul;

        if (hDirection < 0)
        {
            rb.velocity = new Vector2(-effectiveSpeed, rb.velocity.y);
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (hDirection > 0)
        {
            rb.velocity = new Vector2(effectiveSpeed, rb.velocity.y);
            scale.x =  Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        if (Input.GetButtonDown("Jump") && coll.IsTouchingLayers(ground))
        {
            jump();
        }

        float moveSpeed = Mathf.Abs(rb.velocity.x) / Mathf.Max(1f, effectiveSpeed);
        anim.SetFloat("speed", moveSpeed);
    }

    public void jump()
    {
        // ★ 跳躍力吃 Buff 加值
        float jBonus = (buffs ? buffs.jumpForceBonus : 0f);
        rb.velocity = new Vector2(rb.velocity.x, jumpForce + jBonus);
        state = State.jump;
    }

    public void AnimationState()
    {
        if (state == State.jump)
        {
            if (rb.velocity.y < .1f)
                state = State.fall;
        }
        else if (state == State.fall)
        {
            if (coll.IsTouchingLayers(ground))
                state = State.idle;
        }
        else if (state == State.dying)
        {
            state = State.dying;
        }
    }

    private void TryDash()
    {
        if (isDashing) return;

        // ★ 冷卻吃 Buff 乘數（下限避免 0）
        float cdMul = buffs ? Mathf.Max(0.2f, buffs.dashCooldownMultiplier) : 1f;
        if (Time.time < lastDashTime + (dashCooldown * cdMul)) return;

        StartCoroutine(DoDash());
    }

    // 執行衝刺協程
    private IEnumerator DoDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        ToggleIFrames(true);  // 開啟無敵

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        _origGravity = rb.gravityScale;
        rb.gravityScale = 0f;  // 關重力避免下墜

        float dir = transform.localScale.x >= 0 ? 1f : -1f;
        float distMul = (buffs ? Mathf.Max(0.1f, buffs.dashDistanceMultiplier) : 1f);
        rb.velocity = new Vector2(dashSpeed * distMul * dir, 0f);

        // ★ 時長吃 Buff 加秒
        float extra = buffs ? Mathf.Max(0f, buffs.dashDurationBonus) : 0f;
        yield return new WaitForSeconds(dashDuration + extra);

        // 恢復
        rb.gravityScale = _origGravity;
        rb.velocity = Vector2.zero;
        ToggleIFrames(false);
        isDashing = false;
    }

    // 無敵開關（Dash / 受擊 i-frame 共用）
    private void ToggleIFrames(bool on)
    {
        if (_playerLayer < 0) return;
        for (int i = 0; i < _harmLayers.Length; i++)
        {
            if (_harmLayers[i] < 0) continue;
            Physics2D.IgnoreLayerCollision(_playerLayer, _harmLayers[i], on);
        }
    }

    // 公開屬性：切換 Animator Layer
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

    /// <summary>
    /// 根據 ActiveLayerName 的值，設定該 Layer 權重為 1，其餘為 0。
    /// </summary>
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
            if (layerName == ActiveLayerName)
                anim.SetLayerWeight(i, 1f);
            else
                anim.SetLayerWeight(i, 0f);
        }
    }

    // 給動畫模式用的主角轉向
    public void FaceLeft()
    {
        Vector3 s = transform.localScale;
        s.x = -Mathf.Abs(s.x);
        transform.localScale = s;
    }
}
