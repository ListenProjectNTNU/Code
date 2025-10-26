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

    // 🔹新增：可選的對話觸發範圍（Collider Trigger）
    [Header("可選：碰到此 Collider 觸發最後對話")]
    public bool isDialogueDoor = false; // 這個門是不是要對話的門？
    public Collider2D dialogueTriggerCollider;
    [Tooltip("可選：SceneController 參考用來通知進入對話")]
    public Scene2Controller sceneController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return; // 只偵測玩家
        Debug.Log("玩家觸發OnTriggerEnter2D → " + gameObject.name);

        // 🌀 LoopingBG 門
        if (linkedLoopingBG != null)
        {
            Debug.Log("🚪 玩家碰到門（LoopingBG 版本）");
            LoopingBackground[] allBGs = FindObjectsOfType<LoopingBackground>();
            foreach (var bg in allBGs)
            {
                bg.OnDoorTriggered();
            }
            return;
        }

        // 🗣️ 對話門（特別的門）
        if (isDialogueDoor) // 👈 改成用一個 bool 旗標判定這是不是對話門
        {
            if (AllEnemiesDefeated())
            {
                Debug.Log("🚪 玩家碰到【對話門】，敵人已清空 → 觸發對話");
                if (sceneController != null)
                {
                    sceneController.TriggerPortalDialogue();
                }
            }
            else
            {
                Debug.Log("🚪 玩家碰到【對話門】，但敵人還存在");
            }
            return;
        }

        // 🧩 一般傳送門
        if (AllEnemiesDefeated())
        {
            onEnemiesEnd?.Invoke();
        }
        else
        {
            Debug.Log("🚪 玩家碰到【傳送門】，但敵人還存在！");
        }
    }

    // 檢查是否所有敵人都已經消失
    private bool AllEnemiesDefeated()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies.Length == 0; // 如果沒有敵人，就回傳 true
    }
}
