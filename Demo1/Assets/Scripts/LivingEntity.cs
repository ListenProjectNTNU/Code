using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;
    private float lastDamageTime = 0f;
    private float damageCooldown = 0.5f;

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
        // ✅ 若已死亡，直接忽略（多餘命中不影響）
        if (isDead) return;

        // ✅ 若在短時間內剛受傷過 → 略過（防同幀重複）
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        lastDamageTime = Time.time;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        Debug.Log($"{gameObject.name} 受傷 -{damage}，剩餘血量 {currentHealth}");

        // ✅ 一旦血量歸零，就立刻鎖死，不再進入第二次 Die()
        if (currentHealth <= 0 && !isDead)
        {
            //isDead = true;
            Die();
        }
    }


    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 死亡");
        OnDeath(); // 給子類覆寫
    }
    protected virtual void OnDeath()
    {
        Destroy(gameObject);
    }
}
