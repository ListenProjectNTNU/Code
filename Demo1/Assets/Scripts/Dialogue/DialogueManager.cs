using System.Collections;
using System.Collections.Generic;
//using Ink.Parsed;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    private PlayerController playerController;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI displayNameText;
    [SerializeField] private Animator portraitAnimator;

    [Header("Choices UI")]
    public GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    public Story currentStory { get; private set; }
    public bool dialogueIsPlaying { get; private set; }
    private static DialogueManager instance;

    public UnityEvent onDialogueEnd;

    [Header("Scene Controller Reference")]
    public GameObject currentSceneController;
    private ISceneController sceneController;
    public GameObject player;

    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";

    [Header("Ink JSON")]
    public TextAsset inkJSON;

    private GlobalVolumeController globalVolumeController;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");
        }
        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();

        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update()
    {
        if (!dialogueIsPlaying)
            return;

        if (Input.GetButtonDown("Submit"))
        {
            continueStory();
        }
    }

    // ================================
    // 原本的從 JSON 進入對話模式
    // ================================
    public void EnterDialogueMode(TextAsset inkJSON)
    {
        if (playerController != null)
            playerController.enabled = false;
            


        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);

        if (currentSceneController != null)
            sceneController = currentSceneController.GetComponent<ISceneController>();

        UpdateInkVariables();
        continueStory();
    }

    // ================================
    // 新增：從指定 knot 進入對話模式
    // ================================
    public void EnterDialogueModeFromKnot(string knotName)
    {
        Debug.Log("Enter Dialogue Mode From Knot："+knotName);
        if (inkJSON == null)
        {
            Debug.LogError("❌ Ink JSON 未指定");
            return;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log($"playerController enabled = {playerController.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ playerController 未指定！");
        }


        if (currentStory == null)
        {
            currentStory = new Story(inkJSON.text);
        }

        currentStory.ChoosePathString(knotName);

        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        Debug.Log($"dialoguePanel active = {dialoguePanel.activeSelf}");

        if (currentSceneController != null)
            sceneController = currentSceneController.GetComponent<ISceneController>();
            Debug.Log("Get ISceneController");

        UpdateInkVariables();
        continueStory();
    }

    private void UpdateInkVariables()
    {
        InkVariableUpdater inkUpdater = FindObjectOfType<InkVariableUpdater>();
        if (inkUpdater != null)
        {
            inkUpdater.SetCurrentStory(currentStory);
            inkUpdater.ApplyTempVariables();
            inkUpdater.ApplyInventoryVariables(new List<string>(PlayerInventory.Instance.CollectedItems));
        }
    }

    private void ExitDialogueMode()
    {
        Debug.Log("ExitDialogueMode");
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
        playerController.enabled = true;
        Animator playerAnim = player.GetComponent<Animator>();
        playerAnim.Play("Move");
        
    }

    private void continueStory()
    {
        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
            return;
        }

        if (currentStory.canContinue)
        {
            dialogueText.text = currentStory.Continue();
            HandleTags(currentStory.currentTags);
            DisplayChoices();
        }
        else
        {
            ExitDialogueMode();
            onDialogueEnd?.Invoke();
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(":");
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
                continue;
            }

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    if (portraitAnimator != null && portraitAnimator.HasState(0, Animator.StringToHash(tagValue)))
                        portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    break;
                case "scene":
                    if (sceneController != null)
                        sceneController.HandleTag(tagValue);
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More Choices were given than the UI can support. Number of choices given"
                + currentChoices.Count);
        }

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        continueStory();
        globalVolumeController.SetBlur();
    }
}
