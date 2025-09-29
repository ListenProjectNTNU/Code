using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Optional UI")]
    public HealthBar healthBar; // 可以綁 UI 血條，也可以留空

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        Debug.Log($"{gameObject.name} 受傷 -{damage}，剩餘血量 {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 死亡");
        // 預設處理：直接刪掉
        Destroy(gameObject);
    }
}
