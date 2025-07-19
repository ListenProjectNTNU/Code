using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SaveSlot : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private string profileId = "";

    [Header("Content")]
    [SerializeField] private GameObject noData;
    [SerializeField] private GameObject hasData;
    [SerializeField] private TextMeshProUGUI remainHealth;

    private Button saveSlotButton;

    private void Awake()
    {
        saveSlotButton = this.GetComponent<Button>();
    }
    
    public void SetData(GameData data)
    {
        if(data == null)
        {
            noData.SetActive(true);
            hasData.SetActive(false);
        }
        else
        {
            noData.SetActive(false);
            hasData.SetActive(true);

             remainHealth.text = "Temporary : " + data.speed;
        }
    }

    public string GetProfileId()
    {
        return this.profileId;
    }

    public void SetInteractable(bool interactable)
    {
        saveSlotButton.interactable = interactable;
    }
}

