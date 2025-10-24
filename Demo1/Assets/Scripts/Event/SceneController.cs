using UnityEngine;

public class SceneController : MonoBehaviour
{
    public LoopingBackground loopingBG;
    public GameObject senpai; // 學姊物件
    public GameObject player; // 主角物件

    public void HandleTag(string tagValue)
    {
        switch (tagValue)
        {
            case "corridor_withDoor":
                loopingBG.SwitchToNextBGOpen();
                break;

            case "fox_appear":
                senpai.SetActive(true);
                Debug.Log("學姊出現！");
                FlipPlayer(true); // 主角面向左
                break;

            case "player_turnBack":
                FlipPlayer(false); // 主角轉回右邊
                break;
        }
    }
    void FlipPlayer(bool faceLeft) //之後看要不要整理playerController裡
    {
        Vector3 scale = player.transform.localScale;
        if (faceLeft)
            scale.x = Mathf.Abs(scale.x) * -1; // 左
        else
            scale.x = Mathf.Abs(scale.x);      // 右
        player.transform.localScale = scale;
    }
}