using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Save‑slot 選單：可在 <see cref="MainMenu"/> 呼叫下切換「建立新檔」或「讀取舊檔」模式。
/// </summary>
public class SaveSlotMenu : MonoBehaviour
{
    public enum Mode { New, Load }

    [Header("References")]
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Button   backButton;
    [SerializeField] private SaveSlot[] saveSlots;   // Inspector 拖入

    private Mode currentMode;
    private Dictionary<string, GameData> cachedProfiles;

    private void Awake()
    {
        // 若沒手動拖 slot，動態抓子物件補救
        if (saveSlots == null || saveSlots.Length == 0)
            saveSlots = GetComponentsInChildren<SaveSlot>(true);
    }

    // ───────────────────── public API ─────────────────────
    public void ActivateMenu(Mode mode)
    {
        currentMode = mode;
        gameObject.SetActive(true);

        cachedProfiles = DataPersistenceManager.instance.GetAllProfilesGameData();

        // 依 mode & 檔案狀況刷新 slot
        foreach (var slot in saveSlots)
        {
            cachedProfiles.TryGetValue(slot.GetProfileId(), out GameData gd);
            slot.SetData(gd);
            bool canClick = (mode == Mode.New) || gd != null;
            slot.SetInteractable(canClick);
        }
    }

    // Button event from SaveSlot prefab
    public void OnSaveSlotClicked(SaveSlot slot)
    {
        DisableMenuButtons();
        string pid = slot.GetProfileId();

        // 切換到目標 Profile
        DataPersistenceManager.instance.ChangeSelectedProfileId(pid);

        if (currentMode == Mode.New)
        {
            // 若此格已有存檔，彈覆寫確認視窗（示意）
            if (cachedProfiles.ContainsKey(pid))
            {
                // TODO: 你的 UI 彈窗 → 若玩家取消，則 ReEnable 並 return
                // 這裡先直接覆寫
            }
            DataPersistenceManager.instance.NewGame();
        }
        // Load 模式不呼叫 NewGame()，直接沿用舊檔

        SceneManager.LoadSceneAsync("SecondScene");
    }

    public void OnBackClicked()
    {
        mainMenu?.ActivateMenu();
        gameObject.SetActive(false);
    }

    // ──────────────────── helpers ─────────────────────────
    private void DisableMenuButtons()
    {
        foreach (var s in saveSlots) s.SetInteractable(false);
        backButton.interactable = false;
    }
}
