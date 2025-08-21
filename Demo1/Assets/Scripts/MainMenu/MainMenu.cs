using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("SaveSlotMenu")]
    [SerializeField] private SaveSlotMenu saveSlotMenu;
    [Header("Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button loadGameButton;
    
    private void Start()
    {
        if (!DataPersistenceManager.instance.HasGameData)
        {
            continueGameButton.interactable = false;
        }
    }
    public void OnNewGameClicked()
    {
        saveSlotMenu.ActivateMenu(SaveSlotMenu.Mode.New);
        this.DeactivateMenu();
    }

    public void OnLoadGameClicked()
    {
        saveSlotMenu.ActivateMenu(SaveSlotMenu.Mode.Load);
        DeactivateMenu();
    }

    public void OnContinueGameClicked()
    {
        DisableMenuButtons();
        DataPersistenceManager.instance.LoadGame();   // 會自動跳存檔場景
    }

    private void DisableMenuButtons()
    {
        newGameButton.interactable = false;
        continueGameButton.interactable = false;
        continueGameButton.interactable = false;
    }

    public void ActivateMenu()
    {
        this.gameObject.SetActive(true);
    }

    public void DeactivateMenu()
    {
        this.gameObject.SetActive(false);
    }
}
