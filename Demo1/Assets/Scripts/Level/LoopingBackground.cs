using UnityEngine;

public class LoopingBackground : MonoBehaviour
{
    [SerializeField] private float speed = 8f;                // 背景移動速度
    public SpriteRenderer reference;        // 若這張是第二張，指定前一張的 SpriteRenderer
    private float startPos;
    private float length;              

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        length = sr.bounds.size.x;

        // 如果指定了前一張圖，就自動對齊
        if (reference != null)
        {
            float refWidth = reference.bounds.size.x;
            transform.position = new Vector3(reference.transform.position.x + refWidth, reference.transform.position.y, reference.transform.position.z);
        }

        startPos = transform.position.x;
    }

    void Update()
    {
        // 背景往左移動
        transform.Translate(Vector2.left * speed * Time.deltaTime);

        // 當背景超出畫面長度，重置回初始位置
        if (transform.position.x <= startPos - length)
        {
            transform.position = new Vector3(startPos, transform.position.y, transform.position.z);
        }
    }
}
