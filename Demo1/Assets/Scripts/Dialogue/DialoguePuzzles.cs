using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DialoguePuzzles : MonoBehaviour
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
    void Start()
    {
        // 確保一開始面板是啟用的，並啟動對話
        dialoguePanel.SetActive(true);
        StartCoroutine(Typing());
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
        // 當整句話顯示完，啟用繼續按鈕
        contButton.SetActive(true);

    }
    public void TestButton()
    {
        Debug.Log("按鈕被點擊！");
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
            //onDialogueEnd?.Invoke(); // 觸發對話結束事件
        }
    }

}
