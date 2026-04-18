using UnityEngine;
using UnityEngine.EventSystems;

// Drag-based D-pad: slide finger across the pad without lifting.
public class MobileDPadArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerExitHandler
{
    [Tooltip("Deadzone radius (0-1) relative to the shortest side of the pad.")]
    [Range(0f, 0.5f)]
    public float deadzone = 0.15f;

    [Tooltip("If true, allow diagonal input. If false, snap to 4 directions.")]
    public bool allowDiagonal = false;

    [Header("Axis Fix (if directions are wrong)")]
    public bool swapXY = false;
    public bool invertX = false;
    public bool invertY = false;

    RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateDirection(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDirection(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputBridge.SetMove(Vector2.zero);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InputBridge.SetMove(Vector2.zero);
    }

    void OnDisable()
    {
        InputBridge.SetMove(Vector2.zero);
    }

    void UpdateDirection(PointerEventData eventData)
    {
        if (rect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 half = rect.rect.size * 0.5f;
        if (half.x <= 0.01f || half.y <= 0.01f) return;

        // Normalize to -1..1 range.
        Vector2 n = new Vector2(localPoint.x / half.x, localPoint.y / half.y);
        float mag = n.magnitude;

        if (mag < deadzone)
        {
            InputBridge.SetMove(Vector2.zero);
            return;
        }

        if (swapXY)
        {
            n = new Vector2(n.y, n.x);
        }
        if (invertX) n.x = -n.x;
        if (invertY) n.y = -n.y;

        if (!allowDiagonal)
        {
            if (Mathf.Abs(n.x) >= Mathf.Abs(n.y))
                n = new Vector2(Mathf.Sign(n.x), 0f);
            else
                n = new Vector2(0f, Mathf.Sign(n.y));
        }
        else
        {
            n = n.normalized;
        }

        InputBridge.SetMove(n);
    }
}
