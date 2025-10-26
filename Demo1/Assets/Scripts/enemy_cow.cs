using System.Collections;
using UnityEngine;

public class enemy_cow : LivingEntity, IDataPersistence
{
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;  
    [SerializeField] private string enemyID = "cow1";
    private Quaternion fixedRotation;
    public LayerMask ground;
    public float edgeCheckDistance = 1f;
    
    private bool facingLeft = true;
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;
    public GameObject hitbox;

    private enum State { idle, attack, hurt, dying, run };
    private State state = State.idle;
    private Vector3 originalScale;
    private float leftCap;
    private float rightCap;

    public Transform player;  
    public Vector3 attackOffset;
    public LayerMask attackMask;
    public float attackRange = 3f;  
    public float attackCooldown = 10f;   // ⚡ 建議設 ≥ 攻擊動畫時長
    private float nextAttackTime = 0f;  
    public float chaseRange = 6f;
    public float stopChaseRange = 10f;
    private bool isChasing = false;

    // ⚡ 攻擊鎖定
    private bool isAttacking = false;
    private bool hasDealtDamage = false;

    [Header("非自由模式控制用")]
    public bool isActive = true;

    // 🔹 新增：SceneController 控制相關
    [Header("SceneController 控制")]
    public bool controlledBySC = false; // 是否由 SC 控制
    private bool canMove = true;         // SC 控制的移動開關

    [Header("Loot")]
    private bool lootDropped = false; // ✅ 防止重複掉落

