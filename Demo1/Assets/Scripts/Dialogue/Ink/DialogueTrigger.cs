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
        if(playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            visualCue.SetActive(true);
            if(Input.GetKeyDown(KeyCode.E))
            {
                //InputManager.GetInstance().GetInteractPressed()
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
        if(other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other) 
    {
        if(other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
