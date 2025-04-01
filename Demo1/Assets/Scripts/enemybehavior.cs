using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    private Animator animator;
    private float attackTimer;
    private bool isAttacking = false;
    public float attackInterval = 2f;
    
    public float chaseRange = 10f;
    public float moveSpeed = 2f;
    private float originalSpeed;
    public float patrolSpeed = 2f;
    
    [SerializeField] private float leftCap;
    [SerializeField] private float rightCap;
    private Vector3 patrolTarget;
    public float detectRange = 3f;
    
    private Transform player;
    public GameObject hitbox;
    
    public float health = 100f;
    public bool isDead = false;
    
    public healthbar healthBar;
    private bool facingRight = true;
    private bool isChasing = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        attackTimer = attackInterval + Random.Range(0f, 1f);
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (hitbox != null) hitbox.SetActive(false);
        patrolTarget = new Vector3(rightCap, transform.position.y, transform.position.z);
        originalSpeed = moveSpeed;
    }

    void Update()
    {
        if (isDead) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            Attack();
            attackTimer = attackInterval;
        }

        float playerDistance = Vector2.Distance(transform.position, player.position);
        bool isPlayerInBounds = (player.position.x >= leftCap && player.position.x <= rightCap);

        if (isPlayerInBounds && playerDistance <= detectRange)
            ChasePlayer();
        else
        {
            StopChasing();
            Patrol();
        }
    }

    private void ChasePlayer()
    {
        if (player == null) return;
        isChasing = true;
        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        SetState(4);
        LookAtPlayer();
    }

    private void Patrol()
    {
        if (isChasing) return;
        transform.position = Vector2.MoveTowards(transform.position, patrolTarget, patrolSpeed * Time.deltaTime);
        SetState(4);
        if (Vector2.Distance(transform.position, patrolTarget) < 0.1f)
        {
            patrolTarget = (patrolTarget.x == leftCap) ? new Vector3(rightCap, transform.position.y, transform.position.z) : new Vector3(leftCap, transform.position.y, transform.position.z);
            Flip();
        }
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        if ((player.position.x > transform.position.x && !facingRight) || (player.position.x < transform.position.x && facingRight))
        {
            Flip();
        }
    }

    public void StopChasing()
    {
        isChasing = false;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void Attack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;
        SetState(1);

        if (hitbox != null) hitbox.SetActive(true);
        Invoke("ResetAttack", 0.1f);
    }

    void ResetAttack()
    {
        isAttacking = false;
        SetState(0);
        if (hitbox != null) hitbox.SetActive(false);
    }

    // ✅ 敵人受傷處理
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("playerhitbox"))
        {
            // 減少血量
            health = Mathf.Max(health - 30, 0);
            PlayerUtils.TakeDamage(healthBar, 30f);

            animator.SetInteger("state", 2);
            Debug.Log($"{gameObject.name} is hurt!");
            Invoke("ResetToIdle", 0.5f);

            // ✅ 如果血量歸零，執行死亡
            if (health <= 0)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        isDead = true;
        SetState(3);
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
        GetComponent<LootBag>().InstantiateLoot(transform.position);
        Destroy(gameObject, 1f);
    }

    private void ResetToIdle()
    {
        if (!isDead) SetState(0);
    }

    public void SetState(int state)
    {
        if (animator != null) animator.SetInteger("state", state);
    }
}