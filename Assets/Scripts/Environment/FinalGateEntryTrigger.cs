using UnityEngine;

public class FinalGateEntryTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        FinalGateOutcomeController outcome = FindFirstObjectByType<FinalGateOutcomeController>();
        if (outcome == null) return;

        outcome.BeginResolution();
    }
}
