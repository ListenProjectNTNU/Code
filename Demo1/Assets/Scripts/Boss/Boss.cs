using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossController : LivingEntity
{
    [Header("Refs")]
    public Transform player;
    public Collider2D attackHitbox;
    public LayerMask groundMask;

    [Header("Stats")]
    public int contactDamage = 25;
    public float moveSpeed = 2.5f;
    public float chaseRange = 15f;
    public float attackRangeX = 3.5f;
    public float attackCooldown = 3f;

    [Header("Drop Attack Tunings")]
    public float riseHeightAbovePlayer = 4.0f;
    public float riseSpeed = 10f;
    public float dropStartDelay = 0.1f;
    public float dropSpeed = 20f;
    public float gravityDuringDrop = 5f;
    public float postLandPause = 0.35f;

    [Header("Misc")]
    public float groundCheckRadius = 0.15f;
    public Transform groundCheck;

    [SerializeField] float hurtStun = 0.5f;
    [SerializeField] bool disableColliderOnDeath = true;

    // runtime
    Rigidbody2D rb;
    Animator anim;
    Collider2D col;
    float nextAttackTime;
    bool isHurting;
    bool isAttacking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (attackHitbox) attackHitbox.enabled = false;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (isDead) return;

        // 更新 Animator 參數
        anim.SetFloat("SpeedX", Mathf.Abs(rb.velocity.x));
        int moveDir = rb.velocity.x > 0.05f ? 1 : (rb.velocity.x < -0.05f ? -1 : (transform.localScale.x >= 0 ? 1 : -1));
        anim.SetInteger("MoveDir", moveDir);

        if (isAttacking || player == null) return;

        float dx = player.position.x - transform.position.x;
        float ax = Mathf.Abs(dx);

        // 追擊
        if (Vector2.Distance(player.position, transform.position) <= chaseRange)
        {
            int dir = dx > 0 ? 1 : -1;
            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // 攻擊判定
        if (ax <= attackRangeX && Time.time >= nextAttackTime && IsGrounded())
        {
            StartCoroutine(Co_DropAttack());
        }
    }

    IEnumerator Co_DropAttack()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;

        anim.SetTrigger("DoAttack");

        // 升到玩家頭頂上方
        Vector2 targetPos = new Vector2(player.position.x, player.position.y + riseHeightAbovePlayer);
        float t = 0f;
        while (Vector2.Distance(transform.position, targetPos) > 0.05f && t < 0.35f)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, riseSpeed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(dropStartDelay);

        // 俯衝
        float oldGravity = rb.gravityScale;
        rb.gravityScale = gravityDuringDrop;
        rb.velocity = Vector2.down * dropSpeed;

        if (attackHitbox) attackHitbox.enabled = true;

        // 等到落地（可改成 Raycast 更穩）
        while (!IsGrounded())
            yield return new WaitForFixedUpdate();

        if (attackHitbox) attackHitbox.enabled = false;
        rb.gravityScale = oldGravity;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(postLandPause);

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    // ==== 傷害 / 死亡：覆寫基底 ====
    public override void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // 這行會扣血，若 <=0 會自動呼叫 Die() → OnDeath()
        base.TakeDamage(damage);

        // 還活著才進入受傷硬直
        if (!isDead && !isHurting)
            StartCoroutine(Co_HurtStagger());
    }
    protected override void OnDeath()
    {
        // 這裡會在 base.TakeDamage() 觸發 Die() 後被呼叫
        CancelInvoke();
        StopAllCoroutines();

        if (attackHitbox) attackHitbox.enabled = false;

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        if (disableColliderOnDeath && col) col.enabled = false;

        anim.SetBool("IsDying", true);  // Any State → dying (IsDying==true)

        // 建議：在 dying clip 的最後放 Animation Event -> DestroySelf()
        // 若還沒有動畫事件，可用保底協程：
        // StartCoroutine(DestroyAfterAnim("dying", 0.1f));
    }

    IEnumerator Co_HurtStagger()
    {
        isHurting = true;
        // 若你有 hurt 動畫就觸發；沒有可以移除此行
        anim.SetTrigger("hurt");
        // 短暫停住水平速度
        rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(hurtStun);
        isHurting = false;
    }

    // 給動畫事件呼叫
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking) return;
        if (attackHitbox && other == attackHitbox) return;

        if (other.CompareTag("Player"))
        {
            // TODO: 呼叫玩家受傷（依你的玩家腳本）
            // var hp = other.GetComponent<PlayerHealth>();
            // hp?.Hurt(contactDamage, (other.transform.position - transform.position).normalized);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
