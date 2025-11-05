using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Collider2D))]
public class RemoteVoidArm : MonoBehaviour
{
    private Animator anim;
    private Collider2D hitbox;

    private LayerMask playerMask;
    private int damage;
    private GameObject target;
    private bool hasHit = false;

    public void Init(LayerMask playerMask, int damage, GameObject target)
    {
        this.playerMask = playerMask;
        this.damage = damage;
        this.target = target;
    }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true; // 很重要！
    }

    // 動畫事件：命中幀（只打一次）
    public void Hand_Hit()
    {
        if (hasHit) return;
        hasHit = true;

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = playerMask,
            useTriggers = true
        };

        var results = new Collider2D[8];
        int count = hitbox.OverlapCollider(filter, results);
        for (int i = 0; i < count; i++)
        {
            var c = results[i];
            if (!c) continue;
            if (c.TryGetComponent<LivingEntity>(out var le))
                le.TakeDamage(damage);
        }

        // TODO: 在這裡可加相機震動/音效等
    }

    // 動畫事件：最後一幀
    public void Hand_End()
    {
        Destroy(gameObject);
    }
}
