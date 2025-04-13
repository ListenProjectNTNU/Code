using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    //Start() var
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;
    public LayerMask wallLayer;
    public int attackseg = 0;
    public int defenceseg = 0;
    public int speedseg = 0;
    //public Text cherryText;

    //FSM
    private enum State{idle,running,jumping,falling,hurt,dead};
    private State state = State.idle;
    //Inspector variable
    public LayerMask ground;
    public float jumpForce = 15f;
    public float hurtForce = 10f;
    
    public int speed = 5 ;
    public int attackDamage = 20;
    public int defence = 15;

    public int curdefence => defence + defenceseg * 10;
    public int curattack => attackDamage + attackseg * 10;
    public int curspeed => speed + speedseg * 20;

    public healthbar healthBar;
    public GameObject deathMenu;

    public int attack_damage = 20;

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

        if (transform.position.y < -10) // 設定掉落的臨界點
        {
            ResetPlayerPosition(); // 重置玩家位置並扣血
        }

        if (PlayerUtils.CheckDeath(healthBar))
        {
            state = State.dead;
            anim.SetInteger("state", (int)State.dead);
            Debug.Log("Player is dead!");
            rb.velocity = Vector2.zero;
            this.enabled = false;
            deathMenu.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectable"))
        {
            LootItem lootItem = collision.GetComponent<LootItem>();
            if (lootItem != null)
            {
                PlayerInventory.Instance.AddItem(lootItem.lootData);
                Destroy(collision.gameObject);
            }
        }
        else if (collision.tag == "enemyhitbox" || collision.tag == "trap")  // 檢測是否碰到敵人的 Hitbox
        {
            state = State.hurt;
            PlayerUtils.ApplyKnockback(rb, hurtForce, collision.transform, transform);  
        }
    }

    //重生
    public void RevivePlayer()
    {
        Debug.Log("RevivePlayer() 被執行！");
        Debug.Log("RevivePlayer() 被執行！重新載入場景！");
        healthBar.SetHealth(healthBar.maxHP);
        transform.position = new Vector3(0, 0, 0);
        state = State.idle;
        anim.SetInteger("state", (int)state);
        rb.velocity = Vector2.zero;

        // 重新啟動 PlayerController
        this.enabled = true;

        // 關閉死亡選單
        deathMenu.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetPlayerPosition()
    {
        Debug.Log("玩家掉落過低，重置位置並扣血！");
        
        // 設定玩家回到原點
        transform.position = new Vector3(0, 0, 0);

        // 扣血
        if (healthBar != null)
        {
            PlayerUtils.TakeDamage(healthBar, 9999); 
        }
    }



    public void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal");

        // 判斷是否碰牆
        bool touchingWallLeft = Physics2D.Raycast(transform.position, Vector2.left, 0.6f, wallLayer);
        bool touchingWallRight = Physics2D.Raycast(transform.position, Vector2.right, 0.6f, wallLayer);

        // 判斷是否在嘗試往牆上移動
        bool movingIntoLeftWall = hDirection < 0 && touchingWallLeft;
        bool movingIntoRightWall = hDirection > 0 && touchingWallRight;

        if (movingIntoLeftWall || movingIntoRightWall)
        {
            // 如果正在推牆，停止水平速度
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else if (hDirection < 0)
        {
            rb.velocity = new Vector2(-curspeed, rb.velocity.y);
            transform.localScale = new Vector2(-1, 1);
        }
        else if (hDirection > 0)
        {
            rb.velocity = new Vector2(curspeed, rb.velocity.y);
            transform.localScale = new Vector2(1, 1);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // jump
        if (Input.GetButtonDown("Jump") && coll.IsTouchingLayers(ground))
        {
            jump();
        }
    }
    
    public void jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        state = State.jumping;
    }

    public void AnimationState()
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
