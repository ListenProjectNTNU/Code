using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFrog : MonoBehaviour
{
    public int maxHealth = 100;
    int currentHealth;
    [SerializeField]private float leftCap;
    [SerializeField]private float rightCap;
    private Quaternion fixedRotation;
    public LayerMask ground;

    [SerializeField]private float jumpLength = 5f;
    [SerializeField]private float jumpHeight = 5f;
    
    private bool facingLeft = true;
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;

    private enum State {idle, jumping, falling};
    private State state = State.idle;

    private void Start()
    {
        currentHealth = maxHealth;
        fixedRotation = transform.rotation;

        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        transform.rotation = fixedRotation;
        //Move();
        AnimationState();
        anim.SetInteger("state", (int)state);
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        //play hurt animation
        anim.SetTrigger("Hurt");

        if (currentHealth < 0)
        {
            Die();
        }
    }
    public void Die()
    {
        Debug.Log("Enemy Died");
        //play die animation
        anim.SetBool("IsDead",true);
        //disable enemy
        coll.enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        this.enabled = false;
    }
     private void Move()
    {
        
        if (facingLeft == true)
        {
            //test if we are beyonded the leftCap
            if (transform.position.x > leftCap)
            {
                //make sure sprite is facing left
                if (transform.localScale.x != 1)
                {
                    transform.localScale = new Vector3(1, 1);
                }

                //test if frog on the ground, if so then jump left
                if (coll.IsTouchingLayers(ground))
                {
                    //Debug.Log("jump left");
                    rb.velocity = new Vector2(-jumpLength, jumpHeight);
                    state = State.jumping;
                }
            }
            else
            {
                //if not we are going to face right
                facingLeft = false;
            }
        }
        else
        {
            if (transform.localScale.x != -1)
            {
                transform.localScale = new Vector3(-1, 1);
            }
            Debug.Log("facing right");
            if (facingLeft == false && transform.position.x < rightCap)
            {
                //test if frog on the ground, if so then jump right
                if (coll.IsTouchingLayers(ground))
                {
                    //Debug.Log("jump right");
                    rb.velocity = new Vector2(jumpLength, jumpHeight);
                    state = State.jumping;
                }
            }
            else
            {
                //if not we are going to face left
                facingLeft = transform;
                transform.localScale = new Vector2(1, 1);
                facingLeft = true;
                //Debug.Log("facing left");
            }
        }
    } 
    private void AnimationState()
    {
        if (state == State.jumping)
        {
            //state = State.idle;
            if (rb.velocity.y < .1f)
            {
                //Debug.Log("fall");
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
        else
        {
            state = State.idle;
        }
    }
}
