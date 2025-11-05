using UnityEngine;

public class SpecialPointFollower : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;            // 追蹤目標（玩家）
    public Vector3   offset = Vector3.zero;
    public bool      follow = false;    // 開關
    public bool      smooth = true;
    [Range(0.01f, 0.5f)]
    public float     smoothTime = 0.08f;

    private Vector3 _vel;

    void LateUpdate()
    {
        if (!follow || !target) return;

        Vector3 dest = target.position + offset;
        if (smooth)
            transform.position = Vector3.SmoothDamp(transform.position, dest, ref _vel, smoothTime);
        else
            transform.position = dest;
    }

    // 給外部（Death）方便呼叫
    public void Bind(Transform t) => target = t;
    public void StartFollow()    => follow = true;
    public void StopFollow()     => follow = false;
}
