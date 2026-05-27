using UnityEngine;

public class PrototypeCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -14f);
    [SerializeField] private float lookAtHeight = 1.4f;
    [SerializeField] private float followSharpness = 12f;

    public void Configure(Transform newTarget, Vector3 newOffset, float newLookAtHeight)
    {
        target = newTarget;
        offset = newOffset;
        lookAtHeight = newLookAtHeight;
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        LookAtTarget();
    }

    private void SnapToTarget()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        LookAtTarget();
    }

    private void LookAtTarget()
    {
        Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}
