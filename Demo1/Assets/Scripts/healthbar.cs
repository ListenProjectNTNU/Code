using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthbar : MonoBehaviour
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
        currenthp = -1f; // 特殊值表示尚未載入資料
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
        isDataLoaded = true; // ✅ 告訴 Start() 不要再重設 currenthp
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

}