using UnityEngine;
using System;

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

    [Tooltip("廣播給S2C進對話模式用的")]
    public event Action<LivingEntity> OnDeathEvent;

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
        Debug.Log($"[LE] {name} 收到傷害 {damage}，上次受傷到現在：{Time.time - lastDamageTime:0.000}s");
        // ✅ 若在短時間內剛受傷過 → 略過（防同幀重複）
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        lastDamageTime = Time.time;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"[LE] {name} 扣血後剩 {currentHealth}");
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

        // ✅ 廣播死亡事件給訂閱者
        OnDeathEvent?.Invoke(this);

        OnDeath(); // 給子類覆寫
    }

    // ✅ 改成 protected internal，保留原有功能但允許同 Assembly 類別存取（不報CS0122）
    protected virtual void OnDeath()
    {
        Destroy(gameObject);
    }
    
    // LivingEntity.cs 內新增這段
    protected void InvokeDeathEvent()
    {
        OnDeathEvent?.Invoke(this);
    }

}
