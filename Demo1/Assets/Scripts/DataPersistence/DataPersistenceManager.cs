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
        //TODO Load any saved data from a flie using the data handler 
    }
    public void SaveGame()
    {

    }
}
