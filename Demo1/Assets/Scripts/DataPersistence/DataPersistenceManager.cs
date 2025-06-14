using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataPersistenceManager : MonoBehaviour
{
    //singleton class
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
            Debug.LogError("Found more than one Data Persistence Manager in the Scene.");
        }
        instance = this;
    }
    private void Start()
    {
        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }
    public void NewGame()
    {
        this.gameData = new GameData();
        Debug.Log("NewGame created with HP: " + gameData.currenthp);
    }
    public void LoadGame()
    {
        //Load any saved data from a flie using the data handler
        this.gameData = dataHandler.Load();
        //如果沒有就NewGame 
        if(this.gameData == null)
        {
            Debug.Log("No data was found. A New Game needs to be started before data can be loaded.");
            NewGame();
            //return;
        }
        // 把所以資料載入給需要的腳本
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.LoadData(gameData);
        }

        Debug.Log("Loaded Health = " + gameData.currenthp);
    }
    public void SaveGame()
    {
         foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects)
        {
            Debug.Log("Saving data from: " + dataPersistenceObj.GetType().Name);
            dataPersistenceObj.SaveData(ref gameData);
        }

        Debug.Log("Saved Health = " + gameData.currenthp);
        
        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[OnApplicationQuit] 嘗試保存遊戲資料");
        SaveGame();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        return FindObjectsOfType<MonoBehaviour>()
            .OfType<IDataPersistence>()
            .ToList();
    }
}
