using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    public int health = 500;
    public healthbar healthBar;
    public bool isInvulnerable = false;

    private Animator animator;
    private int maxHP = 500;

    void Start()
    {
        // 確保 Animator 正確獲取
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("BossHealth: Animator component is missing!");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("playerhitbox"))
        {
            // 減少血量
            health = Mathf.Max(health - 200, 0);
            PlayerUtils.TakeDamage(healthBar, 200f);
            healthBar.SetHealth(health);

            Debug.Log($"{gameObject.name} is hurt!");

            // ✅ 進入「狂暴模式」
            if (health <= maxHP / 2)
            {
                animator.SetBool("IsEnraged", true); // 設置 Animator 變數
                Debug.Log($"{gameObject.name} is now enraged!");
            }

            // ✅ 血量歸零，執行死亡
            if (health <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died!");

        // 停止所有動作並播放死亡動畫
        animator.SetTrigger("Die");

        // 延遲銷毀物件（確保音效播放完）
        Destroy(gameObject, GetComponent<AudioSource>() != null ? GetComponent<AudioSource>().clip.length : 0f);
    }
}
