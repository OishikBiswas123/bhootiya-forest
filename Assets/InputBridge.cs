using UnityEngine;

public static class InputBridge
{
    public static bool useMobileInput = true;

    private static float mobileHorizontal = 0f;
    private static float mobileVertical = 0f;
    private static bool mobileInteractDown = false;
    private static bool mobileInteractHeld = false;
    private static bool mobileRunDown = false;
    private static bool mobileRunUp = false;
    private static bool mobileRunHeld = false;

    public static float GetAxisRaw(string axis)
    {
        if (useMobileInput)
        {
            if (axis == "Horizontal")
            {
                if (Mathf.Abs(mobileHorizontal) > 0.01f)
                    return Mathf.Sign(mobileHorizontal);
            }
            else if (axis == "Vertical")
            {
                if (Mathf.Abs(mobileVertical) > 0.01f)
                    return Mathf.Sign(mobileVertical);
            }
        }

        return Input.GetAxisRaw(axis);
    }

    public static bool GetKeyDown(KeyCode key)
    {
        bool mobile = false;
        if (useMobileInput)
        {
            if (key == KeyCode.X) mobile = mobileInteractDown;
            if (key == KeyCode.Z) mobile = mobileRunDown;
        }
        return Input.GetKeyDown(key) || mobile;
    }

    public static bool GetKey(KeyCode key)
    {
        bool mobile = false;
        if (useMobileInput)
        {
            if (key == KeyCode.X) mobile = mobileInteractHeld;
            if (key == KeyCode.Z) mobile = mobileRunHeld;
        }
        return Input.GetKey(key) || mobile;
    }

    public static bool GetKeyUp(KeyCode key)
    {
        bool mobile = false;
        if (useMobileInput)
        {
            if (key == KeyCode.Z) mobile = mobileRunUp;
        }
        return Input.GetKeyUp(key) || mobile;
    }

    public static void SetMove(Vector2 dir)
    {
        mobileHorizontal = Mathf.Clamp(dir.x, -1f, 1f);
        mobileVertical = Mathf.Clamp(dir.y, -1f, 1f);
    }

    public static void SetInteract(bool pressed)
    {
        if (pressed)
        {
            if (!mobileInteractHeld) mobileInteractDown = true;
            mobileInteractHeld = true;
        }
        else
        {
            mobileInteractHeld = false;
        }
    }

    public static void SetRun(bool pressed)
    {
        if (pressed)
        {
            if (!mobileRunHeld) mobileRunDown = true;
            mobileRunHeld = true;
        }
        else
        {
            if (mobileRunHeld) mobileRunUp = true;
            mobileRunHeld = false;
        }
    }

    public static void EndFrame()
    {
        mobileInteractDown = false;
        mobileRunDown = false;
        mobileRunUp = false;
        
        if (UIManager.Instance != null && 
            UIManager.Instance.choicePanel != null && 
            !UIManager.Instance.choicePanel.activeSelf)
        {
            mobileInteractDown = false;
            mobileInteractHeld = false;
        }
    }
}
