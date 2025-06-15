using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthbar : MonoBehaviour, IDataPersistence
{
    public Image hpImg;
    public Image hpEffectImg;

    public float maxHP = 100f;
    public float currenthp;
    public float bufftime = 0.5f;

    private Coroutine updateCoroutine;
    private bool isDataLoaded = false;
    private void Awake()
    {
        currenthp = -1f; // 用特殊值判斷是否還沒被載入
        Debug.Log("[Awake] 初始化 currenthp = -1");
    }

    private void Start()
    {
        if (!isDataLoaded)
        {
            currenthp = maxHP;
        }
        updatehealthbar();
    }

    public void SetHealth(float health)
    {
        currenthp = Mathf.Clamp(health, 0f, maxHP);
        Debug.Log("[SetHealth] 設定 currenthp = " + currenthp);

        updatehealthbar();
    }

    private void updatehealthbar()
    {
        hpImg.fillAmount = currenthp / maxHP;
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(updateHpeffect());
    }

    private IEnumerator updateHpeffect()
    {
        float effectLength = hpEffectImg.fillAmount - hpImg.fillAmount;
        float elapsedTime = 0f;


        while (elapsedTime < bufftime && effectLength > 0)
        {
            elapsedTime += Time.deltaTime;
            hpEffectImg.fillAmount = Mathf.Lerp(hpImg.fillAmount + effectLength, hpImg.fillAmount, elapsedTime / bufftime);
            yield return null;
        }

        hpEffectImg.fillAmount = hpImg.fillAmount;
    }

    public void LoadData(GameData data)
    {
        currenthp = data.currenthp;
        isDataLoaded = true;
        updatehealthbar();
    }

    public void SaveData(ref GameData data)
    {
        Debug.Log($"[SaveGame] 呼叫前 currenthp = {this.currenthp}");
        data.currenthp = this.currenthp;
        Debug.Log("[SaveGame] 呼叫後");
    }
}
