using UnityEngine;
using System; // 為了使用 Action 事件

public class CameraController : MonoBehaviour
{
    public Transform player;       // 主角
    private Transform target;      // 當前追蹤目標

    public float smoothSpeed = 5f; // 平滑跟隨速度

    private void OnEnable()
    {
        // 當 PlayerController 發出事件時接收
        PlayerController.OnPlayerReady += HandlePlayerReady;
    }

    private void OnDisable()
    {
        // 避免記憶體洩漏（事件反註冊）
        PlayerController.OnPlayerReady -= HandlePlayerReady;
    }

    private void Start()
    {
        // 如果一開始就已經有玩家（例如在同場景中），直接抓取
        if (player == null && PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }

        target = player; // 預設追蹤主角
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    // 外部呼叫改變追蹤目標
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log("[CameraController] Set New Target");
    }

    // 外部呼叫恢復追蹤主角
    public void ResetTarget()
    {
        target = player;
        Debug.Log("[CameraController] Return to Player");
    }

    // 當玩家生成或準備好時會被呼叫
    private void HandlePlayerReady(PlayerController playerController)
    {
        player = playerController.transform;
        target = player;
        Debug.Log("[CameraController] Player Ready, Camera Target Set!");
    }
}
