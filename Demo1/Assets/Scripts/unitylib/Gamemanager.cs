using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 單例（兩個別名都給，I / instance 都可用）
    public static GameManager I { get; private set; }
    public static GameManager instance { get; private set; }

    [Header("Player")]
    public GameObject player;           // 場上那隻玩家（若做常駐，記得也 DontDestroyOnLoad）
    public GameObject playerPrefab;     // 若要在新場景生成玩家，可指定 prefab（可選）

    [Header("Scene Spawn")]
    public string NextSpawnId = "default"; // 轉場時記住要落地的 spawnId

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 給 ScenePortal 用的轉場方法：記住下一個 spawnId，然後載入場景
    /// </summary>
    public void GoToScene(string sceneName, string spawnId)
    {
        NextSpawnId = string.IsNullOrEmpty(spawnId) ? "default" : spawnId;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void RevivePlayer()
    {
        Debug.Log("Reviving player...");
        if (!player) { Debug.LogWarning("GameManager.player 未指定"); return; }

        player.SetActive(true);
        var playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.RevivePlayer();
        }
    }
}