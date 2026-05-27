using UnityEngine;
using System.Collections;

public class NonStepTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TriggerRoutine(other.GetComponent<PlayerController3D>()));
    }

    private IEnumerator TriggerRoutine(PlayerController3D player)
    {
        if (player != null) player.SetCanMove(false);
        if (WorldState.Instance != null) WorldState.Instance.nonStepBias += 20;

        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialogue(
                "SYSTEM",
                "Действие зарегистрировано. Ввод корректен. Результат отклонен. Ты сделал всё правильно. Но не получилось.");
        }

        yield return new WaitForSeconds(3f);

        if (DialogueController.Instance == null && player != null)
        {
            player.SetCanMove(true);
        }
    }
}
