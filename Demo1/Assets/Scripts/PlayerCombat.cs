using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator animator;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 30;
    public float knockbackForce = 5f;

    // 防重複命中
    private HashSet<GameObject> hitEnemiesThisAttack = new HashSet<GameObject>();
    private bool isAttacking = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            StartCoroutine(PerformAttack("kick"));
        else if (Input.GetKeyDown(KeyCode.C))
            StartCoroutine(PerformAttack("kick2"));
        else if (Input.GetKeyDown(KeyCode.R))
            StartCoroutine(PerformAttack("punch"));
    }

    private IEnumerator PerformAttack(string trigger)
    {
        if (isAttacking) yield break; // 攻擊中不接受新指令

        isAttacking = true;
        hitEnemiesThisAttack.Clear(); // 每次攻擊開始前清空
        animator.SetTrigger(trigger);

        // 攻擊執行期間 (防止多次輸入)
        yield return new WaitForSeconds(0.4f); // 根據攻擊動畫長度微調
        isAttacking = false;
    }

    // 在攻擊動畫的關鍵幀（Animation Event）呼叫這個
    public void Attack()
    {
        if (!isAttacking) return;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            if (hitEnemiesThisAttack.Contains(enemy.gameObject))
                continue;

            hitEnemiesThisAttack.Add(enemy.gameObject);

            // ✅ 用 GetComponent<LivingEntity>() 直接呼叫，不用 SendMessage
            LivingEntity target = enemy.GetComponent<LivingEntity>();
            if (target != null)
            {
                target.TakeDamage(attackDamage);
            }

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
