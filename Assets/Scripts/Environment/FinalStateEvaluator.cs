using UnityEngine;
using System.Collections.Generic;

public class FinalStateEvaluator : MonoBehaviour
{
    private struct DialogueStep
    {
        public readonly string Speaker;
        public readonly string Text;

        public DialogueStep(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    private void Start()
    {
        Invoke(nameof(StartFinalSequence), 2f);
    }

    private void StartFinalSequence()
    {
        if (DialogueController.Instance == null) return;

        List<DialogueStep> steps = new List<DialogueStep>
        {
            new DialogueStep("GATE", "Перед тобой ворота. Или суд. Или ловушка.")
        };

        GuardianController[] guardians = FindObjectsByType<GuardianController>(FindObjectsSortMode.None);
        foreach (GuardianController guardian in guardians)
        {
            steps.Add(new DialogueStep(guardian.GuardianName, guardian.BuildEvaluationMessage()));
        }

        WorldState state = WorldState.Instance;
        if (state != null && state.shadowViolence > 0)
        {
            steps.Add(new DialogueStep("SYSTEM", "Ночная сила подтверждена. Милосердие не подтверждено."));
        }

        if (state != null && state.nonStepBias > 15)
        {
            steps.Add(new DialogueStep("SYSTEM", "Ошибка 9. Объект не должен быть здесь. Объект всё ещё здесь."));
        }

        ShowSequence(steps, 0);
    }

    private void ShowSequence(IReadOnlyList<DialogueStep> steps, int index)
    {
        if (index >= steps.Count || DialogueController.Instance == null) return;

        DialogueStep step = steps[index];
        DialogueController.Instance.ShowDialogue(step.Speaker, step.Text, () => ShowSequence(steps, index + 1));
    }
}
