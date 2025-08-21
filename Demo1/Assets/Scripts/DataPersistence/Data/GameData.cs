using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class HPRecord   { public string id; public float hp; }

[System.Serializable] public class SceneHPGroup   // 🔸 一張圖一包
{
    public string               sceneName;
    public List<HPRecord>       hpList = new();

    public SceneHPGroup(string scene) => sceneName = scene;
}

[System.Serializable]
public class GameData
{
    public List<SceneHPGroup> sceneHPGroups = new();
    public Vector3  playerPosition = Vector3.zero;
    public string   sceneName      = "MainMenu";

    public int speed = 5, attackDamage = 20, defence = 15;
    public int attackSeg = 0, defenceSeg = 0, speedSeg = 0;

    /* ────── 讀 / 寫血量 ────── */
    public float GetHP(string id, float def, string scene = null)
    {
        scene ??= sceneName;
        var g = sceneHPGroups.Find(x => x.sceneName == scene);
        var r = g?.hpList.Find(x => x.id == id);
        return r == null ? def : r.hp;
    }
    public void SetHP(string id, float hp, string scene = null)
    {
        scene ??= sceneName;
        var g = sceneHPGroups.Find(x => x.sceneName == scene) ?? new SceneHPGroup(scene);
        if (!sceneHPGroups.Contains(g)) sceneHPGroups.Add(g);

        int idx = g.hpList.FindIndex(x => x.id == id);
        var rec = new HPRecord { id = id, hp = hp };
        if (idx >= 0) g.hpList[idx] = rec; else g.hpList.Add(rec);
    }
}
