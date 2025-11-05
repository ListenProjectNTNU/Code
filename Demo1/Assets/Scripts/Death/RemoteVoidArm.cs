using UnityEngine;

public class RemoteVoidArm : MonoBehaviour
{
    [Header("Hitbox")]
    public Collider2D hitbox;                 // 可不填，會在 Awake 自動抓
    public LayerMask playerMask;
    public int damage = 15;
    public float lifeTime = 1.2f;             // 手臂存活時間（等於動畫長度）
    public bool destroyOnAnimEvent = true;    // 若動畫尾會呼叫 Anim_HandEnd，就把它打勾

    private bool hasHit = false;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (!hitbox) hitbox = GetComponent<Collider2D>();
        if (hitbox) hitbox.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!destroyOnAnimEvent)
            Destroy(gameObject, lifeTime);
    }

    public void Init(LayerMask mask, int dmg)
    {
        playerMask = mask;
        damage = dmg;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 檢查 Layer 是否在 playerMask 裡
        if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;

        if (other.TryGetComponent<LivingEntity>(out var le))
        {
            le.TakeDamage(damage);
            hasHit = true;
        }
    }

    // 動畫事件：尾端呼叫
    public void Anim_HandEnd()
    {
        Destroy(gameObject);
    }
}
