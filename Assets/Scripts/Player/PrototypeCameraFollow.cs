using UnityEngine;
using UnityEngine.InputSystem;

public class PrototypeCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -14f);
    [SerializeField] private float lookAtHeight = 1.4f;
    [SerializeField] private float followSharpness = 12f;
    [SerializeField] private float rotationSharpness = 18f;
    [SerializeField] private float mouseYawSensitivity = 0.12f;
    [SerializeField] private float mousePitchSensitivity = 0.08f;
    [SerializeField] private float gamepadYawSpeed = 140f;
    [SerializeField] private float gamepadPitchSpeed = 90f;
    [SerializeField] private float minPitch = 22f;
    [SerializeField] private float maxPitch = 58f;
    [SerializeField] private float minDistance = 5.5f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float zoomSpeed = 0.018f;
    [SerializeField] private float obstructionRadius = 0.32f;
    [SerializeField] private float obstructionPadding = 0.35f;
    [SerializeField] private LayerMask obstructionMask = ~0;

    private readonly RaycastHit[] obstructionHits = new RaycastHit[10];
    private float yaw;
    private float pitch;
    private float distance;

    public void Configure(Transform newTarget, Vector3 newOffset, float newLookAtHeight)
    {
        target = newTarget;
        offset = newOffset;
        lookAtHeight = newLookAtHeight;
        RebuildOrbitFromOffset();
        SnapToTarget();
    }

    private void Awake()
    {
        RebuildOrbitFromOffset();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleOrbitInput();

        Vector3 lookPoint = GetLookPoint();
        Vector3 desiredOffset = GetOrbitOffset();
        Vector3 desiredPosition = ResolveObstructedPosition(lookPoint, lookPoint + desiredOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        LookAtPoint(lookPoint);
    }

    private void SnapToTarget()
    {
        if (target == null) return;

        Vector3 lookPoint = GetLookPoint();
        transform.position = ResolveObstructedPosition(lookPoint, lookPoint + GetOrbitOffset());
        LookAtPoint(lookPoint);
    }

    private void RebuildOrbitFromOffset()
    {
        Vector3 planarOffset = new Vector3(offset.x, 0f, offset.z);
        Vector3 lookRelativeOffset = new Vector3(offset.x, offset.y - lookAtHeight, offset.z);
        distance = Mathf.Clamp(lookRelativeOffset.magnitude, minDistance, maxDistance);
        yaw = planarOffset.sqrMagnitude > 0.001f ? Vector3.SignedAngle(Vector3.back, planarOffset.normalized, Vector3.up) : yaw;

        float rawPitch = Mathf.Atan2(Mathf.Max(0.1f, offset.y - lookAtHeight), Mathf.Max(0.1f, planarOffset.magnitude)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
    }

    private void HandleOrbitInput()
    {
        if (Time.timeScale <= Mathf.Epsilon || IsGameplayInputBlocked()) return;

        float sensitivity = SettingsManager.Instance != null ? SettingsManager.Instance.MouseSensitivity : 1f;
        bool invertY = SettingsManager.Instance != null && SettingsManager.Instance.InvertY;

        Mouse mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            yaw += mouseDelta.x * mouseYawSensitivity * sensitivity;
            pitch += mouseDelta.y * mousePitchSensitivity * sensitivity * (invertY ? 1f : -1f);

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.rightStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                yaw += stick.x * gamepadYawSpeed * sensitivity * Time.unscaledDeltaTime;
                pitch += stick.y * gamepadPitchSpeed * sensitivity * (invertY ? 1f : -1f) * Time.unscaledDeltaTime;
            }
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private Vector3 GetOrbitOffset()
    {
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        return orbitRotation * (Vector3.back * distance);
    }

    private Vector3 GetLookPoint()
    {
        return target.position + Vector3.up * lookAtHeight;
    }

    private Vector3 ResolveObstructedPosition(Vector3 lookPoint, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - lookPoint;
        float cameraDistance = toCamera.magnitude;
        if (cameraDistance <= 0.01f) return desiredPosition;

        Vector3 direction = toCamera / cameraDistance;
        int mask = obstructionMask.value == 0 ? Physics.DefaultRaycastLayers : obstructionMask.value;
        int hitCount = Physics.SphereCastNonAlloc(
            lookPoint,
            obstructionRadius,
            direction,
            obstructionHits,
            cameraDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = obstructionHits[i].collider;
            if (hitCollider == null) continue;
            if (hitCollider.transform == target || hitCollider.transform.IsChildOf(target)) continue;

            nearestDistance = Mathf.Min(nearestDistance, obstructionHits[i].distance);
        }

        if (float.IsPositiveInfinity(nearestDistance)) return desiredPosition;

        float resolvedDistance = Mathf.Clamp(nearestDistance - obstructionPadding, 1.25f, cameraDistance);
        return lookPoint + direction * resolvedDistance;
    }

    private void LookAtPoint(Vector3 lookPoint)
    {
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
    }

    private static bool IsGameplayInputBlocked()
    {
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return true;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return true;
        return Cursor.lockState != CursorLockMode.Locked;
    }
}
