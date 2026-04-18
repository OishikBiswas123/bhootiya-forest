using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MobileDPadButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IDragHandler
{
    public Vector2 direction = Vector2.zero; // (1,0), (-1,0), (0,1), (0,-1)

    private static bool isPointerDown = false;
    private static int activePointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        activePointerId = eventData.pointerId;
        InputBridge.SetMove(direction);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPointerDown || eventData.pointerId != activePointerId) return;

        // Raycast UI under finger and switch to the button being touched.
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            var btn = results[i].gameObject.GetComponent<MobileDPadButton>();
            if (btn != null)
            {
                InputBridge.SetMove(btn.direction);
                return;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Allow sliding finger across buttons without lifting.
        if (isPointerDown && eventData.pointerId == activePointerId)
        {
            InputBridge.SetMove(direction);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            isPointerDown = false;
            activePointerId = int.MinValue;
            InputBridge.SetMove(Vector2.zero);
        }
    }
}
