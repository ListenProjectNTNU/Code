using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;       // 主角
    private Transform target;      // 當前追蹤目標

    public float smoothSpeed = 5f; // 平滑跟隨速度

    void Start()
    {
        target = player;           // 預設追蹤主角
    }

    void Update()
    {
        if (target == null) return;

        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    // 外部呼叫改變追蹤目標
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log("Set New Target");
    }

    // 外部呼叫恢復追蹤主角
    public void ResetTarget()
    {
        target = player;
        Debug.Log("Return to Player");
    }
}