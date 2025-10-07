using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Root Panels")]
    [SerializeField] GameObject menuRoot;       // 指向「Panel」(主選單視窗)
    [SerializeField] GameObject settingsPanel;  // 指向「SettingsPanel」

    [Header("Buttons")]
    [SerializeField] Button btnResume;
    [SerializeField] Button btnRestart;
    [SerializeField] Button btnSettings;
    [SerializeField] Button btnMainMenu;
    [SerializeField] Button btnQuit;

    [Header("Settings Widgets (Optional)")]
    [SerializeField] Slider volumeSlider;
    [SerializeField] Button btnSettingsBack;

    bool isPaused;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // 防呆：如果沒綁，試著在場景中自動找
        if (menuRoot == null)     menuRoot = transform.Find("Canvas/Panel")?.gameObject;
        if (settingsPanel == null) settingsPanel = transform.Find("Canvas/SettingsPanel")?.gameObject;

        // 綁定按鈕
        if (btnResume)   btnResume.onClick.AddListener(Resume);
        if (btnRestart)  btnRestart.onClick.AddListener(RestartLevel);
        if (btnSettings) btnSettings.onClick.AddListener(OpenSettings);
        if (btnMainMenu) btnMainMenu.onClick.AddListener(ToMainMenu);
        if (btnQuit)     btnQuit.onClick.AddListener(QuitGame);

        if (btnSettingsBack) btnSettingsBack.onClick.AddListener(CloseSettings);
        if (volumeSlider)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
        }

        HideAll();
        ApplyPause(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) Pause();
            else
            {
                // 若在設定頁，先退回主選單；否則直接 Resume
                if (settingsPanel && settingsPanel.activeSelf) CloseSettings();
                else Resume();
            }
        }
    }

    void Pause()
    {
        isPaused = true;
        ApplyPause(true);
        ShowMenu();
    }

    public void Resume()
    {
        isPaused = false;
        ApplyPause(false);
        HideAll();
    }

    void ApplyPause(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
        Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = pause;
        // 如需暫停 Audio，可補上：AudioListener.pause = pause;
    }

    void ShowMenu()
    {
        if (menuRoot) menuRoot.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void HideAll()
    {
        if (menuRoot) menuRoot.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void OpenSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        if (menuRoot) menuRoot.SetActive(false);
    }

    void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (menuRoot) menuRoot.SetActive(true);
    }

    void RestartLevel()
    {
        // 關閉選單並恢復時間流動
        isPaused = false;
        ApplyPause(false);
        HideAll();  // ✅ 關閉所有 UI (menuRoot, settingsPanel)

        // 重新載入場景
        var idx = SceneManager.GetActiveScene().buildIndex;
        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(idx);
    }

    void ToMainMenu()
    {
        // TODO: 換成你的主選單場景名稱或 Build Index
        ApplyPause(false);
        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    void QuitGame()
    {
        // 在 Editor 不會關閉；打包後有效
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
