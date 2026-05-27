using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    private readonly Collider[] overlapHits = new Collider[16];

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        TryInteract();
    }

    private void TryInteract()
    {
        int mask = interactableLayer.value == 0 ? Physics.DefaultRaycastLayers : interactableLayer.value;
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, interactionRange, mask, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }
        }

        Interactable nearest = null;
        float nearestDistance = float.MaxValue;
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, overlapHits, mask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidateCollider = overlapHits[i];
            if (candidateCollider == null) continue;

            Interactable candidate = candidateCollider.GetComponentInParent<Interactable>();
            if (candidate == null) continue;

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance >= nearestDistance) continue;

            nearest = candidate;
            nearestDistance = distance;
        }

        if (nearest != null) nearest.Interact();
    }
}
