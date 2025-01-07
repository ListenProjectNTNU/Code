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
    public int attackDamage =50;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Attack();
        }
    }

    void Attack()
    {
        //play an attack animation
        animator.SetTrigger("Attack");

        //detect enemies in range of attack
        Collider2D [] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position,attackRange,enemyLayers);
        //Damage them
        foreach(Collider2D enemy in hitEnemies)
        {
            Debug.Log("we hit"+ enemy.name);
            enemy.GetComponent<EnemyFrog>().TakeDamage(attackDamage);
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
