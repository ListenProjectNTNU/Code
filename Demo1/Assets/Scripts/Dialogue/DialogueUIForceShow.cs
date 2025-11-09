// Assets/Scripts/Debug/DialogueUIForceShow.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;

public class DialogueUIForceShow : MonoBehaviour
{
    [Header("Hook (optional)")]
    public GameObject dialoguePanel;           // assign in inspector (optional)
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI displayNameText;

    [Header("Fallback font (optional)")]
    public string resourcesTmpFontPath = "Fonts/LXGW_WenKai_Mono_TC/LXGW_WenKai_Mono_TC-Regular SDF";

    IEnumerator Start()
    {
        // 等下一幀，給其他 Start/Awake 跑掉
        yield return null;
        yield return new WaitForEndOfFrame();

        // 自動綁定若 inspector 未設
        if (dialoguePanel == null)
        {
            var dp = GameObject.Find("DialoguePanel");
            if (dp) dialoguePanel = dp;
        }
        if (dialogueText == null)
        {
            var dt = GameObject.FindObjectOfType<TextMeshProUGUI>();
            if (dt) dialogueText = dt;
        }

        Debug.Log($"[Debug] DialoguePanel = {(dialoguePanel? dialoguePanel.name : "NULL")}, scene={gameObject.scene.name}");
        Debug.Log($"[Debug] dialogueText = {(dialogueText ? dialogueText.name : "NULL")}");

        ForceMakeVisible();
    }

    public void ForceMakeVisible()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("[Debug] dialoguePanel is null — cannot force show.");
            return;
        }

        // 1) 確保 Canvas: override sorting，render mode 為 Overlay（build 常見 camera mismatch）
        var canvas = dialoguePanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[Debug] Canvas found: {canvas.name}, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999;
        }
        else
        {
            Debug.LogWarning("[Debug] No Canvas found as parent of DialoguePanel.");
        }

        // 2) 把 panel 提到最上層
        dialoguePanel.transform.SetParent(dialoguePanel.transform.root, true);
        dialoguePanel.transform.SetAsLastSibling();

        // 3) 如果沒有 CanvasGroup，加入並確保alpha/interactable
        var cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 4) 強制顯示並恢復 RectTransform layout
        dialoguePanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        var rt = dialoguePanel.GetComponent<RectTransform>();
        if (rt != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // 5) TMP fallback font：若 dialogueText 未設定 font，從 Resources 嘗試載入
        if (dialogueText != null)
        {
            if (dialogueText.font == null)
            {
                var f = Resources.Load<TMP_FontAsset>(resourcesTmpFontPath);
                if (f != null)
                {
                    dialogueText.font = f;
                    Debug.Log("[Debug] Fallback TMP font applied from Resources.");
                }
                else
                {
                    Debug.LogWarning("[Debug] fallback TMP font not found at Resources/" + resourcesTmpFontPath);
                }
            }

            // 顯示測試字串
            dialogueText.text = "DEBUG: Dialogue panel forced visible — if you see this, UI rendering works in build.";
        }
        else
        {
            Debug.LogWarning("[Debug] dialogueText is null.");
        }
    }
}
