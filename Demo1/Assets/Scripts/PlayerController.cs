using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : LivingEntity, IDataPersistence
{
    // ─────────────────────────────────────────────────────────
    // Singleton + 事件 + DDOL
    // ─────────────────────────────────────────────────────────
    public static PlayerController Instance { get; private set; }
    public static event System.Action<PlayerController> OnPlayerReady;

    private Rigidbody2D rb;
    public Animator anim;
    private Collider2D coll;
    public LayerMask wallLayer;

    public int attackseg = 0;
    public int defenceseg = 0;
    public int speedseg = 0;

    [SerializeField] private string playerID = "player";

    // FSM
    public enum State { idle, jump, fall, hurt, dying };
    private State state = State.idle;

    // Inspector variable
    public LayerMask ground;

    [Header("角色數值")]
    public float jumpForce = 3f;
    public float hurtForce = 3f;

    public int speed = 5;
    public int attackDamage = 20;
    public int defence = 15;

    public int curdefence => defence + defenceseg * 10;
    public int curattack  => attackDamage + attackseg * 10;
    public int curspeed   => speed + speedseg * 20;

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

        // 常駐跨場景
        DontDestroyOnLoad(gameObject);

        // 保險：Tag = Player
        if (tag != "Player") tag = "Player";

        // 基本元件
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
    }

    protected override void Start()
    {
        base.Start();
        // 確保初始 Layer 權重
        UpdateAnimatorLayerWeight();
    }

    private void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 切到新場景後，等到下一幀 + 幀末再定位，最後廣播 OnPlayerReady
        StartCoroutine(RebindAfterSceneLoad());
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        // 等 1 幀：讓場景物件（SpawnPoint/相機/管理器）出現
        yield return null;
        // 再等到幀末：避免其他 OnEnable/Start 還在跑造成競態
        yield return new WaitForEndOfFrame();

        // 先用 GameManager.NextSpawnId 定位；找不到就保留現狀（之後如 LoadData 覆蓋）
        TryMoveToSpawnPoint();

        // 廣播「玩家就緒」給相機/場景控制器
        OnPlayerReady?.Invoke(this);
    }

    // ─────────────────────────────────────────────────────────
    // Update / 互動
    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        Movement();
        AnimationState();
        anim.SetInteger("state", (int)state);

        if (transform.position.y < -10)
        {
            ResetPlayerPosition();
        }

        if (PlayerUtils.CheckDeath(healthBar))
        {
            state = State.dying;
            anim.SetInteger("state", (int)State.dying);
            Debug.Log("Player is dead!");
            rb.velocity = Vector2.zero;
            this.enabled = false;
            if (deathMenu) deathMenu.SetActive(true);
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
            PlayerUtils.ApplyKnockback(rb, hurtForce, collision.transform, transform);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 死亡 / 復活
    // ─────────────────────────────────────────────────────────
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("die");
        rb.velocity = Vector2.zero;
        this.enabled = false;

        if (deathMenu != null) deathMenu.SetActive(true);

        Debug.Log("玩家死亡 → 顯示死亡選單，不 Destroy 玩家物件");
    }

    // 同場景復活
    public void RevivePlayer()
    {
        if (anim == null) anim = GetComponent<Animator>();

        Debug.Log("RevivePlayer() 被執行！");

        healthBar.SetHealth(healthBar.maxHP);
        transform.position = Vector3.zero;
        state = State.idle;
        anim.SetInteger("state", (int)state);
        rb.velocity = Vector2.zero;

        this.enabled = true;

        if (deathMenu) deathMenu.SetActive(false);

        // 若你有資料管理器要同步 UI/狀態，可用現成流程
        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetPlayerPosition()
    {
        Debug.Log("玩家掉落過低，重置位置並扣血！");
        transform.position = Vector3.zero;

        if (healthBar != null)
        {
            base.TakeDamage(9999);
        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (!isDead)
        {
            anim.SetTrigger("hurt");
        }
    }

    // ─────────────────────────────────────────────────────────
    // 存讀
    // ─────────────────────────────────────────────────────────
    public void LoadData(GameData data)
    {
        // 對齊到目前場景，避免用到舊的 sceneName
        data.sceneName = SceneManager.GetActiveScene().name;

        if (!TryMoveToSpawnPoint())
        {
            string sceneName = data.sceneName;
            if (data.TryGetPlayerPosition(sceneName, out var savedPosition))
            {
                transform.position = savedPosition;
            }
        }

        float loadedHP = data.GetHP(data.sceneName, playerID, healthBar.maxHP);
        healthBar.SetHealth(loadedHP);

        speed        = data.speed;
        attackDamage = data.attackDamage;
        defence      = data.defence;

        attackseg  = data.attackSeg;
        defenceseg = data.defenceSeg;
        speedseg   = data.speedSeg;

        // 讀檔可能改變了位置 → 再廣播一次，讓相機/SC 跟到最終位置
        OnPlayerReady?.Invoke(this);
    }

    public void SaveData(ref GameData data)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        data.sceneName = sceneName;

        data.SetPlayerPosition(sceneName, transform.position);

        if (data.sceneName == sceneName)
            data.playerPosition = transform.position;

        data.SetHP(data.sceneName, playerID, healthBar.currenthp);

        data.speed        = speed;
        data.attackDamage = attackDamage;
        data.defence      = defence;

        data.attackSeg  = attackseg;
        data.defenceSeg = defenceseg;
        data.speedSeg   = speedseg;
    }

    // ─────────────────────────────────────────────────────────
    // 出生點 / 場景定位
    // ─────────────────────────────────────────────────────────
    bool TryMoveToSpawnPoint()
    {
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

        if (hDirection < 0)
        {
            rb.velocity = new Vector2(-curspeed, rb.velocity.y);
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (hDirection > 0)
        {
            rb.velocity = new Vector2(curspeed, rb.velocity.y);
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

        float moveSpeed = Mathf.Abs(rb.velocity.x) / Mathf.Max(1f, curspeed);
        anim.SetFloat("speed", moveSpeed);
    }

    public void jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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

    // ─────────────────────────────────────────────────────────
    //（可選）切場輔助：存檔 + 指定下一出生點 + 切場
    // ─────────────────────────────────────────────────────────
    public static void GoToScene(string sceneName, string nextSpawnId = "default")
    {
        var gm = GameManager.instance ?? GameManager.I;
        if (gm != null) gm.NextSpawnId = string.IsNullOrEmpty(nextSpawnId) ? "default" : nextSpawnId;

        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.SaveGame();

        SceneManager.LoadScene(sceneName);
    }
}
