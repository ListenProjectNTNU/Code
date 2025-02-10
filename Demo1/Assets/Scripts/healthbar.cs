using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class healthbar : MonoBehaviour
{
    public Image hpImg;
    public Image hpeffectgImg;

    public float maxHP = 100f;
    public float currenthp;
    public float bufftime = 0.5f;

    private Coroutine updateCoroutine;
    // Start is called before the first frame update
    private void Start()
    {
        currenthp = maxHP;
        updatehealthbar();
    }

    public void SetHealth(float health)
    {
        currenthp = Mathf.Clamp(health, 0f, maxHP); // 確保血量不小於 0
        // Debug.Log($"SetHealth called: {currenthp}");
        // Debug.Log($"SetHealth called: {hpImg.fillAmount}");
        updatehealthbar();
    }
    private void updatehealthbar()
    {
        hpImg.fillAmount = currenthp / maxHP;
        if(updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(updateHpeffect());
    }

    private IEnumerator updateHpeffect()
    {
        float effectLength = hpeffectgImg.fillAmount - hpImg.fillAmount;
        float elapsedTime = 0f;
        while (elapsedTime < bufftime && effectLength > 0) // 這裡可以調整為 effectLength > 0 來防止負數情況
        {
            elapsedTime += Time.deltaTime;
            hpeffectgImg.fillAmount = Mathf.Lerp(hpImg.fillAmount + effectLength, hpImg.fillAmount, elapsedTime / bufftime);
            yield return null;
        }
        hpeffectgImg.fillAmount = hpImg.fillAmount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
