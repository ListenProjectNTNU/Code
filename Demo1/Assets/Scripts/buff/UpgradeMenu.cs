using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

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

        if (!arena)
            arena = FindObjectOfType<ArenaManager>(true);

        if (!statusPanel)
            statusPanel = FindObjectOfType<ArenaStatusPanel>(true);

        gameObject.SetActive(false);
    }

    public void ShowThreeRandom()
    {
        if (!cardParent || !cardPrefab)
        {
            Debug.LogWarning("[UpgradeMenu] cardParent 或 cardPrefab 未設定");
            return;
        }

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
            if (!buff) continue;

            Debug.Log($"[UpgradeMenu] ===== Spawn card for buff: {buff.name}, icon = {buff.icon}");

            var go = Instantiate(cardPrefab, cardParent);

            // 1) 找 Icon（Image）—— 用名稱模糊比對，抓子物件裡叫 Icon 的那個
            Image icon = null;
            var images = go.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                Debug.Log($"[UpgradeMenu] Image found on card: {img.name}", img);
                var n = img.name.Trim();
                if (n == "Icon" || n.StartsWith("Icon"))
                {
                    icon = img;
                    Debug.Log("[UpgradeMenu] >>> Icon Image chosen: " + img.name, img);
                    break;
                }
            }

            // 2) 找 Title / Desc
            TMP_Text t1 = null; // Title
            TMP_Text t2 = null; // Desc

            var tmps = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                Debug.Log($"[Card TMP] found: '{t.name}'", t);

                var n = t.name.Trim();

                if (t1 == null && (n == "Title" || n.StartsWith("Title")))
                    t1 = t;
                else if (t2 == null && (n == "Desc" || n.StartsWith("Desc")))
                    t2 = t;
            }

            // 3) 填 Icon
            if (icon)
            {
                Debug.Log($"[UpgradeMenu] Icon BEFORE set: sprite = {icon.sprite}", icon);
                icon.sprite = buff.icon;
                icon.preserveAspect = true;
                icon.enabled = (buff.icon != null);
                Debug.Log($"[UpgradeMenu] Icon AFTER set: sprite = {icon.sprite}", icon);
            }
            else
            {
                Debug.LogWarning("[UpgradeMenu] Icon Image not found on card prefab.", go);
            }

            // 4) 填文字
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

            // 5) 綁定點擊事件
            var btn = go.GetComponentInChildren<Button>(true);
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[UpgradeMenu] Player clicked buff: {buff.title}", buff);

                    buff.Apply(ArenaPlayerController.Instance
                               ? ArenaPlayerController.Instance.gameObject
                               : null);

                    HealPlayer();

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

        player.currentHealth = Mathf.Min(player.maxHealth,
                                         player.currentHealth + healOnChoose);

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
