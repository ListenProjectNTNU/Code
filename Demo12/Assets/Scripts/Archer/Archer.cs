using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : MonoBehaviour
{
    private Collider2D coll;
    private Rigidbody2D rb;
    private Animator anim;
    private GameObject player;
    public GameObject bullet;
    public Transform bulletPos;
    public bool isFlipped = false;
     [SerializeField] private LayerMask ground;

    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);
        //Debug.Log(distance);
        if (distance < 8)
        {
            timer += Time.deltaTime;
            anim.SetBool("Attacking",true);

            if(timer > 1f)
            {
            timer = 0;
            shoot();
            }
        }
        else
        {
            anim.SetBool("Attacking", false);
        }        
    }

    public void LookAtPlayer()
	{
		Vector3 flipped = transform.localScale;
		flipped.z *= -1f;

		if (transform.position.x > player.transform.position.x && isFlipped)
		{
			transform.localScale = flipped;
			transform.Rotate(0f, 180f, 0f);
			isFlipped = false;
		}
		else if (transform.position.x < player.transform.position.x && !isFlipped)
		{
			transform.localScale = flipped;
			transform.Rotate(0f, 180f, 0f);
			isFlipped = true;
		}
	}

    void shoot()
    {
    Instantiate(bullet, bulletPos.position, Quaternion.identity);
    }
}
