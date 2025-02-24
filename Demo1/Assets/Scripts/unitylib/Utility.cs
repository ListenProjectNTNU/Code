using UnityEngine;

public static class PlayerUtils
{
    // 確保數值在範圍內（例如血量不低於 0）
    public static float ClampValue(float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }

    // 處理擊退（Knockback）效果
    public static void ApplyKnockback(Rigidbody2D rb, float force, Transform attacker, Transform player)
    {
        if (attacker.position.x > player.position.x)
        {
            rb.velocity = new Vector2(-force, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(force, rb.velocity.y);
        }
    }

    // 銷毀物件，例如玩家吃掉櫻桃
    public static void DestroyObject(GameObject obj)
    {
        GameObject.Destroy(obj);
    }

    // 讓玩家受傷
    public static void TakeDamage(healthbar healthBar, float damage)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(ClampValue(healthBar.currenthp - damage, 0, healthBar.maxHP));
        }
        else
        {
            Debug.LogError("HealthBar reference is missing.");
        }
    }

    // 檢查是否死亡
    public static bool CheckDeath(healthbar healthBar)
    {
        return healthBar != null && healthBar.currenthp <= 0;
    }
}
