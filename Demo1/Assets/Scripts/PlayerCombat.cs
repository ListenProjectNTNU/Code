using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator animator;

    public GameObject attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 30;
    public float knockbackForce = 5f;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Attack();
            
        }
    }

    void Attack()
    {
        // 播放攻擊動畫
        animator.SetTrigger("Attack");
        // 搜尋攻擊範圍內的敵人
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.transform.position,
            attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            // 嘗試呼叫 TakeDamage
            enemy.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);

            // 加入擊退效果
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (enemy.transform.position - attackPoint.transform.position).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        attackPoint.SetActive(false);
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.transform.position, attackRange);
    }
}
