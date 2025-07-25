using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemy_cow : MonoBehaviour, IDataPersistence
{

    public int maxHealth = 100;
    public int health = 100;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;  
    [SerializeField] private string enemyID = "cow1";
    private Quaternion fixedRotation;
    public LayerMask ground;
    
    private bool facingLeft = true;
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;
    public healthbar healthBar;
    public GameObject hitbox;

    private enum State {idle, attack, hurt, dying, run};
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
    public float chaseRange = 6f;
    public float stopChaseRange = 10f;
    private bool isChasing = false;
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
        
        anim.SetInteger("state", (int)state);
        AnimationState();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= chaseRange)
        {
            isChasing = true;
        }
        else if (distanceToPlayer >= stopChaseRange)
        {
            isChasing = false;
        }

        // 只有在追擊狀態才能攻擊
        if (isChasing && distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
        }
    }
    private void Attack()
    {
        
        Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;
        // 觸發攻擊動畫
        state = State.attack;
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
        float moveSpeed = 2f;

        if (isChasing)
        {
            // 追玩家
            state = State.run;
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

            // 調整面向
            if (direction.x < 0)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
            else
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y);
        }
        else
        {
            // 原本的左右巡邏
            if (facingLeft)
            {
                if (transform.position.x > leftCap)
                {
                    rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
                    transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y);
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
                    transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y);
                }
                else
                {
                    facingLeft = true;
                }
            }
        }
    }



    private void AnimationState()
    {
        Move(); // 自動移動

        if (state == State.hurt || state == State.dying || state == State.attack)
        {
            return;
        }

        if (isChasing)
        {
            state = State.run;
        }
        else
        {
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
            healthBar.SetHealth(health);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 停止追擊範圍（藍色）
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopChaseRange);
    }
    public void LoadData(GameData data)
    {
        // 使用你 GameData 中的 GetHP 方法
        health = (int)data.GetHP(enemyID, maxHealth);
        healthBar.SetHealth(health);

        if (health <= 0)
        {
            Destroy(gameObject); // 不重生
        }
    }

    public void SaveData(ref GameData data)
    {
        if (health > 0)
            data.SetHP(enemyID, health);
        else
            data.SetHP(enemyID, 0);  // 儲存 0 也可以保留資料（讓你之後可以看哪隻死了）
    }
}


