using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
            var go  = Instantiate(cardPrefab, cardParent);
            var icon= go.transform.Find("Icon")?.GetComponent<Image>();
            var t1  = go.transform.Find("Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            var t2  = go.transform.Find("Desc") ?.GetComponent<TMPro.TextMeshProUGUI>();
            var btn = go.GetComponentInChildren<Button>();

            if (icon) icon.sprite = buff.icon;
            if (t1)   t1.text     = buff.title;
            if (t2)   t2.text     = buff.description;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[UpgradeMenu] Player clicked buff: {buff.title}");
                buff.Apply(ArenaPlayerController.Instance ? ArenaPlayerController.Instance.gameObject : null);
                HealPlayer();
                CloseAndResume();
            });
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
