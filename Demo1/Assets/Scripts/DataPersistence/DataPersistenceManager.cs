using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coordinates all <see cref="IDataPersistence"/> objects and the <see cref="FileDataHandler"/>.
/// ‑ 支援多存檔 (profile 0 / 1 / 2 …)。
/// ‑ 保證任何時候 <c>selectedProfileId</c> 都不是空字串，預設為 "0"。
/// ‑ 會把最後一次載入 / 存檔的 profile 記在 <see cref="PlayerPrefs"/>，下次開遊戲自動選同一格。
/// </summary>
public class DataPersistenceManager : MonoBehaviour
{
    // ────────────────────────────── Inspector ──────────────────────────────
    [Header("Debugging")]
    [Tooltip("若於載入時找不到存檔且勾選此項，會立即建立並寫出一份空的存檔。")]
    [SerializeField] private bool initializeDataIfNull = false;

    [Header("File Storage Config")]
    [Tooltip("存檔檔名。預設 data.name (可自訂)")]
    [SerializeField] private string fileName = "data.name";

    // ────────────────────────────── Fields ──────────────────────────────
    private const string PLAYER_PREFS_LAST_PROFILE = "LAST_PROFILE_ID";
    private const string DEFAULT_PROFILE_ID = "0";

    private string selectedProfileId;
    private GameData gameData;

    private FileDataHandler dataHandler;
    private List<IDataPersistence> dataPersistenceObjects;

    public static DataPersistenceManager instance { get; private set; }

    // ────────────────────────────── MonoBehaviour ─────────────────────────
    private void Awake()
    {
        // Singleton 保護
        if (instance != null)
        {
            Debug.Log("Found more than one DataPersistenceManager. Destroying the newest one.");
            Destroy(gameObject);
            return;
            
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 檔名保險：若 Inspector 留空就用預設值
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "data.name";

        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);

        // 從 PlayerPrefs 讀取最後一次使用的存檔格；若沒有則用 DEFAULT_PROFILE_ID
        selectedProfileId = PlayerPrefs.GetString(PLAYER_PREFS_LAST_PROFILE, DEFAULT_PROFILE_ID);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded   += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded   -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        SaveGame();
    }

    // ────────────────────────────── Public API ────────────────────────────
    /// <summary>
    /// 切換當前 profile，並立即嘗試載入。
    /// </summary>
    public void ChangeSelectedProfileId(string newProfileId)
    {
        if (string.IsNullOrWhiteSpace(newProfileId))
        {
            Debug.LogWarning("[DataPersistence] 傳入的 profileId 為空，忽略。");
            return;
        }

        selectedProfileId = newProfileId;
        PlayerPrefs.SetString(PLAYER_PREFS_LAST_PROFILE, selectedProfileId);
        PlayerPrefs.Save();
        LoadGame();
    }

    public void NewGame()
    {
        gameData = new GameData();
        SaveGame(); // 立即寫檔以保證新格被建立
    }

    public bool HasGameData => gameData != null;

    public Dictionary<string, GameData> GetAllProfilesGameData() => dataHandler.LoadAllProfiles();

    // ────────────────────────────── Core Logic ───────────────────────────
    private void LoadGame()
    {
        // 嘗試讀取
        gameData = dataHandler.Load(selectedProfileId);

        if (gameData == null)
        {
            Debug.Log($"[DataPersistence] No save found for profile '{selectedProfileId}'.");
            if (initializeDataIfNull)
            {
                Debug.Log("[DataPersistence] initializeDataIfNull = true → 建立新遊戲資料並寫檔");
                NewGame();
            }
            return;
        }

        // 將資料分發給所有實作 IDataPersistence 的物件
        foreach (var obj in dataPersistenceObjects)
            obj.LoadData(gameData);
    }

    private void SaveGame()
    {
        if (gameData == null)
        {
            Debug.LogWarning("[DataPersistence] SaveGame() 被呼叫但 gameData 為 null");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedProfileId))
        {
            Debug.LogWarning("[DataPersistence] SaveGame() 失敗：profileId 為空");
            return;
        }

        foreach (var obj in dataPersistenceObjects)
            obj.SaveData(ref gameData);

        dataHandler.Save(gameData, selectedProfileId);
    }

    private void OnApplicationQuit() => SaveGame();

    // ────────────────────────────── Helpers ───────────────────────────────
    private static List<IDataPersistence> FindAllDataPersistenceObjects() =>
        FindObjectsOfType<MonoBehaviour>(true)  // 包含 disabled 物件
            .OfType<IDataPersistence>()
            .ToList();
}
