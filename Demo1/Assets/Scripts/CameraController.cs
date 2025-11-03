using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Transform target;
    public float smoothSpeed = 5f;

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

    // 切場後保險：還是等一楨再抓一次
    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        StartCoroutine(BindNextFrame());
    }

    IEnumerator BindNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        var pgo = GameObject.FindGameObjectWithTag("Player");
        if (pgo) SetTarget(pgo.transform);
    }

    private void HandlePlayerReady(PlayerController playerCtrl)
    {
        SetTarget(playerCtrl.transform);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        player = newTarget;
        // 立即對齊一次，避免第一幀看到相機還沒移動
        if (target != null)
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
    }

    public void ResetTarget()
    {
        target = player;
    }

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
