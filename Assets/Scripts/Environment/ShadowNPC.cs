using UnityEngine;
using System.Collections.Generic;

public class ShadowNPC : Interactable
{
    [SerializeField] private string npcName = "SHADOW";
    [SerializeField] private string[] lines;
    [SerializeField] private bool hasChoices;
    [SerializeField] private bool reactsToNightPath;
    [SerializeField] private string peacefulLine;
    [SerializeField] private string violentLine;

    public void Configure(string newName, string[] newLines, bool choices)
    {
        npcName = newName;
        lines = newLines;
        hasChoices = choices;
    }

    public void ConfigureNightReaction(string newName, string neutral, string peaceful, string violent)
    {
        npcName = newName;
        lines = new[] { neutral };
        hasChoices = false;
        reactsToNightPath = true;
        peacefulLine = peaceful;
        violentLine = violent;
    }

    public override void Interact()
    {
        if (DialogueController.Instance == null || WorldState.Instance == null) return;

        if (reactsToNightPath)
        {
            if (WorldState.Instance.nightViolenceAttempted)
            {
                DialogueController.Instance.ShowDialogue(npcName, violentLine);
            }
            else if (WorldState.Instance.nightFragmentRoute == WorldState.NightFragmentRoute.Mercy)
            {
                DialogueController.Instance.ShowDialogue(npcName, peacefulLine);
            }
            else
            {
                DialogueController.Instance.ShowDialogue(npcName, GetLineOrFallback("Я не знаю, зачем ты здесь."));
            }
            return;
        }

        if (hasChoices)
        {
            DialogueController.Instance.ShowChoices(
                npcName,
                GetLineOrFallback("Если ты поможешь мне, они увидят тебя."),
                new List<DialogueChoice>
                {
                    new DialogueChoice("Помочь", () =>
                    {
                        WorldState.Instance.helpedShadow = true;
                        WorldState.Instance.mercyChoice += 1;
                        WorldState.Instance.recognition += 10;
                        WorldState.Instance.lightLevel += 1;
                        DialogueController.Instance.ShowDialogue(npcName, "Ты сделал это не для меня. Но я запомню один цикл.");
                    }),
                    new DialogueChoice("Игнорировать", () =>
                    {
                        WorldState.Instance.ignoredShadow = true;
                        WorldState.Instance.apathyTimer += 10;
                        DialogueController.Instance.ShowDialogue(npcName, "Так проще. Так система любит.");
                    }),
                    new DialogueChoice("Оттолкнуть", () =>
                    {
                        WorldState.Instance.aggressionChoice += 1;
                        WorldState.Instance.pursuitLevel += 10;
                        DialogueController.Instance.ShowDialogue(npcName, "Ночь быстро учит тебя быть сильным.");
                    })
                });
            return;
        }

        if (lines != null && lines.Length > 0)
        {
            DialogueController.Instance.ShowDialogue(npcName, lines[Random.Range(0, lines.Length)]);
        }
    }

    private string GetLineOrFallback(string fallback)
    {
        if (lines != null && lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])) return lines[0];
        return fallback;
    }
}
