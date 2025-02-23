using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D coll;

    public int cherries = 0;
    public Text cherryText;

    private enum State { idle, running, jumping, falling, hurt, dead };
    private State state = State.idle;

    public LayerMask ground;
    public float speed = 5f;
    public float jumpForce = 15f;
    public float hurtForce = 10f;
    public healthbar healthBar;

    private float trapDamageCooldown = 1f;
    private float lastTrapDamageTime = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (state != State.hurt)
        {
            Movement();
        }
        AnimationState();
        anim.SetInteger("state", (int)state);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Collectable"))
        {
            LootItem lootItem = collision.GetComponent<LootItem>();
            if (lootItem != null)
            {
                PlayerInventory.Instance.AddItem(lootItem.lootData.lootName);
                Destroy(collision.gameObject);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "trap" && Time.time > lastTrapDamageTime + trapDamageCooldown)
        {
            lastTrapDamageTime = Time.time;
            TakeDamage(2);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            if (state == State.falling)
            {
                jump();
            }
            else
            {
                state = State.hurt;
                if (other.gameObject.transform.position.x > transform.position.x)
                {
                    rb.velocity = new Vector2(-hurtForce, rb.velocity.y);
                }
                else
                {
                    rb.velocity = new Vector2(hurtForce, rb.velocity.y);
                }
                TakeDamage(100);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        state = State.hurt;
        if (healthBar != null)
        {
            healthBar.SetHealth(healthBar.currenthp - damage);
        }
        else
        {
            Debug.LogError("HealthBar reference is missing in PlayerController.");
        }

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
            rb.velocity = new Vector2(-speed, rb.velocity.y);
            transform.localScale = new Vector2(-1, 1);
        }
        else if (hDirection > 0)
        {
            rb.velocity = new Vector2(speed, rb.velocity.y);
            transform.localScale = new Vector2(1, 1);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

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
        if (state == State.jumping)
        {
            if (rb.velocity.y < .1f)
            {
                state = State.falling;
            }
        }
        else if (state == State.falling)
        {
            if (coll.IsTouchingLayers(ground))
            {
                state = State.idle;
            }
        }
        else if (state == State.hurt)
        {
            if (Math.Abs(rb.velocity.x) < .1f)
            {
                state = State.idle;
            }
        }
        else if (Math.Abs(rb.velocity.x) > 4.5f)
        {
            state = State.running;
        }
        else
        {
            state = State.idle;
        }
    }
}
