using UnityEngine;

public class FinalStateEvaluator : MonoBehaviour
{
    private void Start()
    {
        if (FindFirstObjectByType<FinalGateOutcomeController>() == null)
        {
            Debug.LogWarning("FinalStateEvaluator is deprecated; add FinalGateOutcomeController to drive the final sequence.", this);
        }
    }
}
