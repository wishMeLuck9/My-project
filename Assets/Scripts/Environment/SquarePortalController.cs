using UnityEngine;

public class SquarePortalController : MonoBehaviour
{
    [SerializeField] private LightFragmentPickup.FragmentKind requiredFragment = LightFragmentPickup.FragmentKind.Exterior;
    [SerializeField] private bool unlockWhenFragmentCollected = true;
    [SerializeField] private bool startsUnlocked;
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private Collider physicalBlocker;
    [SerializeField] private float openDistance = 1.5f;

    private Vector3 leftClosedPosition;
    private Vector3 rightClosedPosition;
    private bool positionsCaptured;

    public bool IsUnlocked { get; private set; }

    private void Awake()
    {
        CaptureClosedPositions();
        ApplyInitialState();
    }

    private void OnEnable()
    {
        LightFragmentPickup.FragmentCollected += HandleFragmentCollected;
    }

    private void OnDisable()
    {
        LightFragmentPickup.FragmentCollected -= HandleFragmentCollected;
    }

    public void Configure(
        LightFragmentPickup.FragmentKind fragmentKind,
        bool unlockOnCollection,
        bool initiallyUnlocked,
        Transform newLeftDoor,
        Transform newRightDoor,
        Collider newPhysicalBlocker,
        float newOpenDistance)
    {
        requiredFragment = fragmentKind;
        unlockWhenFragmentCollected = unlockOnCollection;
        startsUnlocked = initiallyUnlocked;
        leftDoor = newLeftDoor;
        rightDoor = newRightDoor;
        physicalBlocker = newPhysicalBlocker;
        openDistance = Mathf.Max(0f, newOpenDistance);
        positionsCaptured = false;
        CaptureClosedPositions();

        if (startsUnlocked || HasRequiredFragment())
        {
            Unlock();
        }
        else
        {
            Lock();
        }
    }

    public void Lock()
    {
        CaptureClosedPositions();
        IsUnlocked = false;
        if (leftDoor != null) leftDoor.localPosition = leftClosedPosition;
        if (rightDoor != null) rightDoor.localPosition = rightClosedPosition;
        if (physicalBlocker != null) physicalBlocker.enabled = true;
    }

    public void SetUnlockWhenFragmentCollected(bool state)
    {
        unlockWhenFragmentCollected = state;
    }

    public void SetPortalVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = visible;
        }

        foreach (Light light in GetComponentsInChildren<Light>(true))
        {
            light.enabled = visible;
        }
    }

    public void Unlock()
    {
        CaptureClosedPositions();
        IsUnlocked = true;
        if (leftDoor != null) leftDoor.localPosition = leftClosedPosition + Vector3.left * openDistance;
        if (rightDoor != null) rightDoor.localPosition = rightClosedPosition + Vector3.right * openDistance;
        if (physicalBlocker != null) physicalBlocker.enabled = false;
    }

    private void ApplyInitialState()
    {
        if (startsUnlocked || HasRequiredFragment())
        {
            Unlock();
        }
        else
        {
            Lock();
        }
    }

    private void HandleFragmentCollected(LightFragmentPickup.FragmentKind fragmentKind)
    {
        if (unlockWhenFragmentCollected && fragmentKind == requiredFragment)
        {
            Unlock();
        }
    }

    private bool HasRequiredFragment()
    {
        return WorldState.Instance != null && WorldState.Instance.HasFragment(requiredFragment);
    }

    private void CaptureClosedPositions()
    {
        if (positionsCaptured) return;

        leftClosedPosition = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
        rightClosedPosition = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
        positionsCaptured = true;
    }
}
