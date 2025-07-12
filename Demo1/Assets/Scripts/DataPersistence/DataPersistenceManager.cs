using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
public class DataPersistenceManager : MonoBehaviour
{
    //singleton class

    [Header("Debugging")]
    [SerializeField] private bool initializeDataIfNull = false;

    [Header("File Storage Config")]
    [SerializeField] private string fileName;
    private GameData gameData;
    private FileDataHandler dataHandler;
    public static DataPersistenceManager instance { get; private set;}
    private List<IDataPersistence> dataPersistenceObjects;
    private void Awake()
    {
        if(instance != null)
        {
            Debug.Log("Found more than one Data Persistence Manager in the Scene. Destroying the newest one.");
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
    }

    public void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    public void OnSceneLoaded(Scene scenen, LoadSceneMode mode)
    {
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    public void OnSceneUnloaded(Scene scene)
    {
        SaveGame();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }
    public void LoadGame()
    {
        //Load any saved data from a flie using the data handler
        this.gameData = dataHandler.Load();


        if(this.gameData == null && initializeDataIfNull)
        {
            NewGame();
        }
        //如果沒有就NewGame 
        if (this.gameData == null)
        {
            Debug.Log("No data was found. A New Game needs to be started before data can be loaded.");
            return;
        }
        // 把所以資料載入給需要的腳本
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.LoadData(gameData);
        }
        
        foreach (var rec in gameData.allHPs)
        {
            //Debug.Log($"[LoadGame] 角色 {rec.id} 載入 HP = {rec.hp}");
        }

    }
    public void SaveGame()
    {
        if (this.gameData == null)
        {
            Debug.LogWarning("No data found, a new game needs to be started.");
            return;
        }
        
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
        {
            //Debug.Log("Saving data from: " + dataPersistenceObj.GetType().Name);
            dataPersistenceObj.SaveData(ref gameData);
        }
        foreach (var rec in gameData.allHPs)
        {
            //Debug.Log($"[SaveGame] 角色 {rec.id} 儲存 HP = {rec.hp}");
        }
        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        //Debug.Log("[OnApplicationQuit] 嘗試保存遊戲資料");
        SaveGame();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return FindObjectsOfType<MonoBehaviour>()
            .OfType<IDataPersistence>()
            .ToList();
    }

    public bool HasGameData()
    {
        return gameData != null;
    }
}