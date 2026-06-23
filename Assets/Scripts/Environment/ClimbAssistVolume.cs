using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClimbAssistVolume : MonoBehaviour
{
    [SerializeField] private Transform landingPoint;
    [SerializeField] private float activationRadius = 2.2f;
    [SerializeField] private float maxVerticalDelta = 2.6f;
    [SerializeField] private bool requirePlayerInFront = true;

    private Collider triggerCollider;
    private static readonly List<ClimbAssistVolume> ActiveVolumes = new List<ClimbAssistVolume>();

    private void OnEnable()
    {
        if (!ActiveVolumes.Contains(this)) ActiveVolumes.Add(this);
        triggerCollider = GetComponent<Collider>();
    }

    private void OnDisable()
    {
        ActiveVolumes.Remove(this);
    }

    public void Configure(Transform newLandingPoint, float newActivationRadius)
    {
        landingPoint = newLandingPoint;
        activationRadius = Mathf.Max(0.5f, newActivationRadius);
    }

    public static bool TryFindBest(Transform player, Collider playerCollider, out Vector3 position, out Quaternion rotation, out Vector3 topPoint)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        topPoint = Vector3.zero;
        if (player == null) return false;

        Scene scene = player.gameObject.scene;
        ClimbAssistVolume best = null;
        float bestScore = float.MaxValue;

        for (int i = ActiveVolumes.Count - 1; i >= 0; i--)
        {
            ClimbAssistVolume volume = ActiveVolumes[i];
            if (volume == null)
            {
                ActiveVolumes.RemoveAt(i);
                continue;
            }

            if (!volume.isActiveAndEnabled || volume.gameObject.scene != scene) continue;
            if (!volume.IsPlayerEligible(player, playerCollider, out float score)) continue;
            if (score >= bestScore) continue;

            best = volume;
            bestScore = score;
        }

        if (best == null) return false;

        Transform target = best.landingPoint != null ? best.landingPoint : best.transform;
        Vector3 forward = Vector3.ProjectOnPlane(best.transform.forward, Vector3.up);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(target.position - player.position, Vector3.up);
        }

        if (forward.sqrMagnitude <= 0.0001f) forward = player.forward;
        forward.Normalize();

        position = target.position;
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        topPoint = target.position;
        return true;
    }

    private bool IsPlayerEligible(Transform player, Collider playerCollider, out float score)
    {
        score = float.MaxValue;
        Vector3 playerPosition = player.position;
        Vector3 closest = triggerCollider != null
            ? triggerCollider.ClosestPoint(playerPosition)
            : transform.position;

        Vector3 planar = closest - playerPosition;
        planar.y = 0f;
        float planarDistance = planar.magnitude;
        if (planarDistance > activationRadius) return false;

        Transform target = landingPoint != null ? landingPoint : transform;
        float verticalDelta = target.position.y - ResolvePlayerBottom(player, playerCollider);
        if (verticalDelta < 0.2f || verticalDelta > maxVerticalDelta) return false;

        if (requirePlayerInFront)
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(playerPosition - transform.position, Vector3.up);
            Vector3 front = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);
            if (toPlayer.sqrMagnitude > 0.001f && front.sqrMagnitude > 0.001f && Vector3.Dot(toPlayer.normalized, front.normalized) < -0.25f)
            {
                return false;
            }
        }

        score = planarDistance + Mathf.Abs(verticalDelta - 1.1f) * 0.25f;
        return true;
    }

    private static float ResolvePlayerBottom(Transform player, Collider playerCollider)
    {
        return playerCollider != null ? playerCollider.bounds.min.y : player.position.y;
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = landingPoint != null ? landingPoint : transform;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.16f);
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
