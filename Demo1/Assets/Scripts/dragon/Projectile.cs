using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 15;
    public float speed = 8f;
    public float lifeTime = 5f;
    public string targetTag = "Player";
    public LayerMask groundMask; // 若撞牆要消失

    private Rigidbody2D rb;
    private float dieAt;
    private bool used = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 from, Vector2 dir)
    {
        transform.position = from;
        rb.velocity = dir.normalized * speed;
        dieAt = Time.time + lifeTime;
        used = false;
    }

    void Update()
    {
        if (Time.time >= dieAt) Destroy(gameObject); // 時間到消失
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        // 撞牆
        if (groundMask != 0 && ((1 << other.gameObject.layer) & groundMask) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // 撞到目標
        if (other.CompareTag(targetTag))
        {
            used = true;
            var le = other.GetComponent<LivingEntity>();
            if (le != null)
                le.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}