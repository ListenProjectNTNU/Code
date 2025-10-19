using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    public float damage = 10f;
    public LayerMask hittableLayers; // 設成可打到 Enemy/Boss 的 Layer

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Hitbox] Enter -> {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)})");

        // 檢查 LayerMask
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0) return;

        // 找對方的 LivingEntity（BossController 有繼承）
        var target = other.GetComponentInParent<LivingEntity>() ?? other.GetComponent<LivingEntity>();
        if (target != null)
        {
            Debug.Log($"[Hitbox] Deal {damage} to {target.name}");
            target.TakeDamage(damage); // float 版本
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 假如沒看到 Enter，就看有沒有 Stay 在刷
        // Debug.Log($"[Hitbox] Stay -> {other.name}");
    }
}
