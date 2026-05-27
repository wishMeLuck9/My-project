using UnityEngine;
using System.Collections.Generic;

public class PriceAltar : Interactable
{
    public override void Interact()
    {
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        DialogueController.Instance.ShowChoices(
            "PRICE_ALTAR",
            "Проход требует потери. Выбери, что перестанет быть твоим.",
            new List<DialogueChoice>
            {
                new DialogueChoice("Отдать память", () =>
                {
                    WorldState.Instance.paidMemory = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue("SYSTEM", "Память принята. Воспоминание о цене удалено не полностью.");
                }),
                new DialogueChoice("Отдать имя", () =>
                {
                    WorldState.Instance.paidName = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue("SYSTEM", "Имя принято. Обращение к тебе больше не гарантирует ответ.");
                }),
                new DialogueChoice("Отдать радость", () =>
                {
                    WorldState.Instance.paidJoy = true;
                    WorldState.Instance.nightDebt += 1;
                    DialogueController.Instance.ShowDialogue("SYSTEM", "Радость принята. Улыбка сохранена как внешний жест.");
                }),
                new DialogueChoice("Отказаться", () =>
                {
                    WorldState.Instance.resistedSystem = true;
                    WorldState.Instance.pursuitLevel += 20;
                    WorldState.Instance.nonStepBias += 10;
                    DialogueController.Instance.ShowDialogue("SYSTEM", "Отказ записан как лишнее движение.");
                })
            });
    }
}