    // 🔹 SC 控制用介面
    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove)
        {
            rb.velocity = Vector2.zero;
            anim.SetInteger("state", (int)State.idle);
        }
    }

    protected override void Start()
    {
        base.Start(); // LivingEntity 初始化血量
        fixedRotation = transform.rotation;
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        leftCap = leftPoint.position.x;
        rightCap = rightPoint.position.x;
        if (hitbox != null) hitbox.SetActive(false);
    }

    private void Update()
    {
        transform.rotation = fixedRotation;
        if (isDead) return;

        anim.SetInteger("state", (int)state);
        AnimationState(); // ✅ 始終呼叫 AnimationState，讓巡邏、移動判斷正常

        // 🔹 SC 控制：若由 SC 控制且暫停移動，就不做追擊攻擊判斷
        if (controlledBySC && !canMove) return;

        // 以下只在 SC 控制的敵人上運作
        if (!isActive) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        CheckAttack(distanceToPlayer);
    }

    // 🔹 將攻擊與追擊判斷拆出
    private void CheckAttack(float distanceToPlayer)
    {
        if (state != State.attack) // 正在攻擊時不改 isChasing
        {
            if (distanceToPlayer <= chaseRange)
                isChasing = true;
            else if (distanceToPlayer >= stopChaseRange)
                isChasing = false;
        }
        else
        {
            // 🔥 如果正在攻擊但玩家已經離開範圍，強制回 Idle
            if (distanceToPlayer > chaseRange)
            {
                isAttacking = false;
                state = State.idle;
                anim.ResetTrigger("attack"); // 避免動畫繼續播
                Debug.Log("[Update] 玩家離開範圍 → 中斷攻擊，回到 Idle");
            }
        }

        // 攻擊判斷
        if (isChasing && !isAttacking && distanceToPlayer <= attackRange &&
            Time.time >= nextAttackTime && state != State.attack)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (isAttacking) return; // 🚫 避免連續攻擊

        isAttacking = true; 
        hasDealtDamage = false; // ✅ 新一輪攻擊，重置傷害狀態
        state = State.attack;
        anim.SetTrigger("attack");
        Debug.Log("[Attack] 設定 trigger → attack");

        nextAttackTime = Time.time + attackCooldown;

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null && !hasDealtDamage) // ✅ 加鎖
        {
            Debug.Log($"[Attack] 檢測到物件: {colInfo.name}");
            LivingEntity target = colInfo.GetComponent<LivingEntity>();
            if (target != null)
            {
                target.TakeDamage(20);
                hasDealtDamage = true; // ✅ 這次攻擊已經生效
                Debug.Log("[Attack] 成功對玩家造成傷害！");
            }
        }
        else if (colInfo == null)
        {
            Debug.Log("[Attack] 攻擊範圍內沒有檢測到任何目標");
        }
    }

    // 🔥 在攻擊動畫最後一幀加 Animation Event 呼叫這個
    public void OnAttackAnimationEnd()
    {
        isAttacking = false; // ✅ 解鎖，允許下一次攻擊
        state = isChasing ? State.run : State.idle;
        Debug.Log("[Attack] 攻擊動畫結束 → 回到 " + state);
    }

    private void Move()
    {
        float moveSpeed = 2f;

        // 🔹 SC 控制判斷：若受 SC 控制且暫停移動，直接 return
        if (controlledBySC && !canMove) return;

        if (isChasing) // SC 控制：追擊玩家
        {
            state = State.run;
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

            if (direction.x < 0)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
            else
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y);
        }
        else // 巡邏邏輯
        {
            // 🔹 先檢查是否到達巡邏邊界
            if (facingLeft)
            {
                if (transform.position.x > leftCap)
                {
                    rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
                    transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
                }
                else
                {
                    facingLeft = false; // 反向
                }
            }
            else
            {
                if (transform.position.x < rightCap)
                {
                    rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
                    transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y);
                }
                else
                {
                    facingLeft = true; // 反向
                }
            }
        }
    }

    private void AnimationState()
    {
        if (state == State.hurt || state == State.dying || state == State.attack)
            return;

        Move(); 
        
        if (isChasing)
            state = State.run;
        else
            state = State.idle;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage); 

        if (!isDead)
        {
            state = State.hurt;
            Invoke(nameof(ResetToIdle), 0.5f);
        }
        if (currentHealth <= 0)
        {
            anim.SetTrigger("die");
        }
    }

    public void SetState(int s)
    {
        state = (State)s;  
        if (anim != null)
            anim.SetInteger("state", (int)state); 
    }

    protected override void Die()
    {
        if (isDead) return;   // ✅ 保險：再檢查一次
        isDead = true;

        anim.ResetTrigger("attack");
        anim.SetTrigger("die");
        state = State.dying;

        rb.velocity = Vector2.zero;
        rb.simulated = false;
        coll.enabled = false;
        if (hitbox) hitbox.SetActive(false);

        base.Die(); // ✅ 通知 LivingEntity & 廣播事件
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 等進入動畫狀態
        Debug.Log("DeathSequence()");
        OnDeathAnimationEnd();
        yield return null;
        float timeout = 5f; // ⏱️ 最長等待 5 秒避免死循環

        while (!anim.GetCurrentAnimatorStateInfo(0).IsName("dying") && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 播放完動畫
        while (anim.GetCurrentAnimatorStateInfo(0).IsName("dying") &&
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.99f &&
            timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        //這裡的程式不會被執行到
        Debug.Log("OnDeathAnimationEnd()");
        OnDeathAnimationEnd();
        DropLootAndDestroy();
    }

    public void OnDeathAnimationEnd()
    {
        Debug.Log("OnDeathAnimationEnd()");
        DropLootAndDestroy();
    }

    private void DropLootAndDestroy()
    {
        if (lootDropped) return; // ✅ 防止重複
        lootDropped = true;

        var loot = GetComponent<LootBag>();
        if (loot != null)
        {
            loot.InstantiateLoot(transform.position);
            Debug.Log($"[enemy_cow] 掉落物已生成於 {transform.position}");
        }
        else
        {
            Debug.LogWarning("[enemy_cow] 沒找到 LootBag 元件，無法掉落物品。");
        }

        Destroy(gameObject);
    }

    public void DestroySelf()
    {
        var loot = GetComponent<LootBag>();
        if (loot != null) loot.InstantiateLoot(transform.position);
        Destroy(gameObject);
    }

    public void ResetToIdle()
    {
        if (state == State.hurt) state = State.idle;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopChaseRange);
    }

    /* IDataPersistence */
    public void LoadData(GameData data)
    {
        if (healthBar == null) return;
        float hp = data.GetHP(enemyID, maxHealth);
        healthBar.SetHealth(hp);
        currentHealth = hp;

        if (currentHealth <= 0)
            Destroy(gameObject); 
    }

    public void SaveData(ref GameData data)
    {
        if (healthBar == null) return;
        data.SetHP(enemyID, currentHealth > 0 ? currentHealth : 0);
    }
    public void EnableHitbox()
    {
        hasDealtDamage = false; // ✅ 每次出手前重置
        if (hitbox != null)
            hitbox.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.SetActive(false);
    }
}
