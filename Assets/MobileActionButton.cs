using UnityEngine;
using UnityEngine.EventSystems;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ActionType { Interact, Run }
    public ActionType action = ActionType.Interact;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (action == ActionType.Interact)
            InputBridge.SetInteract(true);
        else
            InputBridge.SetRun(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (action == ActionType.Interact)
            InputBridge.SetInteract(false);
        else
            InputBridge.SetRun(false);
    }
}
