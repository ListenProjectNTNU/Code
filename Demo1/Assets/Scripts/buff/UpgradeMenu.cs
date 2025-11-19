using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
public class UpgradeMenu : MonoBehaviour
{
    [Header("Refs")]
    public ArenaManager arena;
    public GameObject cardPrefab;
    public Transform cardParent;
    public ArenaStatusPanel statusPanel;
    [Header("Pool")]
    public List<BuffSO> allBuffs = new();

    [Header("Reward")]
    [Tooltip("玩家每次選擇增益時回復的血量")]
    public float healOnChoose = 30f;
    private GameObject _player;

    void Awake()
    {
        _player = ArenaPlayerController.Instance ?
                  ArenaPlayerController.Instance.gameObject :
                  GameObject.FindGameObjectWithTag("Player");
        if (!arena) arena = FindObjectOfType<ArenaManager>(true);

        // 🔥 如果你懶得手動拖，可以加這行自動找
        if (!statusPanel)
            statusPanel = FindObjectOfType<ArenaStatusPanel>(true);

        gameObject.SetActive(false);
    }

    public void ShowThreeRandom()
    {
        gameObject.SetActive(true);

        // 清空舊卡
        foreach (Transform c in cardParent)
            Destroy(c.gameObject);

        // 取 3 張不重複
        var picks = new List<BuffSO>();
        var pool  = new List<BuffSO>(allBuffs);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            picks.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // 產卡
        foreach (var buff in picks)
        {
            var go = Instantiate(cardPrefab, cardParent);

            // 先抓 Icon、Button
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            var btn  = go.GetComponentInChildren<Button>(true);

            // 用名稱搜尋 Title / Desc（容錯：Trim + StartsWith）
            TMP_Text t1 = null; // Title
            TMP_Text t2 = null; // Desc

            var tmps = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                // Debug 看看實際名字
                Debug.Log($"[Card TMP] found: '{t.name}'", t);

                var n = t.name.Trim(); // 把前後空白剪掉

                if (t1 == null && (n == "Title" || n.StartsWith("Title")))
                    t1 = t;
                else if (t2 == null && (n == "Desc" || n.StartsWith("Desc")))
                    t2 = t;
            }

            // 寫入顯示
            if (icon) icon.sprite = buff.icon;

            if (t1)
            {
                t1.text = string.IsNullOrEmpty(buff.title) ? "(No Title)" : buff.title;
                t1.ForceMeshUpdate();
                Debug.Log($"[UpgradeMenu] Title set to: {t1.text}", t1);
            }
            else
            {
                Debug.LogWarning("[UpgradeMenu] Title TMP not found on card prefab.", go);
            }

            if (t2)
            {
                t2.text = buff.description ?? "";
            }
            else
            {
                Debug.LogWarning("[UpgradeMenu] Desc TMP not found on card prefab.", go);
            }

            // 綁定點擊
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[UpgradeMenu] Player clicked buff: {buff.title}");

                    buff.Apply(ArenaPlayerController.Instance ? ArenaPlayerController.Instance.gameObject : null);
                    HealPlayer();

                    // 🔥🔥🔥 讓狀態面板更新（重點補在這裡）
                    if (statusPanel != null)
                    {
                        statusPanel.RefreshAll();
                        Debug.Log("[UpgradeMenu] StatusPanel refreshed.");
                    }
                    else
                    {
                        Debug.LogWarning("[UpgradeMenu] statusPanel is NULL, cannot refresh.");
                    }

                    CloseAndResume();
                });
            }
            else
            {
                Debug.LogWarning("[UpgradeMenu] Button not found on card prefab.", go);
            }
        }
    }


    private void HealPlayer()
    {
        var player = ArenaPlayerController.Instance;
        if (player == null || player.isDead) return;

        player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + healOnChoose);

        if (player.healthBar != null)
            player.healthBar.SetHealth(player.currentHealth);

        Debug.Log($"[UpgradeMenu] Heal +{healOnChoose}. HP = {player.currentHealth}/{player.maxHealth}");
    }

    private void CloseAndResume()
    {
        gameObject.SetActive(false);
        if (arena) arena.ResumeGame();
    }
}
