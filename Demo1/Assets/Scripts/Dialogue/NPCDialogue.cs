using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class NPCDialogue : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject dialoguePanel;
    public Text dialogueText;
    public string[] dialogue;//句子們
    private int index;//第幾句

    public GameObject contButton;
    public float wordSpeed;
    public bool playerIsClose;

    public UnityEvent onDialogueEnd; // Unity Event，對話結束時觸發

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E) && playerIsClose)
        {
            //如果已開啟就關掉，否則開啟??
            if(dialoguePanel.activeInHierarchy)
            {
                resetText();
            }
            else
            {
                dialoguePanel.SetActive(true);
                //??what is StartCoroutine？
                StartCoroutine(Typing()); 
            }
        }

        if(dialogueText.text == dialogue[index])
        {
            contButton.SetActive(true);
        }
    }

    private void resetText()
    {
        dialogueText.text = "";
        index = 0;
        dialoguePanel.SetActive(false);
    }

    IEnumerator Typing()
    {
        //typing effect??
        foreach(char letter in dialogue[index].ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(wordSpeed);
        }
    }
    public void NextLine()
    {
        Debug.Log("NextLine() 被呼叫");
        contButton.SetActive(false);
        if(index < dialogue.Length - 1)
        {
            index++;
            dialogueText.text = "";
            StartCoroutine(Typing());
        }
        else
        {
            resetText();
            onDialogueEnd?.Invoke(); // 觸發對話結束事件
        }
    }


    private void OnTriggerEnter2D(Collider2D other) 
    {
        if(other.CompareTag("Player"))
        {
            playerIsClose = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other) 
    {
        if(other.CompareTag("Player"))
        {
            playerIsClose = false;
            resetText();
        }
    }
}
