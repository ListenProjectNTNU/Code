using UnityEngine;

public class UIActions : MonoBehaviour
{
    public void Revive()
    {
        var pc = PlayerController.Instance;
        if (pc != null)
        {
            Debug.Log("UIActions：呼叫 PlayerController.RevivePlayer()");
            pc.RevivePlayer();
        }
        else
        {
            Debug.LogWarning("UIActions：找不到 PlayerController.Instance，無法復活");
        }
    }

    public void RestartScene()
    {
        // 如果你還是想保留重開整個場景功能，可以用這個
        var pc = PlayerController.Instance;
        if (pc != null)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log("UIActions：重新載入場景 " + scene.name);
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }
        else
        {
            Debug.LogWarning("UIActions：找不到 PlayerController.Instance，無法重啟場景");
        }
    }
}
