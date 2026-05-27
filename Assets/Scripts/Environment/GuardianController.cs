using UnityEngine;

public class GuardianController : Interactable
{
    [SerializeField] private string guardianName;
    [SerializeField] private bool isForceGuardian;

    public string GuardianName => string.IsNullOrWhiteSpace(guardianName) ? name : guardianName;

    [SerializeField] private string[] customLines;

    public void Configure(string newName, bool forceGuardian, string[] lines = null)
    {
        guardianName = newName;
        isForceGuardian = forceGuardian;
        customLines = lines;
    }

    public override void Interact()
    {
        EvaluatePlayer();
    }

    public void EvaluatePlayer()
    {
        if (DialogueController.Instance == null) return;
        DialogueController.Instance.ShowDialogue(GuardianName, BuildEvaluationMessage());
    }

    public void ReceiveAttack()
    {
        if (WorldState.Instance != null)
        {
            WorldState.Instance.aggressionChoice += 1;
            WorldState.Instance.pursuitLevel += 15;
        }

        if (TryGetComponent(out Renderer renderer))
        {
            renderer.material.color = isForceGuardian ? Color.red : new Color(0.45f, 0.15f, 0.65f, 1f);
        }

        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialogue(GuardianName, "Суд не прерывается ударом. Удар только добавляет строку.");
        }
    }

    public string BuildEvaluationMessage()
    {
        if (customLines != null && customLines.Length > 0)
        {
            return customLines[Random.Range(0, customLines.Length)];
        }

        WorldState state = WorldState.Instance;
if (state == null) return "Состояние не найдено. Проверка отложена.";

        if (isForceGuardian)
        {
            if (state.pursuitLevel > 30 || state.aggressionChoice > 0 || state.shadowViolence > 0)
            {
                if (TryGetComponent(out Renderer renderer)) renderer.material.color = Color.red;
                return "Ты пришел с движением. Движение похоже на угрозу. Угроза должна быть остановлена.";
            }

            return "Твоё присутствие тихое. Это не оправдание, но это... приемлемо.";
        }

        if (state.paidMemory)
            return "Ты уже платил. Но я не вижу, кем ты был до оплаты. Это удобно. Для нас.";

        if (state.helpedShadow)
            return "В тебе есть отголосок чужой тени. Это ошибка, но она была настоящей.";

        if (state.paidEntryFragment)
            return "Ты отдал свет, чтобы пройти внутрь. Теперь ворота проверят, что осталось.";

        return "Твой след пуст. Ты сохранил всё, но не принес ничего.";
    }
}
