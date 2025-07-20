using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central hub that coordinates every <see cref="IDataPersistence"/> object and the <see cref="FileDataHandler"/>,
/// enabling **multi‑profile save / load**.
/// </summary>
public class DataPersistenceManager : MonoBehaviour
{
    // ─────────────────────────────── Singleton ───────────────────────────────
    public static DataPersistenceManager instance { get; private set; }

    // ─────────────────────────────── Inspector ───────────────────────────────
    [Header("Debugging")]
    [SerializeField] private bool initializeDataIfNull = true;

    [Header("File Storage Config")]
    [SerializeField] private string fileName = "data.name"; // 若 Inspector 空白時使用這個

    // ─────────────────────────────── Runtime ────────────────────────────────
    private const string PREF_LAST_PROFILE = "lastProfileId";
    private const string DEFAULT_PROFILE   = "0";

    private string   selectedProfileId;
    private GameData gameData;

    private FileDataHandler              dataHandler;
    private List<IDataPersistence>       dataPersistenceObjects;

    // ─────────────────────────────── Life‑cycle ─────────────────────────────
    private void Awake()
    {
        // Singleton 保護
        if (instance != null)
        {
            Debug.LogWarning("Found more than one DataPersistenceManager. Destroying the newest one.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 檔案處理器初始化
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "data.name";
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);

        // 讀取上次使用的 Profile，若無則用預設 0
        selectedProfileId = PlayerPrefs.GetString(PREF_LAST_PROFILE, DEFAULT_PROFILE);
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

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    // ─────────────────────────────── Public API ─────────────────────────────
    public void ChangeSelectedProfileId(string newProfileId)
    {
        if (string.IsNullOrWhiteSpace(newProfileId)) return;
        selectedProfileId = newProfileId;
        PlayerPrefs.SetString(PREF_LAST_PROFILE, newProfileId);
        LoadGame();
    }

    public void NewGame()
    {
        gameData = new GameData();
        // 立即存檔以確保磁碟上有檔案，也能讓 LoadGame() 找到
        SaveGame();
    }

    public bool HasGameData => gameData != null;

    public Dictionary<string, GameData> GetAllProfilesGameData() => dataHandler.LoadAllProfiles();

    // For UI 快速取得最後 profile
    public string GetLastProfileId() => selectedProfileId;

    // ─────────────────────────────── Core Logic ────────────────────────────
    public void LoadGame()
    {
        gameData = dataHandler.Load(selectedProfileId);

        if (gameData == null)
        {
            if (initializeDataIfNull)
            {
                Debug.Log("No data found. Creating new game for profile " + selectedProfileId);
                NewGame();
            }
            else
            {
                Debug.Log("No data was found for profile " + selectedProfileId + " and initializeDataIfNull == false.");
                return;
            }
        }

        // 將資料派發給所有 IDataPersistence 物件
        foreach (var dpObj in dataPersistenceObjects)
            dpObj.LoadData(gameData);
    }

    public void SaveGame()
{
    if (gameData == null)
    {
        Debug.LogWarning("[SaveGame] No gameData — nothing to save.");
        return;
    }

    // ★ 每次存檔前，重新收集「還活著」的 IDataPersistence 物件
    dataPersistenceObjects = FindAllDataPersistenceObjects();

    // ★ 逐一呼叫，但跳過已被銷毀的物件
    foreach (var dpObj in dataPersistenceObjects)
    {
        if (dpObj == null) continue;                     // ① 參考已是 null
        if (dpObj is MonoBehaviour mb && !mb) continue;  // ② Unity 假 null (已 Destroy)
        dpObj.SaveData(ref gameData);
    }

    // 記錄目前場景名稱
    gameData.sceneName = SceneManager.GetActiveScene().name;

    // 最終寫入磁碟
    dataHandler.Save(gameData, selectedProfileId);
}

    // ─────────────────────────────── Helper Methods ────────────────────────
    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return FindObjectsOfType<MonoBehaviour>()
               .OfType<IDataPersistence>()
               .ToList();
    }
}