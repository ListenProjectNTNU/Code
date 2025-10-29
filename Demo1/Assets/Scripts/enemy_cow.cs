using System.Collections;
using UnityEngine;

public class enemy_cow : LivingEntity, IDataPersistence
{
    [Header("Patrol")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private float fallbackPatrolHalfWidth = 2f; // 若沒指定巡邏點，給預設寬度
    private float leftCap;
    private float rightCap;

    [Header("IDs & Layers")]
    [SerializeField] private string enemyID = "cow1";
    public LayerMask ground;

    [Header("Combat")]
    public Transform player;                 // ← 可能未指派，已做自動抓取與防護
    public Vector3 attackOffset;
    public LayerMask attackMask;
    public float attackRange = 3f;
    public float attackCooldown = 10f;       // 建議 ≥ 攻擊動畫時長
    private float nextAttackTime = 0f;

    [Header("Chase")]
    public float chaseRange = 6f;
    public float stopChaseRange = 10f;

    [Header("SC Control")]
    public bool isActive = true;             // 非自由模式控制用
    public bool controlledBySC = false;      // 是否由 SceneController 控制
    private bool canMove = true;             // SC 控制的移動開關

    [Header("Misc")]
    public GameObject hitbox;

    private Quaternion fixedRotation;
    private bool facingLeft = true;
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;
    private Vector3 originalScale;

    private enum State { idle, attack, hurt, dying, run };
    private State state = State.idle;

    private bool isChasing = false;
    private bool isAttacking = false;
    private bool hasDealtDamage = false;

    [Header("Loot")]
    private bool lootDropped = false; // 防止重複掉落

    // ───────────── 新增：Awake 先嘗試抓 Player，避免一開始未指派 ─────────────
    private void Awake()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove)
        {
            if (rb) rb.velocity = Vector2.zero;
            if (anim) anim.SetInteger("state", (int)State.idle);
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

        // ───────────── 安全計算巡邏邊界 ─────────────
        if (leftPoint && rightPoint)
        {
            leftCap = leftPoint.position.x;
            rightCap = rightPoint.position.x;
        }
        else
        {
            // 若沒指定，給個以自身為中心的預設巡邏範圍
            leftCap = transform.position.x - Mathf.Abs(fallbackPatrolHalfWidth);
            rightCap = transform.position.x + Mathf.Abs(fallbackPatrolHalfWidth);
            Debug.LogWarning($"[enemy_cow] 未指定 leftPoint/rightPoint，使用預設巡邏範圍 [{leftCap:F2}, {rightCap:F2}]。");
        }

        if (hitbox != null) hitbox.SetActive(false);
    }

    private void Update()
    {
        // 固定轉向避免受物理/動畫影響
        transform.rotation = fixedRotation;
        if (isDead) return;

        anim.SetInteger("state", (int)state);
        AnimationState(); // 讓巡邏、移動判斷正常

        // SC 控制：若暫停移動，就不做追擊攻擊判斷
        if (controlledBySC && !canMove) return;

        if (!isActive) return;

        // ───────────── 重要：player 空值防護 ─────────────
        if (!player)
        {
            // 再嘗試一次以防場景剛生成
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;

            if (!player) return; // 找不到就先不要做後續邏輯
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        CheckAttack(distanceToPlayer);
    }

    private void CheckAttack(float distanceToPlayer)
    {
        if (!player) return; // 雙重保險

        if (state != State.attack)
        {
            if (distanceToPlayer <= chaseRange)
                isChasing = true;
            else if (distanceToPlayer >= stopChaseRange)
                isChasing = false;
        }
        else
        {
            // 正在攻擊但玩家離開範圍 → 中斷
            if (distanceToPlayer > chaseRange)
            {
                isAttacking = false;
                state = State.idle;
                anim.ResetTrigger("attack");
                // Debug.Log("[enemy_cow] 玩家離開範圍 → 中斷攻擊");
            }
        }

        // 攻擊啟動條件
        if (isChasing && !isAttacking && distanceToPlayer <= attackRange &&
            Time.time >= nextAttackTime && state != State.attack)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (isAttacking) return;

        isAttacking = true;
        hasDealtDamage = false;
        state = State.attack;
        anim.SetTrigger("attack");

        nextAttackTime = Time.time + attackCooldown;

        if (!player) return; // 安全：若此刻玩家被刪除/未找到就不做偵測

        Vector3 pos = transform.position;
        pos += transform.right * attackOffset.x;
        pos += transform.up * attackOffset.y;

        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
        if (colInfo != null && !hasDealtDamage)
        {
            var target = colInfo.GetComponent<LivingEntity>();
            if (target != null)
            {
                target.TakeDamage(20);
                hasDealtDamage = true;
            }
        }
    }

    // 在攻擊動畫最後一幀加 Animation Event 呼叫這個
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        state = isChasing ? State.run : State.idle;
    }

    private void Move()
    {
        const float moveSpeed = 2f;

        if (controlledBySC && !canMove) return;

        if (isChasing && player) // 追擊
        {
            state = State.run;
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

            if (direction.x < 0)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
            else
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y);
        }
        else // 巡邏
        {
            if (facingLeft)
            {
                if (transform.position.x > leftCap)
                {
                    rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
                    transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
                }
                else
                {
                    facingLeft = false;
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
                    facingLeft = true;
                }
            }
        }
    }

    private void AnimationState()
    {
        if (state == State.hurt || state == State.dying || state == State.attack)
            return;

        Move();

        state = isChasing ? State.run : State.idle;
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
        if (isDead) return;
        isDead = true;

        anim.ResetTrigger("attack");
        anim.SetTrigger("die");
        state = State.dying;

        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        if (coll) coll.enabled = false;
        if (hitbox) hitbox.SetActive(false);

        base.Die(); // 廣播事件
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 等待動畫（給個保險 timeout）
        float timeout = 5f;
        while (anim && !anim.GetCurrentAnimatorStateInfo(0).IsName("dying") && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        while (anim &&
               anim.GetCurrentAnimatorStateInfo(0).IsName("dying") &&
               anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.99f &&
               timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        OnDeathAnimationEnd();
    }

    public void OnDeathAnimationEnd()
    {
        DropLootAndDestroy();
    }

    private void DropLootAndDestroy()
    {
        if (lootDropped) return;
        lootDropped = true;

        var loot = GetComponent<LootBag>();
        if (loot != null)
            loot.InstantiateLoot(transform.position);

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

    // 給動畫事件呼叫
    public void EnableHitbox()
    {
        hasDealtDamage = false;
        if (hitbox != null)
            hitbox.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
            hitbox.SetActive(false);
    }
}
