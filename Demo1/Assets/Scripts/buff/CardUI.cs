using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffCardUI : MonoBehaviour
{
    [Header("Auto-wired if left empty")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button   selectButton;

    private BuffSO data;

    void Awake()
    {
        // 自動抓取 Title / Desc 文字（避免忘記拖）
        if (!titleText)
        {
            var t = transform.Find("Title");
            if (t) titleText = t.GetComponent<TMP_Text>();
        }
        if (!descText)
        {
            var t = transform.Find("Desc");
            if (t) descText = t.GetComponent<TMP_Text>();
        }
        if (!selectButton)
            selectButton = GetComponent<Button>();

        if (selectButton)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClick);
        }
    }

    public void Setup(BuffSO so)
    {
        data = so;

        if (titleText)
            titleText.text = string.IsNullOrEmpty(so.title) ? "(No Title)" : so.title;

        if (descText)
            descText.text = so.description ?? "";
        
        // 診斷用：你可以在 Console 看到到底有沒有成功設到
        Debug.Log($"[BuffCardUI] Set title: '{(so.title ?? "null")}', desc: '{(so.description ?? "null")}'", this);
    }

    private void OnClick()
    {
        if (!data) return;
        if (ArenaPlayerController.Instance)
            data.Apply(ArenaPlayerController.Instance.gameObject);

        // 關閉選單（依你的結構調整）
        var menu = GetComponentInParent<UpgradeMenu>();
        if (menu) menu.gameObject.SetActive(false);

        Destroy(gameObject);
    }
}
