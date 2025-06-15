using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthbar : MonoBehaviour, IDataPersistence
{
    public string characterID = "player";  // 角色唯一 ID，場景中每個 healthbar 都要設定

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
        Debug.Log($"[Awake] {characterID} 初始化 currenthp = -1");
    }

    private void Start()
    {
        if (!isDataLoaded)
        {
            currenthp = maxHP;
            Debug.Log($"[Start] {characterID} 沒有載入資料，設 currenthp = maxHP = {currenthp}");
        }
        updatehealthbar();
    }

    public void SetHealth(float health)
    {
        currenthp = Mathf.Clamp(health, 0f, maxHP);
        Debug.Log($"[SetHealth] {characterID} 設定 currenthp = {currenthp}");

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

    public void LoadData(GameData data) {
        currenthp = data.GetHP(characterID, maxHP);
        isDataLoaded = true;
        updatehealthbar();
    }
    public void SaveData(ref GameData data) {
        data.SetHP(characterID, currenthp);
    }
}