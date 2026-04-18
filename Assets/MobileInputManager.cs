using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    void LateUpdate()
    {
        // Safety: if no touch/mouse is held, stop movement.
        if (InputBridge.useMobileInput)
        {
            bool holding = false;
            if (Input.touchCount > 0) holding = true;
            if (Input.GetMouseButton(0)) holding = true;
            if (!holding) InputBridge.SetMove(Vector2.zero);
        }
        InputBridge.EndFrame();
    }
}
