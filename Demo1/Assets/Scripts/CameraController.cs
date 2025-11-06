using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public Transform player;         // 傳統跟隨用（非Cinemachine可選）
    private Transform target;        // 實際追蹤目標
    public float smoothSpeed = 5f;

    private CinemachineVirtualCamera vcam;

    void OnEnable()
    {
        // 訂閱事件
        PlayerController.OnPlayerReady += HandlePlayerReady;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        PlayerController.OnPlayerReady -= HandlePlayerReady;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Awake()
    {
        // 嘗試找到 Virtual Camera
        vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam == null)
            Debug.LogWarning("No CinemachineVirtualCamera found in scene. CameraController will fallback to manual transform.");
    }

    // 場景切換後保險：等幀綁定
    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StartCoroutine(BindNextFrame());
    }

    IEnumerator BindNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        // 優先使用 DDOL PlayerController
        if (PlayerController.Instance != null)
        {
            SetTarget(PlayerController.Instance.transform);

            if (vcam != null)
            {
                vcam.Follow = PlayerController.Instance.transform;
                vcam.LookAt = PlayerController.Instance.transform;
                Debug.Log($"Cinemachine VCam follow set to {PlayerController.Instance.name}");
            }
            yield break;
        }

        // 否則用 Tag 找
        var pgo = GameObject.FindGameObjectWithTag("Player");
        if (pgo)
        {
            SetTarget(pgo.transform);
            if (vcam != null)
            {
                vcam.Follow = pgo.transform;
                vcam.LookAt = pgo.transform;
                Debug.Log($"Cinemachine VCam follow set to Tag Player {pgo.name}");
            }
        }
    }

    private void HandlePlayerReady(PlayerController playerCtrl)
    {
        SetTarget(playerCtrl.transform);

        if (vcam != null)
        {
            vcam.Follow = playerCtrl.transform;
            vcam.LookAt = playerCtrl.transform;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        player = newTarget;

        // 立即對齊一次，避免第一幀看到相機還沒移動
        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);

        Debug.Log($"Camera following target: {target.name}");
    }

    public void ResetTarget()
    {
        target = player;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 傳統平滑追蹤 (當你沒有使用 Cinemachine 或額外控制)
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}

