using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootPickupDelay : MonoBehaviour
{
    public float delayTime = 0.3f; // 你可以在 Inspector 自訂這個數字

    void Start()
    {
        StartCoroutine(EnableColliderAfterDelay());
    }

    IEnumerator EnableColliderAfterDelay()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;  // 先關閉碰撞
            yield return new WaitForSeconds(delayTime);
            col.enabled = true;   // 延遲後再開啟碰撞
        }
    }
}
