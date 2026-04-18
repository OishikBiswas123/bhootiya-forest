using UnityEngine;
using UnityEngine.EventSystems;

public class UITransformDragger : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private static UITransformDragger activeScaler;
    public RectTransform target;
    public float scaleSpeed = 0.25f;
    public float minScale = 0.5f;
    public float maxScale = 2.0f;
    public bool clampToParent = true;
    public Vector2 clampPadding = new Vector2(20f, 20f);

    private bool dragging = false;
    private Vector2 dragOffset;

    void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsEditMode()) return;
        dragging = true;
        activeScaler = this;
        if (target != null && target.parent is RectTransform parentRect)
        {
            Vector2 parentPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out parentPoint);
            dragOffset = parentPoint - target.anchoredPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsEditMode() || !dragging) return;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Vector2 pos = localPoint - dragOffset;
            if (clampToParent && target.parent is RectTransform parentRect)
            {
                Vector2 parentHalf = parentRect.rect.size * 0.5f;
                Vector2 targetSize = Vector2.Scale(target.rect.size, target.localScale);
                Vector2 targetHalf = targetSize * 0.5f;
                float minX = -parentHalf.x + targetHalf.x + clampPadding.x;
                float maxX = parentHalf.x - targetHalf.x - clampPadding.x;
                float minY = -parentHalf.y + targetHalf.y + clampPadding.y;
                float maxY = parentHalf.y - targetHalf.y - clampPadding.y;
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }
            target.anchoredPosition = pos;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        if (activeScaler == this) activeScaler = null;
    }

    void Update()
    {
        if (!IsEditMode()) return;
        if (activeScaler != this) return;

        // Mouse wheel scaling for editor/testing
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f && RectTransformUtility.RectangleContainsScreenPoint(target, Input.mousePosition))
        {
            float s = Mathf.Clamp(target.localScale.x + wheel * scaleSpeed * 0.1f, minScale, maxScale);
            target.localScale = new Vector3(s, s, target.localScale.z);
        }

        // Pinch zoom for touch
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float diff = currDist - prevDist;
            if (Mathf.Abs(diff) > 0.5f)
            {
                float s = Mathf.Clamp(target.localScale.x + diff * 0.002f, minScale, maxScale);
                target.localScale = new Vector3(s, s, target.localScale.z);
            }
        }
    }

    bool IsEditMode()
    {
        return LayoutEditorManager.Instance != null && LayoutEditorManager.Instance.IsEditMode();
    }
}
