using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HPRecord
{
    public string id;
    public float hp;
}

[System.Serializable]
public class SceneHPGroup
{
    public string sceneName;
    public List<HPRecord> hpList = new();

    public SceneHPGroup(string scene) => sceneName = scene;
}

[System.Serializable]
public class PlayerPositionRecord   // ← 避免與其它檔案的 ScenePlayerPosition 衝突
{
    public string  sceneName;
    public Vector3 position;

    public PlayerPositionRecord(string scene, Vector3 pos)
    {
        sceneName = scene;
        position  = pos;
    }
}

[System.Serializable]
public class GameData
{
    // HP 依場景分組
    public List<SceneHPGroup> sceneHPGroups = new();

    // 各場景座標
    public List<PlayerPositionRecord> playerPositions = new();

    // 兼容舊版欄位（仍保留）
    public Vector3 playerPosition = Vector3.zero;
    public string  sceneName      = "MainMenu";

    // 能力值
    public int speed = 5, attackDamage = 20, defence = 15;
    public int attackSeg = 0, defenceSeg = 0, speedSeg = 0;

    /* ────── 讀 / 寫玩家座標 ────── */
    public bool TryGetPlayerPosition(string scene, out Vector3 position)
    {
        var rec = playerPositions.Find(x => x.sceneName == scene);
        if (rec != null)
        {
            position = rec.position;
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    public void SetPlayerPosition(string scene, Vector3 position)
    {
        int idx = playerPositions.FindIndex(x => x.sceneName == scene);
        if (idx >= 0)
            playerPositions[idx].position = position;
        else
            playerPositions.Add(new PlayerPositionRecord(scene, position));
    }

    /* ────── HP 儲存（建議：顯式帶入 scene） ────── */
    public float GetHP(string scene, string id, float defaultHp)
    {
        var group = sceneHPGroups.Find(g => g.sceneName == scene);
        var rec   = group?.hpList.Find(r => r.id == id);
        return rec != null ? rec.hp : defaultHp;
    }

    public void SetHP(string scene, string id, float hp)
    {
        var group = sceneHPGroups.Find(g => g.sceneName == scene);
        if (group == null)
        {
            group = new SceneHPGroup(scene);
            sceneHPGroups.Add(group);
        }

        var rec = group.hpList.Find(r => r.id == id);
        if (rec != null) rec.hp = hp;
        else group.hpList.Add(new HPRecord { id = id, hp = hp });
    }

    /* ────── 舊版相容（以當前 this.sceneName 當索引） ────── */
    public float GetHP(string id, float defaultHp)
    {
        return GetHP(sceneName, id, defaultHp);
    }

    public void SetHP(string id, float hp)
    {
        SetHP(sceneName, id, hp);
    }
}
