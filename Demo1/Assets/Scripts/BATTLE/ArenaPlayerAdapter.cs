using UnityEngine;

/// <summary>
/// 競技場場景專用：
/// - 在 Awake 將 PlayerController 切到 arenaMode
/// - 可選：進場時把原本 deathMenu 關掉（死亡由 ArenaManager 控）
/// </summary>
[DisallowMultipleComponent]
public class ArenaPlayerAdapter : MonoBehaviour
{
    [Tooltip("若你的 PlayerController 上已指定 deathMenu，進競技場時是否先隱藏它。")]
    public bool hideDeathMenuOnStart = true;

    private PlayerController pc;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("[ArenaPlayerAdapter] 找不到 PlayerController！");
            enabled = false;
            return;
        }

        pc.arenaMode = true; // 關閉劇情/存檔/切場等行為
    }

    private void Start()
    {
        if (hideDeathMenuOnStart && pc.deathMenu != null)
            pc.deathMenu.SetActive(false);
    }
}
