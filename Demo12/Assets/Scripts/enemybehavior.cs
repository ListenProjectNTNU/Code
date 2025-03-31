using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    public Animator animator;
    private float attackTimer;
    private bool isAttacking = false;
    public float attackInterval = 2f; // 每 2 秒攻擊一次
    public GameObject hitbox; // 攻擊區域
    public float health = 100f; // 敵人血量
    public bool isDead = false; // 是否已死亡
    public healthbar healthBar;
    public int deathState = 3;
    public float chaseRange = 10f;
    public float moveSpeed = 2f;
    private float originalSpeed;
    public Transform player; // 追蹤玩家
    public float leftCap, rightCap;
    private bool isFlipped = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        // ✅ 讓敵人的攻擊計時器有點隨機性，避免同步攻擊
        attackTimer = attackInterval + Random.Range(0f, 1f);

        if (hitbox != null)
        {
            hitbox.SetActive(false); // 開始時隱藏 hitbox
        }

        originalSpeed = moveSpeed;
    }

    void Update()
    {
        if (isDead) return; // 死亡後停止所有行為

        attackTimer -= Time.deltaTime; // 減少計時器
        if (attackTimer <= 0)
        {
            Attack(); // 執行攻擊
            attackTimer = attackInterval; // 重置計時器
        }

        LookAtPlayer();
        ChasePlayer();
    }

    // ✅ 攻擊邏輯
    void Attack()
    {
        if (isDead) return; // 如果已死亡，不執行攻擊
        isAttacking = true;
        SetState(1); // 設置為攻擊狀態

        // 啟用 hitbox
        if (hitbox != null)
        {
            hitbox.SetActive(true);
        }

        // 延遲後重置攻擊狀態並禁用 hitbox
        Invoke("ResetAttack", 0.1f);
    }

    // ✅ 重置攻擊狀態
    void ResetAttack()
    {
        isAttacking = false;
        SetState(0); // 設置為待機狀態

        // 禁用 hitbox
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }
    }

    public void DestroySelf()
    {
        // ✅ 確保死亡時產生掉落物品
        LootBag lootBag = GetComponent<LootBag>();
        if (lootBag != null)
        {
            lootBag.InstantiateLoot(transform.position);
        }

        // ✅ 刪除敵人
        Destroy(gameObject);
    }

    // ✅ 敵人受傷處理
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("playerhitbox"))
        {
            // 減少血量
            health = Mathf.Max(health - 30, 0);
            PlayerUtils.TakeDamage(healthBar, 30f);

            animator.SetInteger("state", 2);
            Debug.Log($"{gameObject.name} is hurt!");
            Invoke("ResetToIdle", 0.5f);

            // ✅ 如果血量歸零，執行死亡
            if (health <= 0)
            {
                Die();
            }
        }
    }

    // ✅ 讓 `Die()` 負責處理死亡
    private void Die()
    {
        PlayerUtils.Die(this, deathState); // 呼叫共用的死亡函數
    }

    // ✅ 重置為待機狀態，但死亡時不應該重置
    private void ResetToIdle()
    {
        if (!isDead)
        {
            SetState(0); // 設置為待機狀態
        }
    }

    // ✅ 設置動畫狀態
    public void SetState(int state)
    {
        if (animator != null)
        {
            animator.SetInteger("state", state);
        }
        else
        {
            Debug.LogWarning("Animator is not assigned on the enemy!");
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public int GetState()
    {
        return animator != null ? animator.GetInteger("state") : -1;
    }

    public void EnableHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true); // 啟用 hitbox
        }
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false); // 禁用 hitbox
        }
    }

    // ✅ 讓敵人面對玩家
    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 flipped = transform.localScale;

        if (transform.position.x > player.position.x && !isFlipped)
        {
            flipped.x *= -1f;
            transform.localScale = flipped;
            isFlipped = true;
            if (animator != null)
            {
                animator.SetBool("IsFlipped", true);
            }
        }
        else if (transform.position.x < player.position.x && isFlipped)
        {
            flipped.x *= -1f;
            transform.localScale = flipped;
            isFlipped = false;
            if (animator != null)
            {
                animator.SetBool("IsFlipped", false);
            }
        }
    }

    // ✅ 追蹤玩家
    private void ChasePlayer()
    {
        if (player == null) return;

        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);

        if (transform.position.x <= leftCap || transform.position.x >= rightCap)
        {
            moveSpeed = 0f;
            SetState(0);
        }
        else
        {
            moveSpeed = originalSpeed;
            SetState(4); // 設定為移動狀態
        }

        if (moveSpeed > 0)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            SetState(4); // 設定為移動狀態
        }
    }
}
