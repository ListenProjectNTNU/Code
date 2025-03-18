using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Animator animator;

    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage =30;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Attack();
        }
    }

    void Attack()
{
    // 播放攻击动画
    animator.SetTrigger("Attack");

    // 检测攻击范围内的所有敌人
    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

    // 对每个敌人造成伤害
    foreach (Collider2D enemy in hitEnemies)
    {
        Debug.Log("We hit " + enemy.name);

        // 获取敌人的 EnemyBehavior 脚本并调用 TakeDamage
        EnemyBehavior enemyBehavior = enemy.GetComponent<EnemyBehavior>();
        if (enemyBehavior != null)
        {
            enemyBehavior.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogWarning("EnemyBehavior is not found on " + enemy.name);
        }

        // 如果你有 EnemyFrog 脚本，也可以继续做类似的检查
        EnemyFrog enemyFrog = enemy.GetComponent<EnemyFrog>();
        if (enemyFrog != null)
        {
            enemyFrog.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogWarning("EnemyFrog is not found on " + enemy.name);
        }
    }
}

    void OnDrawGizmosSelected() 
    {
        if (attackPoint == null){
            return;
        }
        Gizmos.DrawWireSphere(attackPoint.position,attackRange);
    }
}
