using UnityEngine;
using UnityEngine.Events;

public class DoorTrigger : MonoBehaviour
{
    // 🔹原功能：敵人清空後才可進入門
    [Header("戰鬥門用呼叫LevelLoader")]
    public UnityEvent onEnemiesEnd;

    // 🔹新增：可選的 LoopingBackground（非戰鬥型門使用）
    [Header("可選：連動的 LoopingBackground（非戰鬥門用）")]
    public LoopingBackground linkedLoopingBG;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return; // 只偵測玩家

        Debug.Log("玩家觸發OnTriggerEnter2D");
        if (linkedLoopingBG != null)
        {
            Debug.Log("🚪 玩家碰到門（LoopingBG 版本）");

            // 🧩 找出所有的 LoopingBackground，一起停下
            LoopingBackground[] allBGs = FindObjectsOfType<LoopingBackground>();
            foreach (var bg in allBGs)
            {
                bg.OnDoorTriggered();
            }

            return;
        }
        // 🔹以下是原本傳送門的戰鬥邏輯，完全不動
        if (AllEnemiesDefeated())
        {
            onEnemiesEnd?.Invoke();
        }
        else
        {
            Debug.Log("還有敵人，無法進入下一個場景！");
        }
    }

    // 檢查是否所有敵人都已經消失
    private bool AllEnemiesDefeated()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0; // 如果沒有敵人，就回傳 true
    }
}
