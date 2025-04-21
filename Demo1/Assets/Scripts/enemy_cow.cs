using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemy_cow : MonoBehaviour
{

    public int maxHealth = 100;
    public int health = 100;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;  
    private Quaternion fixedRotation;
    public LayerMask ground;
    
    private bool facingLeft = true;
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;
    public healthbar healthBar;
    public GameObject hitbox;

    private enum State {idle, attack,hurt, dying};
    private State state = State.idle;
    private Vector3 originalScale;
    private float leftCap;
    private float rightCap;

    public Transform player;  // 玩家物件
    public Vector3 attackOffset;
    public LayerMask attackMask;
    public float attackRange = 3f;  // 攻擊範圍
    public float attackCooldown = 0.2f;  // 攻擊冷卻時間
    private float nextAttackTime = 0f;  // 下一次攻擊時間
    //bool IsDead = false;
    private void Start()
    {
        fixedRotation = transform.rotation;
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        leftCap = leftPoint.position.x;
        rightCap = rightPoint.position.x;
        hitbox.SetActive(false);
    }

    private void Update()
    {
        transform.rotation = fixedRotation;
        //Move();
        AnimationState();
        if (Vector2.Distance(transform.position, player.position) <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
        }
        anim.SetInteger("state", (int)state);
    }
    private void Attack()
    {
        
        Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;
        // 觸發攻擊動畫
        anim.SetTrigger("attack");
        nextAttackTime = Time.time + attackCooldown;
        Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
		if (colInfo != null)
		{
			// 嘗試從 Player 取得 PlayerController
			PlayerController player = colInfo.GetComponent<PlayerController>();
			if (player != null)
			{
				// 使用 PlayerController 中的 Health Bar
				if (player.healthBar != null)
				{
					//player.healthBar.SetHealth(player.healthBar.currenthp - 100);
					PlayerUtils.TakeDamage(player.healthBar, 20 - player.curdefence );
                    player.anim.SetTrigger("hurt");
					Debug.Log("成功對 Player 造成傷害！");
				}
				else
				{
					Debug.LogWarning("Player 的 Health Bar 為空！");
				}
			}
			else
			{
				Debug.LogWarning("碰到的物体没有 PlayerController 组件：" + colInfo.name);
			}
		}
    }
    
    public void Die()
    {
        Debug.Log("Enemy Died");
        state = State.dying;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        coll.enabled = false;

        anim.SetInteger("state", 3);  // 設定動畫狀態
        GetComponent<LootBag>().InstantiateLoot(transform.position);
        Destroy(gameObject, 1f);
        //StartCoroutine(WaitAndDisappear(1.0f)); // 等待死亡動畫完成
    }

    private void Move()
    {
        float moveSpeed = 2f; // 你可以調整這個速度

        if (facingLeft)
        {
            if (transform.position.x > leftCap)
            {
                rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y); // 面向左
            }
            else
            {
                facingLeft = false;
            }
        }
        else
        {
            if (transform.position.x < rightCap)
            {
                rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y); // 面向右
            }
            else
            {
                facingLeft = true;
            }
        }
    }


    private void AnimationState()
    {
        Move(); // 自動移動

        // 如果敵人正在受傷、死亡或攻擊中，則不改變狀態
        if (state == State.hurt || state == State.dying || state == State.attack)
        {
            return;
        }

        // 如果處於攻擊狀態
        if (state == State.attack)
        {
            // 等待攻擊動畫持續時間結束後，切換回 idle 狀態
            if (Time.time >= nextAttackTime)  // 攻擊結束後
            {
                state = State.idle;  // 攻擊結束後切換回 idle
            }
        }
        else
        {
            // 如果不是在攻擊狀態，則強制設置為 idle
            state = State.idle;
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.CompareTag("playerhitbox"))
        {
            state = State.hurt;
            PlayerController playerController = collision.GetComponentInParent<PlayerController>();
            // 減少血量
            health = Mathf.Max(health - playerController.curattack, 0);
            PlayerUtils.TakeDamage(healthBar, playerController.curattack);
            //healthBar.SetHealth(health);
            
            Debug.Log($"{gameObject.name} is hurt!");
            Invoke("ResetToIdle", 0.5f);
            // ✅ 如果血量歸零，執行死亡
            if (health <= 0)
            {
                Die();
            }
        }
    }

    public void EnableHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true); // 啟用 hitbox
        }
    }

    public void DisableHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false); // 禁用 hitbox
        }
    }
    public void ResetToIdle()
    {
        if (state == State.hurt)
        {
            state = State.idle;
        }
    }
    void OnDrawGizmosSelected()
	{
		Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;

		Gizmos.DrawWireSphere(pos, attackRange);
	}
}


