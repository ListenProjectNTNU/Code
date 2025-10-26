using UnityEngine;

public class BossAttackHitbox : MonoBehaviour
{
    [Header("Refs")]
    public BossController owner;
    public int damage = 20;
    public LayerMask playerMask;

    // 一招只打一次
    bool hasHitThisSwing;

    void OnEnable()
    {
        hasHitThisSwing = false;

        // ✅ 啟用當下就檢查是否已重疊（避免沒有 OnTriggerEnter 的情況）
        TryInstantHitIfOverlapping();
    }
    public void Arm()
    {
        hasHitThisSwing = false;
    }

    void OnDisable()
    {
        hasHitThisSwing = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // ✅ 若一啟用就重疊，這裡也能補到
        TryHit(other);
    }

    void TryInstantHitIfOverlapping()
    {
        // 用 OverlapCollider 檢查當前已重疊的對象
        var selfCol = GetComponent<Collider2D>();
        if (!selfCol) return;

        var filter = new ContactFilter2D();
        filter.layerMask = playerMask;
        filter.useLayerMask = true;
        filter.useTriggers = true;

        Collider2D[] results = new Collider2D[4];
        int count = selfCol.OverlapCollider(filter, results);
        for (int i = 0; i < count; i++)
        {
            if (TryHit(results[i])) break; // 只需要命中一次
        }
    }

    bool TryHit(Collider2D other)
    {
        if (hasHitThisSwing) return false;
        if (((1 << other.gameObject.layer) & playerMask.value) == 0) return false;

        var target = other.GetComponent<LivingEntity>();
        if (target == null || target.isDead) return false;

        target.TakeDamage(damage);
        hasHitThisSwing = true;
        Debug.Log($"[BossHitbox] 命中 {other.name} 扣 {damage}");
        return true;
    }
}
