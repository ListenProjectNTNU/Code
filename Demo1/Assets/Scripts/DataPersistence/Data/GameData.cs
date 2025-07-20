using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HPRecord
{
    public string id;
    public float  hp;
}

[System.Serializable]
public class GameData
{
    // ❶ 一開始就建立 List，避免反序列化空物件時變 null
    public List<HPRecord> allHPs = new List<HPRecord>();
    public Vector3 playerPosition;
    
    public string sceneName = "SecondScene";
    
    public int speed = 5;
    public int attackDamage = 20;
    public int defence = 15;

    public int attackSeg = 0;
    public int defenceSeg = 0;
    public int speedSeg = 0;
    public GameData()
    {
        playerPosition = Vector3.zero;
    }
    
    // 取得血量；若找不到就回傳預設值
    public float GetHP(string id, float defaultHP)
    {
        // allHPs 可能在讀檔失敗時為 null，先保護一下
        if (allHPs == null) return defaultHP;

        var rec = allHPs.Find(r => r.id == id);
        return rec == null ? defaultHP : rec.hp;
    }

    // 設定血量；若沒找到此角色就新增
    public void SetHP(string id, float hp)
    {
        if (allHPs == null) allHPs = new List<HPRecord>();

        int idx = allHPs.FindIndex(r => r.id == id);
        var newRec = new HPRecord { id = id, hp = hp };

        if (idx >= 0) allHPs[idx] = newRec;
        else          allHPs.Add(newRec);
    }
}
