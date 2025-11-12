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

    [Header("Pool")]
    public List<BuffSO> allBuffs = new();

    [Header("Reward")]
    [Tooltip("玩家每次選擇增益時回復的血量")]
    public float healOnChoose = 30f;

    void Awake()
    {
        if (!arena) arena = FindObjectOfType<ArenaManager>(true);
        gameObject.SetActive(false);
    }


    public void ShowThreeRandom()
    {
        gameObject.SetActive(true);

        foreach (Transform c in cardParent) Destroy(c.gameObject);

        var picks = new List<BuffSO>();
        var pool  = new List<BuffSO>(allBuffs);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            picks.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        foreach (var buff in picks)
        {
            var go   = Instantiate(cardPrefab, cardParent);

            // 1) 先嘗試用你原本的路徑抓
            var icon = go.transform.Find("Icon") ?.GetComponent<Image>();
            var t1   = go.transform.Find("Title")?.GetComponent<TMP_Text>();
            var t2   = go.transform.Find("Desc") ?.GetComponent<TMP_Text>();
            var btn  = go.GetComponentInChildren<Button>(true);

            // 2) 如果 t1 或 t2 沒抓到，用更保險的全域搜尋（包含隱藏物件）
            if (!t1 || !t2)
            {
                var tmps = go.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in tmps)
                    Debug.Log($"[Card TMP] found: {t.name}", t);

                if (!t1)
                    t1 = tmps.FirstOrDefault(x => x.name.Equals("Title"));
                if (!t2)
                    t2 = tmps.FirstOrDefault(x => x.name.Equals("Desc"));
            }

            // 3) 寫入顯示
            if (icon) icon.sprite = buff.icon;

            if (t1)
            {
                t1.text = string.IsNullOrEmpty(buff.title) ? "(No Title)" : buff.title;
                t1.ForceMeshUpdate(); // 保險：強制刷新
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

            // 4) 綁定點擊
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[UpgradeMenu] Player clicked buff: {buff.title}");
                    buff.Apply(ArenaPlayerController.Instance ? ArenaPlayerController.Instance.gameObject : null);
                    HealPlayer();
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
