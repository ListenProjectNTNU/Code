using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFireball : MonoBehaviour
{
    private GameObject player;
    public GameObject bullet;
    public Transform bulletPos;
    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);
        //Debug.Log(distance);
        if (distance > 2)
        {
            timer += Time.deltaTime;

            if (timer > 1f)
            {
                timer = 0;
                shoot();
            }
        }
        void shoot()
        {
            Instantiate(bullet, bulletPos.position, Quaternion.identity);
        }
    }
}
