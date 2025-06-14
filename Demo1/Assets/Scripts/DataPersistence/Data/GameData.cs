//遊戲最一開始的狀態數值
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData 

{
    public List<string> collectedItems = new List<string>();
    public float currenthp;
    //初始化
    public GameData()
    {
        // collectedItems.Clear(); // 🔥 把 List 清空
        this.currenthp = 100;
    }
}
