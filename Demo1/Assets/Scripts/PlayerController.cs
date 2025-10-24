using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : LivingEntity,IDataPersistence
{
    
    private Rigidbody2D rb;
    public Animator anim;
    private Collider2D coll;
    public LayerMask wallLayer;
    public int attackseg = 0;
    public int defenceseg = 0;
    public int speedseg = 0;
    //public Text cherryText;
    [SerializeField] private string playerID = "player";
    //FSM
    public enum State{idle,jump,fall,hurt,dying};
    private State state = State.idle;
    //Inspector variable
    public LayerMask ground;
    [Header("角色數值")]
    public float jumpForce = 3f;
    public float hurtForce = 3f;
    
    public int speed = 5 ;
    public int attackDamage = 20;
    public int defence = 15;

    public int curdefence => defence + defenceseg * 10;
    public int curattack => attackDamage + attackseg * 10;
    public int curspeed => speed + speedseg * 20;

    [Header("UI")]
    public GameObject deathMenu;
    // ==========================================================
    // 新增：Animator Layer 管理相關變數和屬性
    // ==========================================================
    [Header("Animator Layer Control")]
    [Tooltip("請輸入 Animator Controller 中所有 Layer 的名稱 (如 Base Layer, CutsceneAnimation)。")]
    public string[] AnimatorLayerNames = { "Base Layer" }; // 預設 Base Layer

    [Tooltip("設定當前要啟用 (權重為 1) 的 Layer 名稱。")]
    [SerializeField] 
    private string activeLayerName = "Base Layer"; 

    private void Start() {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();

        //確保初始 Layer (Base Layer) 的權重被設定為 1
        UpdateAnimatorLayerWeight();
    }

    // Update is called once per frame
    void Update()
    {
 
        Movement();
        AnimationState();
        anim.SetInteger("state", (int)state);//sets animation based on Enumerator state
        //Debug.Log((int)state);

        if (transform.position.y < -10) // 設定掉落的臨界點
        {
            ResetPlayerPosition(); // 重置玩家位置並扣血
        } 

        if (PlayerUtils.CheckDeath(healthBar))
        {
            state = State.dying;
            anim.SetInteger("state", (int)State.dying);
            Debug.Log("Player is dead!");
            rb.velocity = Vector2.zero;
            this.enabled = false;
            deathMenu.SetActive(true);
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
        else if (collision.tag == "trap")  // 檢測是否碰到敵人的 Hitbox
        {
            anim.SetTrigger("hurt");
            PlayerUtils.ApplyKnockback(rb, hurtForce, collision.transform, transform);  
        }
    }

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
    }

    //重生
    public void RevivePlayer()
    {
        if (anim == null) anim = GetComponent<Animator>();
        Debug.Log("RevivePlayer() 被執行！");
        Debug.Log("RevivePlayer() 被執行！重新載入場景！");
        healthBar.SetHealth(healthBar.maxHP);
        transform.position = new Vector3(0, 0, 0);
        state = State.idle;
        anim.SetInteger("state", (int)state);
        rb.velocity = Vector2.zero;

        // 重新啟動 PlayerController
        this.enabled = true;

        // 關閉死亡選單
        deathMenu.SetActive(false);
        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        
        anim.SetTrigger("die");
        rb.velocity = Vector2.zero;
        this.enabled = false;

        if (deathMenu != null)
            deathMenu.SetActive(true);

        Debug.Log("玩家死亡 → 顯示死亡選單，不 Destroy 玩家物件");
    }
    

    private void ResetPlayerPosition()
    {
        Debug.Log("玩家掉落過低，重置位置並扣血！");
        
        // 設定玩家回到原點
        transform.position = new Vector3(0, 0, 0);

        // 扣血
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
            // 播放受傷動畫
            anim.SetTrigger("hurt");

            // 這裡可選擇要不要 knockback，或交由觸發來源決定
        }
    }
    public void LoadData(GameData data)
    {
        this.transform.position = data.playerPosition;
        float loadedHP = data.GetHP(playerID, healthBar.maxHP);
        healthBar.SetHealth(loadedHP);

        speed = data.speed;
        attackDamage = data.attackDamage;
        defence = data.defence;

        attackseg = data.attackSeg;
        defenceseg = data.defenceSeg;
        speedseg = data.speedSeg;
    }

    public void SaveData(ref GameData data)
    {
        data.playerPosition = this.transform.position;

        data.SetHP(playerID, healthBar.currenthp);

        data.speed = speed;
        data.attackDamage = attackDamage;
        data.defence = defence;

        data.attackSeg = attackseg;
        data.defenceSeg = defenceseg;
        data.speedSeg = speedseg;
    }

    public void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal");
        Vector3 scale = transform.localScale;
        // 判斷是否碰牆
        bool touchingWallLeft = Physics2D.Raycast(transform.position, Vector2.left, 0.6f, wallLayer);
        bool touchingWallRight = Physics2D.Raycast(transform.position, Vector2.right, 0.6f, wallLayer);

        // 判斷是否在嘗試往牆上移動
        bool movingIntoLeftWall = hDirection < 0 && touchingWallLeft;
        bool movingIntoRightWall = hDirection > 0 && touchingWallRight;

        if (movingIntoLeftWall || movingIntoRightWall)
        {
            // 如果正在推牆，停止水平速度
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        if (hDirection < 0)
        {
            rb.velocity = new Vector2(-curspeed, rb.velocity.y);
            scale.x = -Mathf.Abs(scale.x);  // 保持原始縮放大小
            transform.localScale = scale;
        }
        else if (hDirection > 0)
        {
            rb.velocity = new Vector2(curspeed, rb.velocity.y);
            scale.x = Mathf.Abs(scale.x);   // 保持原始縮放大小
            transform.localScale = scale;
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // jump
        if (Input.GetButtonDown("Jump") && coll.IsTouchingLayers(ground))
        {
            jump();
        }
        // 設定 Blend Tree 的 speed
        float moveSpeed = Mathf.Abs(rb.velocity.x) / curspeed; // 正規化成 0~1
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
            // 死亡保持 dead 狀態
            state = State.dying;
        }
    }

    // Public Property 供其他腳本切換 Layer
    public string ActiveLayerName
    {
        get { return activeLayerName; }
        set
        {
            if (activeLayerName != value)
            {
                activeLayerName = value;
                UpdateAnimatorLayerWeight(); // 當名稱改變時，立即更新權重
            }
        }
    }

    // ==========================================================
    // 核心功能：更新 Animator Layer 權重
    // ==========================================================

    /// <summary>
    /// 根據 ActiveLayerName 的值，設定該 Layer 權重為 1，其餘為 0。
    /// 此方法應在 ActiveLayerName 改變時被呼叫。
    /// </summary>
    public void UpdateAnimatorLayerWeight()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>(); // 嘗試取得 Animator
            if (anim == null)
            {
                Debug.LogWarning("Animator is missing on " + gameObject.name);
                return;
            }
        }

        if (AnimatorLayerNames == null || AnimatorLayerNames.Length == 0)
        {
            // 至少應包含 "Base Layer"
            Debug.LogWarning("AnimatorLayerNames is empty. Please define layers in the Inspector.");
            return;
        }

        for (int i = 0; i < anim.layerCount; i++)
        {
            string layerName = anim.GetLayerName(i);
            
            if (layerName == ActiveLayerName)
            {
                // 啟用目標 Layer (權重設為 1)
                anim.SetLayerWeight(i, 1f);
                // Debug.Log($"Animator Layer: '{layerName}' set to Weight 1."); // 可選的 Log
            }
            else
            {
                // 禁用其他 Layer (權重設為 0)
                anim.SetLayerWeight(i, 0f);
            }
        }
    }
}