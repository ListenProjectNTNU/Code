using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastProbe : MonoBehaviour
{
    private PointerEventData _ped;
    private readonly List<RaycastResult> _results = new List<RaycastResult>();

    void Awake()
    {
        if (EventSystem.current == null)
            Debug.LogError("❌ 場景沒有 EventSystem！");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _results.Clear();
            _ped = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            EventSystem.current.RaycastAll(_ped, _results);

            if (_results.Count == 0)
            {
                Debug.Log("🧪 此處沒有任何可點擊 UI。");
                return;
            }

            Debug.Log($"🧪 Raycast 命中 {_results.Count} 個 UI（最上面列在最前）:");
            for (int i = 0; i < _results.Count; i++)
            {
                var r = _results[i];
                var canvas = r.gameObject.GetComponentInParent<Canvas>();
                var order = canvas ? canvas.sortingOrder : 0;
                Debug.Log($"[{i}] {r.gameObject.name}  (CanvasOrder={order})  module={r.module}");
            }
        }
    }
}
