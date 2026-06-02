using UnityEngine;

public static class PhysicsLineOfSight
{
    private static readonly RaycastHit[] Hits = new RaycastHit[24];

    public static bool HasClearPath(
        Transform originRoot,
        Transform targetRoot,
        Vector3 origin,
        Vector3 target,
        int mask)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f) return true;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction / distance,
            Hits,
            distance,
            mask,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        Transform nearest = null;
        for (int i = 0; i < hitCount; i++)
        {
            Transform hit = Hits[i].collider != null ? Hits[i].collider.transform : null;
            if (hit == null || IsPartOf(hit, originRoot)) continue;
            if (Hits[i].distance >= nearestDistance) continue;

            nearestDistance = Hits[i].distance;
            nearest = hit;
        }

        return nearest == null || IsPartOf(nearest, targetRoot);
    }

    private static bool IsPartOf(Transform candidate, Transform root)
    {
        return candidate != null
            && root != null
            && (candidate == root || candidate.IsChildOf(root) || root.IsChildOf(candidate));
    }
}
