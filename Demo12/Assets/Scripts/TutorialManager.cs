using UnityEngine;
using System;

public class TutorialManager : MonoBehaviour
{
    public static event Action OnMoveTutorialComplete; // 移動教學完成事件
    public static event Action OnAttackTutorialComplete; // 攻擊教學完成事件

    void Start()
    {
        // 監聽移動教學完成事件
        OnMoveTutorialComplete += StartAttackTutorial;
    }

    void OnDestroy()
    {
        // 取消訂閱，防止內存洩漏
        OnMoveTutorialComplete -= StartAttackTutorial;
    }

    public void CompleteMoveTutorial()
    {
        Debug.Log("移動教學完成！");
        OnMoveTutorialComplete?.Invoke(); // 觸發事件
    }

    void StartAttackTutorial()
    {
        Debug.Log("開始攻擊教學！");
        // 這裡可以啟動攻擊教學的 UI、指導流程等
    }
}
