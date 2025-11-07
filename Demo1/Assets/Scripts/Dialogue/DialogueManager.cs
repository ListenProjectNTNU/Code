using System.Collections;
using System.Collections.Generic;
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
            Debug.LogWarning("Found more than one Dialogue Manager in the scene");
        instance = this;
    }

    public static DialogueManager GetInstance() => instance;

    private void Start()
    {
        EnsurePlayerController();

        globalVolumeController = FindObjectOfType<GlobalVolumeController>();

        dialogueIsPlaying = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            if (choice != null)
                choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }
    }

    private void Update()
    {
        if (!dialogueIsPlaying) return;

        if (Input.GetButtonDown("Submit"))
            continueStory();
    }

    // 從 JSON 進入對話
    public void EnterDialogueMode(TextAsset inkJSON)
    {
        // 可能在切場前後，先判空
        EnsurePlayerController();
        if (playerController != null) playerController.enabled = false;

        if (player != null)
        {
            var playerAnim = player.GetComponent<Animator>();
            if (playerAnim) playerAnim.Play("Move");
        }

        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);

        if (currentSceneController != null)
            sceneController = currentSceneController.GetComponent<ISceneController>();

        UpdateInkVariables();
        continueStory();
    }

    // 從指定 knot 進入對話
    public void EnterDialogueModeFromKnot(string knotName)
    {
        Debug.Log("Enter Dialogue Mode From Knot：" + knotName);
        if (inkJSON == null)
        {
            Debug.LogError("❌ Ink JSON 未指定");
            return;
        }

        EnsurePlayerController();
        if (playerController != null)
        {
            playerController.enabled = false;
            // Debug.Log($"playerController enabled = {playerController.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ playerController 找不到（可能正在切場/銷毀中），以對話為主繼續。");
        }

        if (currentStory == null)
            currentStory = new Story(inkJSON.text);

        currentStory.ChoosePathString(knotName);

        dialogueIsPlaying = true;
        if (dialoguePanel) dialoguePanel.SetActive(true);
        // Debug.Log($"dialoguePanel active = {dialoguePanel?.activeSelf}");

        if (currentSceneController != null)
        {
            sceneController = currentSceneController.GetComponent<ISceneController>();
            // Debug.Log("Get ISceneController");
        }

        UpdateInkVariables();
        continueStory();
    }

    private void UpdateInkVariables()
    {
        var inkUpdater = FindObjectOfType<InkVariableUpdater>();
        if (inkUpdater != null)
        {
            inkUpdater.SetCurrentStory(currentStory);
            inkUpdater.ApplyTempVariables();
            var inv = PlayerInventory.Instance;
            if (inv != null)
                inkUpdater.ApplyInventoryVariables(new List<string>(inv.CollectedItems));
        }
    }

    // 🔒 安全結束對話：判空＋必要時延遲一幀回補 PlayerController
    private void ExitDialogueMode()
    {
        Debug.Log("ExitDialogueMode");
        dialogueIsPlaying = false;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (dialogueText) dialogueText.text = "";

        // 可能此刻 Player 已被銷毀（切場中），先嘗試回補；回補不到就略過啟用
        EnsurePlayerController();
        if (playerController != null)
            playerController.enabled = true;
        else
            Debug.LogWarning("⚠️ ExitDialogueMode 時找不到 PlayerController，略過啟用玩家。");

        if (player != null)
        {
            var playerAnim = player.GetComponent<Animator>();
            if (playerAnim) playerAnim.Play("Move");
        }
    }

    private void continueStory()
    {
        // 先顯示選項（如果有）
        if (currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
            return;
        }

        if (currentStory.canContinue)
        {
            if (dialogueText != null)
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
            string[] splitTag = tag.Split(':');
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
                    if (displayNameText) displayNameText.text = tagValue;
                    break;

                case PORTRAIT_TAG:
                    if (portraitAnimator != null &&
                        portraitAnimator.HasState(0, Animator.StringToHash(tagValue)))
                        portraitAnimator.Play(tagValue);
                    break;

                case LAYOUT_TAG:
                    // 若你有版面切換，這裡補上
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
            Debug.LogError($"More Choices than UI supports: {currentChoices.Count}");

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            if (index < choices.Length && choices[index] != null)
            {
                choices[index].SetActive(true);
                if (choicesText[index] != null)
                    choicesText[index].text = choice.text;
            }
            index++;
        }

        for (int i = index; i < choices.Length; i++)
        {
            if (choices[i] != null) choices[i].SetActive(false);
        }
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (choices != null && choices.Length > 0 && choices[0] != null)
            EventSystem.current.SetSelectedGameObject(choices[0]);
    }

    public void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        continueStory();
        if (globalVolumeController != null)
            globalVolumeController.SetBlur();
    }

    // —— 小工具：確保 playerController 可用（被銷毀就重新抓）——
    private void EnsurePlayerController()
    {
        // 已經有而且沒被銷毀，直接用
        if (playerController != null) return;

        // 優先找有 DontDestroyOnLoad 標記的 Player
        var allPlayers = FindObjectsOfType<PlayerController>(true);
        foreach (var p in allPlayers)
        {
            if (p.gameObject.scene.name == "DontDestroyOnLoad")
            {
                playerController = p;
                player = p.gameObject;
                return;
            }
        }

        // 如果找不到 DDOL，就退而求其次（目前場景裡的 Player）
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.gameObject;
        }    
    }
}
