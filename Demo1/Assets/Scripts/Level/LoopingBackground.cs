using UnityEngine;

public class LoopingBackground : MonoBehaviour
{
    public float speed = 2f; // 背景移動速度
    private float startPos;
    private float length;

    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // 背景向左移動
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // 當背景超出一半長度時重設位置
        if (transform.position.x <= startPos - length)
        {
            transform.position = new Vector3(startPos, transform.position.y, transform.position.z);
        }
    }
}