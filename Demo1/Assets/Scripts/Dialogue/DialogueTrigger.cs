using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    public GameObject visualCue;
    [Header("Ink JSON")]
    public TextAsset inkJSON;
    private bool playerInRange;



    private void Awake() 
    {
        //初始化
        playerInRange = false;
        visualCue.SetActive(false);

    }

    private void Update()
    {
        Debug.Log($"Update: playerInRange={playerInRange}, dialogueIsPlaying={DialogueManager.GetInstance().dialogueIsPlaying}, E={Input.GetKeyDown(KeyCode.E)}");

        if(playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            visualCue.SetActive(true);
            if(Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("✅ E 被偵測到，進入對話模式");
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
            }
        }
        else if(!playerInRange)
        {
            visualCue.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) 
    {
        Debug.Log("Player 進入對話範圍");
        if(other.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other) 
    {
        if(other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
