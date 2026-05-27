using UnityEngine;

public class FinalGateEntryTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;
        if (DialogueController.Instance == null) return;

        DialogueController.Instance.ShowDialogue(
            "GATE",
            "Проход зарегистрирован. Врата не держатся сами. Их держит цена.");
    }
}
