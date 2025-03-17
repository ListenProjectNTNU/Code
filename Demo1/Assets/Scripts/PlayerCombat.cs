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
    public int attackDamage =30;
    public float attackDuration = 0.2f;

    private void Start()
    {
        attackPoint.SetActive(false); // 遊戲開始時先關閉
    }
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
        // 開啟攻擊範圍
        attackPoint.SetActive(true);
        Invoke("DisableAttack", attackDuration);
    }

    void DisableAttack()
    {
        attackPoint.SetActive(false);
    }
}
