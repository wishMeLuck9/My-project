using System;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    private readonly Collider[] overlapHits = new Collider[16];
    private PlayerInputReader inputReader;
    private Interactable currentInteractable;

    public bool HasNearbyInteractable => currentInteractable != null;
    public event Action<bool> InteractionAvailabilityChanged;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void OnEnable()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        if (inputReader != null) inputReader.InteractPressed += HandleInteractPressed;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.InteractPressed -= HandleInteractPressed;
    }

    private void Update()
    {
        SetCurrentInteractable(FindInteractable());
    }

    private void TryInteract()
    {
        Interactable interactable = FindInteractable();
        SetCurrentInteractable(interactable);
        interactable?.Interact();
    }

    private Interactable FindInteractable()
    {
        int mask = interactableLayer.value == 0 ? Physics.DefaultRaycastLayers : interactableLayer.value;
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, interactionRange, mask, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null) return interactable;
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

        return nearest;
    }

    private void SetCurrentInteractable(Interactable interactable)
    {
        if (currentInteractable == interactable) return;

        currentInteractable = interactable;
        InteractionAvailabilityChanged?.Invoke(currentInteractable != null);
    }

    private void HandleInteractPressed()
    {
        if (DialogueController.Instance != null && DialogueController.Instance.IsDialogueOpen) return;

        TryInteract();
    }
}
