using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("SaveSlotMenu")]
    [SerializeField] private SaveSlotMenu saveSlotMenu;

    [Header("Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueGameButton;  // ← 這顆就是「進 BATTLE」
    [SerializeField] private Button loadGameButton;

    [Header("Scenes")]
    [SerializeField] private string arenaSceneName = "BATTLE";

    private void Start()
    {
        // ✅ 不再依賴存檔狀態，Continue 永遠可按（因為它是進競技場）
        if (continueGameButton) continueGameButton.interactable = true;

        // （可選）若你想避免誤觸，Load 在沒有存檔時才關閉：
        var dpm = DataPersistenceManager.instance;
        bool hasData = dpm != null && dpm.HasGameData;
        if (loadGameButton) loadGameButton.interactable = true;   // 你也可以改成 hasData
        if (newGameButton) newGameButton.interactable = true;     // 新遊戲永遠可按
    }

    public void OnNewGameClicked()
    {
        // 只有真的按 New Game 才開存檔面板 → 不會在主選單自動讀檔
        if (!saveSlotMenu) return;
        saveSlotMenu.ActivateMenu(SaveSlotMenu.Mode.New);
        DeactivateMenu();
    }

    public void OnLoadGameClicked()
    {
        // 只有真的按 Load 才開存檔面板 → 這時候才會掃描 Profiles
        if (!saveSlotMenu) return;
        saveSlotMenu.ActivateMenu(SaveSlotMenu.Mode.Load);
        DeactivateMenu();
    }

    // ✅ Continue 按鈕直接當作「進入競技場」
    public void OnContinueGameClicked()
    {
        EnterArena();
    }

    public void EnterArena()
    {
        Time.timeScale = 1f;

        // 清理：如果場上有被 DDOL 帶著的「劇情玩家」，先移除，避免污染競技場
        var pc = PlayerController.Instance;
        if (pc && !pc.arenaMode)
        {
            Destroy(pc.gameObject);
        }

        // 防呆：場景是否已加入 Build Settings
        if (!Application.CanStreamedLevelBeLoaded(arenaSceneName))
        {
            Debug.LogError($"Scene '{arenaSceneName}' 不在 Build Settings。請到 File → Build Settings → Add Open Scenes 加入。");
    #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(
                "Missing Scene",
                $"Scene '{arenaSceneName}' 不在 Build Settings。\n請開啟該場景後到 File → Build Settings → Add Open Scenes。",
                "OK"
            );
    #endif
            return;
        }

        SceneManager.LoadScene(arenaSceneName, LoadSceneMode.Single);
    }

    public void DeactivateMenu() => gameObject.SetActive(false);
    public void ActivateMenu()   => gameObject.SetActive(true);
}
