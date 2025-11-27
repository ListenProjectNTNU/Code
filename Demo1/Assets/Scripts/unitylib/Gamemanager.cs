using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public static GameManager instance { get; private set; }

    [Header("Player")]
    public GameObject player;
    public GameObject playerPrefab;

    [Header("Scene Spawn")]
    public string NextSpawnId = "default";

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        instance = this;

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Debug.Log("🌟 GameManager Awake，單例建立完成");
    }

    public void GoToScene(string sceneName, string spawnId)
    {
        NextSpawnId = string.IsNullOrEmpty(spawnId) ? "default" : spawnId;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void RevivePlayer()
    {
        Debug.Log("GameManager.RevivePlayer() 被呼叫");

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("GameManager：場景中找不到 Player");
                return;
            }
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("GameManager：Player 上沒有 PlayerController");
            return;
        }

        pc.RevivePlayer();
    }

    public void RestartCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        Debug.Log("Restart scene: " + scene.name);
        SceneManager.LoadScene(scene.name);
    }
}
