using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    //Start() var
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;

    public int cherries = 0;
    //public Text cherryText;

    //FSM
    private enum State{idle,running,jumping,falling,hurt,dead};
    private State state = State.idle;
    //Inspector variable
    public LayerMask ground;
    public float speed = 5f;
    public float jumpForce = 15f;
    public float hurtForce = 10f;
    public healthbar healthBar;
    private float trapDamageCooldown = 1f; // 地刺傷害間隔 1 秒
    private float lastTrapDamageTime = 0f; // 上次受到地刺傷害的時間

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
    }
  
    // Update is called once per frame
    void Update()
    {
        if(state != State.hurt)
        {
            Movement();
        }
        AnimationState();
        anim.SetInteger("state", (int)state);//sets animation based on Enumerator state
        //Debug.Log((int)state);
    }

    //Cherries
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectable")) 
        {
            cherries += 1;
            PlayerUtils.DestroyObject(collision.gameObject);
            //cherryText.text = cherries.ToString();
        }
        else if (collision.tag == "enemyhitbox") // 檢測是否碰到敵人的 Hitbox
        {
            EnemyBehavior enemy = collision.GetComponentInParent<EnemyBehavior>(); // 獲取敵人的主要行為腳本

            if (state == State.falling)
            {
                // 玩家從上方踩到敵人
                jump();
            }
            else  // 敵人處於攻擊狀態
            {
                // 玩家被敵人攻擊
                state = State.hurt;
                PlayerUtils.ApplyKnockback(rb, hurtForce, collision.transform, transform);
            }             
        }
    }

    //Enemy
    private void OnCollisionEnter2D(Collision2D other) 
    {
        if(other.gameObject.tag == "Enemy")  
        {
            if(state ==State.falling)
            {
                // Destroy(other.gameObject);
                jump();
            }
            else
            {
                state = State.hurt;
                Debug.Log(state);
                PlayerUtils.ApplyKnockback(rb, hurtForce, other.transform, transform);
                
                PlayerUtils.TakeDamage(healthBar, 20f);

                // 確保血量不小於 0
                if (PlayerUtils.CheckDeath(healthBar))
                {
                    state = State.dead;
                    anim.SetInteger("state", (int)State.dead);
                    Debug.Log("Player is dead!");
                    rb.velocity = Vector2.zero;
                    this.enabled = false;
                }
            }
            
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
{
    if (collision.tag == "trap" && Time.time > lastTrapDamageTime + trapDamageCooldown)
    {
        lastTrapDamageTime = Time.time; // 更新上次受傷時間
        TakeTrapDamage();
    }
}

// 受地刺傷害的方法
    private void TakeTrapDamage()
    {
        state = State.hurt;

        // 讓玩家受到擊退
        if (transform.position.x > 0)
        {
            rb.velocity = new Vector2(-hurtForce, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(hurtForce, rb.velocity.y);
        }

        // 扣血
        if (healthBar != null)
        {
            healthBar.SetHealth(healthBar.currenthp - 2f);
        }
        else
        {
            Debug.LogError("HealthBar reference is missing in PlayerController.");
        }

        // 檢查血量是否歸零
        if (healthBar != null && healthBar.currenthp <= 0)
        {
            healthBar.SetHealth(0f);
            state = State.dead;
            anim.SetInteger("state", (int)State.dead);
            Debug.Log("Player is dead!");
            rb.velocity = Vector2.zero;
            this.enabled = false;
        }
    }

    private void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal");

        if (hDirection < 0)
        {
            //moving left
            rb.velocity = new Vector2(-speed, rb.velocity.y);
            transform.localScale = new Vector2(-1, 1);
        }
        else if (hDirection > 0)
        {
            //moveing right
            rb.velocity = new Vector2(speed, rb.velocity.y);
            transform.localScale = new Vector2(1, 1);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        //jump
        if (Input.GetButtonDown("Jump") && coll.IsTouchingLayers(ground))
        {
            jump();
        }
    }
    
    private void jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        state = State.jumping;
    }

    private void AnimationState()
    {
        if(state == State.jumping)
        {
           //state = State.idle;
            if(rb.velocity.y < .1f)
            {
                state = State.falling;
            }
        }
        else if(state == State.falling)
        {
            if(coll.IsTouchingLayers(ground))
            {
                state = State.idle;
            }
        }
        //Math.Abs means absloute value
        else if(state == State.hurt)
        {
            if(Math.Abs(rb.velocity.x) < .1f)
            {
                state = State.idle;
            }
        }
        else if(Math.Abs(rb.velocity.x) > 4.5f)
        {
            //is running
            state = State.running;
        }
        else
        {
            state = State.idle;
        }
        //Debug.Log(state);
    }
}
