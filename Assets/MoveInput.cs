using UnityEngine;

public class MobileInput : MonoBehaviour
{
    public static MobileInput Instance;

    [HideInInspector] public bool up, down, left, right;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Called by EventTrigger → PointerDown
    public void PressUp() => up = true;
    public void PressDown() => down = true;
    public void PressLeft() => left = true;
    public void PressRight() => right = true;

    // Called by EventTrigger → PointerUp
    public void ReleaseUp() => up = false;
    public void ReleaseDown() => down = false;
    public void ReleaseLeft() => left = false;
    public void ReleaseRight() => right = false;
}
