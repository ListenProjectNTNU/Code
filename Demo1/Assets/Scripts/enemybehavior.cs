using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    private Animator animator;
    private float attackTimer;
    private bool isAttacking = false;
    public float attackInterval = 2f; // 每 2 秒攻擊一次
    public GameObject hitbox; // 攻擊區域
    public GameObject dropItemPrefab;
    public Transform dropPosition;   // 掉落物生成位置，可選（默認為敵人位置）
    public int dropAmount = 1; // 掉落物的數量，可調整

    public float health = 100f; // 敵人血量
    private bool isDead = false; // 是否已死亡
    public healthbar healthBar;
    void Start()
    {
        animator = GetComponent<Animator>();
        attackTimer = attackInterval; // 初始化計時器
        if (hitbox != null)
        {
            hitbox.SetActive(false); // 開始時隱藏 hitbox
        }
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
    }

    // 攻擊邏輯
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

    // 重置攻擊狀態
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

    // 敵人受傷處理
    public void TakeDamage(int damage)
    {
        if (isDead) return; // 如果已死亡，不执行受伤处理

        if(healthBar != null)
        {
            healthBar.SetHealth(healthBar.currenthp - damage);
        }
        health -= damage; // 减少血量
        if (health <= 0)
        {
            Die(); // 如果血量小于等于 0，进入死亡状态
        }
        else
        {
            Hurt(); // 否则进入受伤状态
        }
    }

    // 受傷處理
    private void Hurt()
    {
        animator.SetInteger("state", 2);// 設置為受傷狀態
        Debug.Log($"{gameObject.name} is hurt!");

        // 在受傷後短暫恢復到待機狀態
        Invoke("ResetToIdle", 0.5f);
    }

    // 死亡處理
    private void Die()
    {
        isDead = true;
        SetState(3); // 設置為死亡狀態
        Debug.Log($"{gameObject.name} is dead!");

        // 禁用敵人碰撞和行為
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;

        //呼叫掉落物
        GetComponent<LootBag>().InstantiateLoot(transform.position);

        // 延遲後刪除敵人
        Destroy(gameObject, 1f);
        //SpawnDropItems();
    }

    // 重置為待機狀態
    private void ResetToIdle()
    {
        if (!isDead)
        {
            SetState(0); // 設置為待機狀態
        }
    }

    // 設置動畫狀態
    private void SetState(int state)
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

    /* private void SpawnDropItems()
    {
        for (int i = 0; i < dropAmount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                0
            );

            // 生成掉落物
            Instantiate(
                dropItemPrefab, 
                (dropPosition != null ? dropPosition.position : transform.position) + randomOffset, 
                Quaternion.identity
            );
        }
    } */
}
