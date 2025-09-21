using UnityEngine;

public class Busket : MonoBehaviour
{
    public float health = 100f; 
    public bool isDead = false;
    public HealthBar healthBar;

    void Start()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }
    }

    // 玩家攻擊命中時，必須有一個帶 "playerhitbox" Tag 的 Collider 碰到木樁
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("playerhitbox"))
        {
            PlayerController playerController = collision.collider.GetComponentInParent<PlayerController>();

            if (playerController != null)
            {
                // 扣血
                health = Mathf.Max(health - playerController.curattack, 0);
                if (healthBar != null)
                {
                    healthBar.SetHealth(health);
                }

                Debug.Log($"{gameObject.name} 被打，剩餘血量: {health}");

                // 判斷死亡
                if (health <= 0)
                {
                    isDead = true;
                    Die();
                }
            }
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 被打壞了！");
        Destroy(gameObject);
    }
}
