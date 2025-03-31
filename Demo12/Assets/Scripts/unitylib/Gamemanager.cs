using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player; // 拖入 Player 物件

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
