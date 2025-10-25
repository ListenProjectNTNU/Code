using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Scene2Controller : MonoBehaviour
{
    public Transform monster;
    public Transform player;
    public float moveDistance = 2f;
    public float moveSpeed = 2f;
    private bool isRunningEvent = false;

    private void Start()
    {
        monster.GetComponent<enemy_cow>().isActive = false;// 確保進場時不會亂動
        monster.GetComponent<Animator>().Play("idle");

        player.GetComponent<PlayerController>().canControl = false;
        player.localScale = new Vector3(-1f, 1f, 1f); // 面向左邊

    }

    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "monster_approach":
                if (!isRunningEvent)
                    StartCoroutine(MonsterApproachEvent());
                break;
        }
    }

    private IEnumerator MonsterApproachEvent()
    {
        isRunningEvent = true;

        Animator monsterAnim = monster.GetComponent<Animator>();
        monsterAnim.Play("run"); // 播放移動動畫

        Vector3 monsterTarget = monster.position + monster.forward * moveDistance;
        Vector3 playerTarget = player.position - player.forward * moveDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            monster.position = Vector3.Lerp(monster.position, monsterTarget, t);
            player.position = Vector3.Lerp(player.position, playerTarget, t);
            yield return null;
        }

        monsterAnim.Play("idle"); // 回到 Idle

        // 恢復 AI
        //monster.GetComponent<enemy_cow>().isActive = true;

        //isRunningEvent = false;
    }

}
