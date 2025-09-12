using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用血條元件：
/// - 以 <see cref="id"/> 作為在 GameData 中儲存 / 讀取的索引鍵。  
/// - 自動撐開「血量降低緩降效果」(hpEffectImg)。  
/// - 實作 <see cref="IDataPersistence"/>，可被 DataPersistenceManager 自動存讀。  
/// </summary>
public class HealthBar : MonoBehaviour, IDataPersistence
{
    [Header("Data ID (必填)\nplayer / enemy / boss …")]
    public string id = "enemy";               // 每個血條唯一

    [Header("UI References")]
    public Image hpImg;                       // 正式血條
    public Image hpEffectImg;                 // 漸變效果條

    [Header("Config")]
    public float maxHP    = 100f;
    public float bufftime = 0.5f;             // 緩降時間 (秒)

    [Header("Runtime")]
    public float currenthp;
    

    Coroutine effectCo;

    /* ─────────────────── Unity LifeCycle ─────────────────── */
    void Awake()  => currenthp = -1f;         // -1 代表尚未載入資料
    void Start()
    {
        if (currenthp < 0) currenthp = maxHP; // 若沒有載入任何存檔，滿血開局
        RefreshUI();
    }

    /* ─────────────────── Public API ─────────────────── */
    public void SetHealth(float hp)
    {
        currenthp = Mathf.Clamp(hp, 0, maxHP);
        RefreshUI();
    }

    /* ─────────────────── UI 更新 ─────────────────── */
    void RefreshUI()
    {
        hpImg.fillAmount = currenthp / maxHP;

        if (effectCo != null) StopCoroutine(effectCo);
        effectCo = StartCoroutine(EffectCoroutine());
    }

    IEnumerator EffectCoroutine()
    {
        float start = hpEffectImg.fillAmount;
        float end   = hpImg.fillAmount;
        float t = 0;

        while (t < bufftime && start > end)
        {
            t += Time.deltaTime;
            hpEffectImg.fillAmount = Mathf.Lerp(start, end, t / bufftime);
            yield return null;
        }
        hpEffectImg.fillAmount = end;
    }

    public void LoadData(GameData data)
    {
        currenthp = data.GetHP(id, maxHP);
    }

    public void SaveData(ref GameData data)
    {
        data.SetHP(id, currenthp);
    }

}
