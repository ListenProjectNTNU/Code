using UnityEngine;

public class GameManager : MonoBehaviour
{
    //internal static object instance;
    public static GameManager instance;
    public GameObject audioManagerPrefab;
    public AudioManager audioManager;

    public GameObject player; // 拖入 Player 物件

     void Start()
    {
        if (instance == null) 
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 確保 GameManager 不會被銷毀

            if (audioManagerPrefab != null) 
            {
                audioManager = Instantiate(audioManagerPrefab).GetComponent<AudioManager>();
                DontDestroyOnLoad(audioManager.gameObject); // 確保 AudioManager 不會被銷毀
            }
            else 
            {
                Debug.LogError("audioManagerPrefab 尚未在 Inspector 設定！");
            }
        }
        else 
        {
            Destroy(gameObject); // 避免產生多個 GameManager
        }
    }

    public void RevivePlayer()
    {
        Debug.Log("Reviving player...");
        player.SetActive(true);
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.RevivePlayer();
        }
    }
}
