using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArenaStatusPanel : MonoBehaviour
{
    [Header("Player Refs")]
    public ArenaPlayerController player;   // 若沒拖，會自動用 .Instance 找
    private PlayerBuffs buffs;

    [Header("Stats UI")]
    public TMP_Text attackText;
    public TMP_Text defenceText;
    public TMP_Text speedText;

    [Header("Buff List UI")]
    public Transform buffListParent;      // 用來擺 Buff 小卡的容器 (Vertical Layout Group)
    public GameObject buffEntryPrefab;    // 預製體：裡面有 Icon + Title

    private void Awake()
    {
        if (!player)
            player = ArenaPlayerController.Instance;

        if (player)
            buffs = player.GetComponent<PlayerBuffs>();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    /// <summary>外面（UpgradeMenu）也可以呼叫這個，強制刷新</summary>
    public void RefreshAll()
    {
        if (!player) return;

        RefreshStats();
        RefreshBuffList();
    }

    private void RefreshStats()
    {
        

        int curAtk = buffs.CurAttack(player.attackDamage);
        int curDef = buffs.CurDefence(player.defence);
        int curSpd = buffs.CurSpeed(player.speed);

        if (attackText)  attackText.text  = $"Attack : {curAtk}";
        if (defenceText) defenceText.text = $"Denfence : {curDef}";
        if (speedText)   speedText.text   = $"Speed : {curSpd}";
    }

    private void RefreshBuffList()
    {
        if (!buffListParent || buffs == null)
        {
            Debug.LogWarning("[StatusPanel] buffListParent or buffs is null, skip RefreshBuffList.");
            return;
        }

        // 清空舊的
        foreach (Transform c in buffListParent)
            Destroy(c.gameObject);

        // 🔥 防呆：Prefab 沒設定或被摧毀
        if (!buffEntryPrefab)
        {
            Debug.LogWarning("[StatusPanel] buffEntryPrefab is null or destroyed, cannot spawn buff entries.");
            return;
        }

        foreach (var b in buffs.acquiredBuffs)
        {
            if (!b) continue;

            var go = Instantiate(buffEntryPrefab, buffListParent);
            var icon  = go.transform.Find("Icon") ?.GetComponent<Image>();
            var title = go.transform.Find("Title")?.GetComponent<TMP_Text>();

            if (icon)  icon.sprite = b.icon;
            if (title) title.text  = b.title;
        }
    }
}
