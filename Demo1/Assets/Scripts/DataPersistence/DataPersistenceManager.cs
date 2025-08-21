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
    public static DataPersistenceManager instance { get; private set; }

    [SerializeField] bool  initializeDataIfNull = true;
    [SerializeField] string fileName = "data.name";

    const string PREF_LAST = "lastProfileId", DEFAULT_PROFILE = "0";
    string  selectedProfileId;
    GameData gameData;

    FileDataHandler dataHandler;
    List<IDataPersistence> objs;
    bool alreadySwitched;
    bool skipNextSave = false;

    void Awake()
    {
        if (instance){ Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);

        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        selectedProfileId = PlayerPrefs.GetString(PREF_LAST, DEFAULT_PROFILE);
    }

    void OnEnable(){ SceneManager.sceneLoaded += Loaded;  SceneManager.sceneUnloaded += Unloaded; }
    void OnDisable(){ SceneManager.sceneLoaded -= Loaded; SceneManager.sceneUnloaded -= Unloaded; }

    /* ───── 事件 ───── */
    void Loaded(Scene sc, LoadSceneMode m)
    {
        objs = FindAll();
        if (sc.name != "MainMenu") LoadGame();   // MainMenu 由 UI 決定
    }
    void Unloaded(Scene sc)
    {
        if (skipNextSave) {        // ★ 只跳過第一次
            skipNextSave = false;  //   之後恢復正常
            return;
        }
        SaveGame();
    }
    void OnApplicationQuit(){ SaveGame(); }

    /* ───── 公開 API ───── */
    public void ChangeSelectedProfileId(string id){ selectedProfileId=id; PlayerPrefs.SetString(PREF_LAST,id); }
    public void NewGame(string startScene="FirstScene"){ gameData=new GameData{sceneName=startScene}; SaveGame(); }
    public bool HasGameData => gameData!=null;
    public Dictionary<string,GameData> GetAllProfilesGameData()=>dataHandler.LoadAllProfiles();

    /* ───── Load / Save ───── */
    public void LoadGame()
    {
        gameData = dataHandler.Load(selectedProfileId);

        if (gameData == null)
        {
            if (!initializeDataIfNull) return;
            NewGame("FirstScene");           // 首次建立
            return;                           // NewGame 內已有 LoadScene
        }

        string savedScene   = gameData.sceneName;
        string currentScene = SceneManager.GetActiveScene().name;

        if (!alreadySwitched && currentScene != savedScene)
        {
            alreadySwitched = true;
            skipNextSave    = true;           // ★ 告訴 Unloaded() 暫時別存
            SceneManager.LoadScene(savedScene);
            return;
        }

        /* 走到這裡 = 已在正確場景 */
        gameData.sceneName = currentScene;    // 同步
        objs = FindAll();
        foreach (var o in objs) o.LoadData(gameData);
    }

    public void SaveGame()
    {
        if (gameData == null) return;

        gameData.sceneName = SceneManager.GetActiveScene().name; // ① 先記錄場景
        objs = FindAll();
        foreach (var o in objs) o.SaveData(ref gameData);        // ② 再把資料寫進 gameData
        dataHandler.Save(gameData, selectedProfileId);           // ③ 最後存檔
    }

    /* ───── utils ───── */
    List<IDataPersistence> FindAll() =>
        FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistence>().ToList();
}
