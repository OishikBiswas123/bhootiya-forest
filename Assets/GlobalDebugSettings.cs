using UnityEngine;

public class GlobalDebugSettings : MonoBehaviour
{
    [Header("Master Debug Switch")]
    public bool enableAllLogs = false;

    public static bool EnableAllLogs { get; private set; } = false;

    void Awake()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Update()
    {
        // Keep runtime/static value in sync with inspector toggle.
        if (EnableAllLogs != enableAllLogs)
        {
            Apply();
        }
    }

    void Apply()
    {
        EnableAllLogs = enableAllLogs;
    }
}

