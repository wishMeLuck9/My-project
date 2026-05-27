using UnityEngine;
using System.Collections.Generic;

public class LightFragmentPickup : Interactable
{
    public override void Interact()
    {
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        DialogueController.Instance.ShowChoices(
            "LIGHT_FRAGMENT",
            "Это не награда. Это часть тебя, которая еще не поняла, что потеряна. Поднять?",
            new List<DialogueChoice>
            {
                new DialogueChoice("Поднять", () =>
                {
                    WorldState.Instance.AddLight(1);
                    WorldState.Instance.foundTraceCount += 1;
                    gameObject.SetActive(false);
                    DialogueController.Instance.ShowDialogue("SYSTEM", "Фрагмент принят. Теперь вход может назвать цену.");
                    Destroy(gameObject, 0.1f);
                }),
                new DialogueChoice("Оставить", () =>
                {
                    WorldState.Instance.AddApathy(10);
                    DialogueController.Instance.ShowDialogue("SHADOW", "Умно. Или трусливо. Мы не решили.");
                })
            });
    }
}
