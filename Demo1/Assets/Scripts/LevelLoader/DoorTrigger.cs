using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DoorTrigger : MonoBehaviour
{
    //此腳本只負責偵測是否開啟傳送門
    public UnityEvent onEnemiesEnd;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // 確保是玩家進入
        {
            if (AllEnemiesDefeated()) // 檢查敵人是否全部消失
            {
                onEnemiesEnd?.Invoke();
            }
            else
            {
                Debug.Log("還有敵人，無法進入下一個場景！");
            }
        }
    }

    // 檢查是否所有敵人都已經消失
    private bool AllEnemiesDefeated()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0; // 如果沒有敵人，就回傳 true
    }
}
