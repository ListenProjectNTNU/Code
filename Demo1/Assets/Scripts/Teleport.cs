using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    public string targetSceneName; // 設定要切換的場景名稱
    public Animator transition;//換場動畫
    public float transitionTime = 1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // 確保是玩家進入
        {
            if (AllEnemiesDefeated()) // 檢查敵人是否全部消失
            {
                //play animation
                transition.SetTrigger("Start");

                //wait
                //yield return new WaitForSeconds(transitionTime);
                
                SceneManager.LoadScene(targetSceneName); // 切換場景
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
