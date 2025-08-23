using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuKeyboardNavigator : MonoBehaviour
{
    [SerializeField] List<Selectable> items = new(); // 依鍵盤切換順序放 Button
    [SerializeField] int startIndex = 0;             // 預設選中的項目

    int index;

    void OnEnable()
    {
        index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, items.Count - 1));
        Select(index);
    }

    void Update()
    {
        if (items.Count == 0) return;

        // 方向鍵移動（上下左 = 前一個；下右 = 下一個）
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
            Move(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            Move(+1);

        // Enter / Space 觸發目前項目的 onClick（若是 Button）
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (items[index] is Button b && b.IsInteractable())
                b.onClick?.Invoke();
        }
    }

    void Move(int delta)
    {
        int n = items.Count;
        int tries = 0;
        do
        {
            index = (index + delta + n) % n; // 迴圈選擇
            tries++;
        } while (tries <= n && (items[index] == null || !items[index].IsInteractable()));

        Select(index);
    }

    void Select(int i)
    {
        if (items[i] == null) return;
        EventSystem.current?.SetSelectedGameObject(items[i].gameObject);
    }
}
