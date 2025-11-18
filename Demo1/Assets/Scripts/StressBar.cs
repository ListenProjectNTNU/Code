using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 壓力值條：從 0 往上加到 maxStress。
/// - 與 HealthBar 幾乎同結構，方便沿用。
/// - 以 <see cref="id"/> 作為存讀鍵，為求相容直接沿用 GameData.GetHP/SetHP。
/// </summary>
public class StressBar : MonoBehaviour, IDataPersistence
{
    [Header("Data ID (必填)\n建議用 stress 或 player_stress")]
    public string id = "stress";

    [Header("UI References")]
    public Image stressImg;            // 主要條
    public Image stressEffectImg;      // 追趕用效果條（延遲追上）

    [Header("Config")]
    public float maxStress = 100f;
    public float effectTime = 0.5f;    // 追趕耗時（秒）

    [Header("Runtime")]
    public float currentStress = 0f;   // 從 0 起

    Coroutine effectCo;

    void Start()
    {
        ApplySceneDefaults();
        // 沒有存檔就用 0 起
        currentStress = Mathf.Clamp(currentStress, 0, maxStress);
        RefreshUI(immediate:true);
    }
    // 寫死的壓力值
    void ApplySceneDefaults()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        switch (sceneName)
        {
            case "SecondScene":
                currentStress = 100f;
                break;
            case "ThirdScene":
                currentStress = 70f;
                break;
            case "FourthScene":
                currentStress = 30f;
                break;
        }
    }

    /* ─────────────────── Public API ─────────────────── */
    public void SetStress(float v)
    {
        currentStress = Mathf.Clamp(v, 0, maxStress);
        RefreshUI();
    }

    public void AddStress(float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;
        currentStress = Mathf.Clamp(currentStress + delta, 0, maxStress);
        RefreshUI();
    }

    public void ClearStress() => SetStress(0);

    /* ─────────────────── UI 更新 ─────────────────── */
    void RefreshUI(bool immediate = false)
    {
        float target = maxStress > 0 ? currentStress / maxStress : 0f;
        stressImg.fillAmount = target;

        if (stressEffectImg == null) return;

        if (effectCo != null) StopCoroutine(effectCo);
        if (immediate)
        {
            stressEffectImg.fillAmount = target;
        }
        else
        {
            effectCo = StartCoroutine(EffectCoroutine(target));
        }
    }

    IEnumerator EffectCoroutine(float target)
    {
        float start = stressEffectImg.fillAmount;
        if (Mathf.Approximately(start, target)) yield break;

        float t = 0f;
        while (t < effectTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / effectTime);
            stressEffectImg.fillAmount = Mathf.Lerp(start, target, u);
            yield return null;
        }
        stressEffectImg.fillAmount = target;
    }

    /* ─────────────────── Persistence ───────────────────
       為了不動到你的 GameData 結構，這裡直接用既有的 GetHP/SetHP，
       只是索引鍵使用 id="stress"（或你自訂）。
    ─────────────────────────────────────────────────── */
    public void LoadData(GameData data)
    {
        currentStress = data.GetHP(id, 0f);           // 預設 0
        currentStress = Mathf.Clamp(currentStress, 0, maxStress);
    }

    public void SaveData(ref GameData data)
    {
        data.SetHP(id, currentStress);
    }
}
