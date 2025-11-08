using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradeMenu : MonoBehaviour
{
    [Header("Refs")]
    public ArenaManager arena;       // 拖引用或 FindObjectOfType
    public GameObject cardPrefab;    // 內含：Image(Icon) + Text(Title) + Text(Desc) + Button
    public Transform cardParent;     // 擺 3 張卡的容器

    [Header("Pool")]
    public List<BuffSO> allBuffs = new();

    private GameObject _player;

    void Awake()
    {
        _player = ArenaPlayerController.Instance ? 
                  ArenaPlayerController.Instance.gameObject : 
                  GameObject.FindGameObjectWithTag("Player");
        if (!arena) arena = FindObjectOfType<ArenaManager>(true);
        gameObject.SetActive(false);
    }

    public void ShowThreeRandom()
    {
        gameObject.SetActive(true);
        // 清空舊卡
        foreach (Transform c in cardParent) Destroy(c.gameObject);

        // 取 3 張不重複
        var picks = new List<BuffSO>();
        var pool  = new List<BuffSO>(allBuffs);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            picks.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // 生成卡片
        foreach (var buff in picks)
        {
            var go = Instantiate(cardPrefab, cardParent);
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            var t1   = go.transform.Find("Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            var t2   = go.transform.Find("Desc") ?.GetComponent<TMPro.TextMeshProUGUI>();
            var btn  = go.GetComponentInChildren<Button>();

            if (icon) icon.sprite = buff.icon;
            if (t1)   t1.text     = buff.title;
            if (t2)   t2.text     = buff.description;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                buff.Apply(_player);
                CloseAndResume();
            });
        }
    }

    private void CloseAndResume()
    {
        gameObject.SetActive(false);
        if (arena) arena.ResumeGame();
    }
}
