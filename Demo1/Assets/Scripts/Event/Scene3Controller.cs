using UnityEngine;

public class Scene3Controller : MonoBehaviour, ISceneController
{
    [Header("Scene References")]
    public BossController boss;
    public Transform player;
    public CameraController cameraController; // 指定 Inspector

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip bossAwakenClip;

    [Header("Dialogue Knots")]
    public TextAsset inkJSON;
    public string knotBattleBefore = "battle_before";
    public string knotBattleAfter = "battle_after";

    private DialogueManager dialogueManager;

    private void Start()
    {
        dialogueManager = DialogueManager.GetInstance();
        if (dialogueManager == null) return;

        // 一進場就進入 battle_before
        if (inkJSON != null)
        {
            dialogueManager.inkJSON = inkJSON;
            dialogueManager.EnterDialogueModeFromKnot(knotBattleBefore);
        }

        if (boss != null)
        {
            boss.gameObject.SetActive(false); // 初始隱藏
            boss.enabled = false;             // 暫停 Update 行為
            boss.rb.velocity = Vector2.zero;
        }
    }

    public void HandleTag(string tag)
    {
        switch (tag)
        {
            case "enter_scene3":
                // 進入場景，玩家操作自動禁用
                break;

            case "appear_boss":
                boss.gameObject.SetActive(true);
                boss.enabled = true;        
                audioSource.PlayOneShot(bossAwakenClip);

                if (cameraController != null)
                {
                    // 先瞬間跳到 Boss
                    cameraController.transform.position = new Vector3(boss.transform.position.x, boss.transform.position.y, cameraController.transform.position.z);
                    cameraController.SetTarget(boss.transform); 
                    Debug.Log("Camera now follows Boss");
                }
                break;


            case "start_boss_fight":
                if (boss != null)
                {
                    boss.enabled = true;  // 啟用 Boss Update 行為 → 追擊玩家
                    cameraController.ResetTarget();
                }
                break;
        }
    }
    
    // 可以在未來 trigger 傳送門時呼叫
    public void TriggerPortalDialogue()
    {
        if (dialogueManager != null && inkJSON != null)
        {
            //dialogueManager.EnterDialogueModeFromKnot(knotBeforePortal);
        }
    }
}
