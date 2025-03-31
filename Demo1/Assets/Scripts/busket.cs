using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class busket : MonoBehaviour
{
    public float health = 100f; // 敵人血量
    public bool isDead = false;
    public healthbar healthBar;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;
            if (collision.CompareTag("playerhitbox"))
            {
                PlayerController playerController = collision.GetComponentInParent<PlayerController>();
                // 減少血量
                health = Mathf.Max(health - playerController.curattack , 0);
                PlayerUtils.TakeDamage(healthBar, playerController.curattack);
                healthBar.SetHealth(health);

                Debug.Log($"{gameObject.name} is hurt!");
                // ✅ 血量歸零，執行死亡
                if (health <= 0)
                {
                    isDead = true;
                    Destroy(gameObject);
                }
            }
    }
}
