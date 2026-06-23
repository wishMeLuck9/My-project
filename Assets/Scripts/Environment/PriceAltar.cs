using UnityEngine;
using System.Collections.Generic;

public class PriceAltar : Interactable
{
    public override void Interact()
    {
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
        string systemSpeaker = localizer.Get("speaker.system", "SYSTEM");

        DialogueController.Instance.ShowChoices(
            localizer.Get("speaker.price_altar", "PRICE_ALTAR"),
            localizer.Get("raw.price.prompt"),
            new List<DialogueChoice>
            {
                new DialogueChoice(localizer.Get("raw.price.memory"), () =>
                {
                    WorldState.Instance.paidMemory = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue(systemSpeaker, localizer.Get("raw.price.memory.accepted"));
                }),
                new DialogueChoice(localizer.Get("raw.price.name"), () =>
                {
                    WorldState.Instance.paidName = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue(systemSpeaker, localizer.Get("raw.price.name.accepted"));
                }),
                new DialogueChoice(localizer.Get("raw.price.joy"), () =>
                {
                    WorldState.Instance.paidJoy = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue(systemSpeaker, localizer.Get("raw.price.joy.accepted"));
                }),
                new DialogueChoice(localizer.Get("raw.price.refuse"), () =>
                {
                    WorldState.Instance.resistedSystem = true;
                    WorldState.Instance.pursuitLevel += 20;
                    WorldState.Instance.nonStepBias += 10;
                    DialogueController.Instance.ShowDialogue(systemSpeaker, localizer.Get("raw.price.refused"));
                })
            });
    }
}
