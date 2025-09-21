using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator animator;

    [Header("Attack Settings")]
    public Transform attackPoint;        // 建議用 Transform 就好，不需要 GameObject
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 30;
    public float knockbackForce = 5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) // 踢
        {
            animator.SetTrigger("kick");
        }
        else if (Input.GetKeyDown(KeyCode.C)) // 第二踢
        {
            animator.SetTrigger("kick2");
        }
        else if (Input.GetKeyDown(KeyCode.R)) // 拳擊
        {
            animator.SetTrigger("punch");
        }
    }

    // 在攻擊動畫的關鍵幀(Animation Event) 呼叫這個
    public void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            // 呼叫敵人身上的 TakeDamage(int damage) 方法
            enemy.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);

            // 加入擊退效果
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (enemy.transform.position - transform.position).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
