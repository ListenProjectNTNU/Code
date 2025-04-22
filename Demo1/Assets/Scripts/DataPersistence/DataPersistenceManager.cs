using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataPersistenceManager : MonoBehaviour
{
    //singleton class

    private GameData gameData;
    public static DataPersistenceManager instance { get; private set;}

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogError("Found more than one Data Persistence Manager in the Scene.");
        }
        instance = this;
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }
    public void LoadGame()
    {
        //TODO - Load any saved data from a flie using the data handler
        //如果沒有就NewGame 
        if(this.gameData == null)
        {
            Debug.Log("No data was found. A New Game needs to be started before data can be loaded.");
            NewGame();
            //return;
        }
        // ToDO - 把所以資料載入給需要的腳本
    }
    public void SaveGame()
    {

    }
}
