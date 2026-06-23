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

    private ExteriorPursuer exteriorPursuer;
    private PrototypeShadowActor shadowActor;

    private void Awake()
    {
        exteriorPursuer = GetComponentInParent<ExteriorPursuer>();
        shadowActor = GetComponentInParent<PrototypeShadowActor>();
    }

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
        if (TryShowHostileInteraction()) return;

        LocalizationManager localizer = LocalizationManager.EnsureInstance();
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
                DialogueController.Instance.ShowDialogue(npcName, GetLineOrFallback(localizer.Get("raw.shadow_npc.unknown")));
            }
            return;
        }

        if (hasChoices)
        {
            DialogueController.Instance.ShowChoices(
                npcName,
                GetLineOrFallback(localizer.Get("raw.shadow_npc.help_prompt")),
                new List<DialogueChoice>
                {
                    new DialogueChoice(localizer.Get("raw.shadow_npc.choice.help"), () =>
                    {
                        WorldState.Instance.helpedShadow = true;
                        WorldState.Instance.mercyChoice += 1;
                        WorldState.Instance.recognition += 10;
                        WorldState.Instance.lightLevel += 1;
                        DialogueController.Instance.ShowDialogue(npcName, localizer.Get("raw.shadow_npc.help"));
                    }),
                    new DialogueChoice(localizer.Get("raw.shadow_npc.choice.ignore"), () =>
                    {
                        WorldState.Instance.ignoredShadow = true;
                        WorldState.Instance.apathyTimer += 10;
                        DialogueController.Instance.ShowDialogue(npcName, localizer.Get("raw.shadow_npc.ignore"));
                    }),
                    new DialogueChoice(localizer.Get("raw.shadow_npc.choice.push"), () =>
                    {
                        WorldState.Instance.aggressionChoice += 1;
                        WorldState.Instance.pursuitLevel += 10;
                        DialogueController.Instance.ShowDialogue(npcName, localizer.Get("raw.shadow_npc.push"));
                    })
                });
            return;
        }

        if (lines != null && lines.Length > 0)
        {
            DialogueController.Instance.ShowDialogue(npcName, lines[Random.Range(0, lines.Length)]);
        }
    }

    private bool TryShowHostileInteraction()
    {
        if (exteriorPursuer != null && exteriorPursuer.IsHunting)
        {
            exteriorPursuer.ShowHuntingInteractionResponse();
            return true;
        }

        if (shadowActor != null && shadowActor.IsHunting)
        {
            RuntimeHudController.Instance?.ShowAmbientMessage(
                LocalizationManager.EnsureInstance().Get(Random.value > 0.5f
                    ? "raw.shadow.hunt.interact.close"
                    : "raw.shadow.hunt.interact.angry"),
                1.8f);
            return true;
        }

        return false;
    }

    private string GetLineOrFallback(string fallback)
    {
        if (lines != null && lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])) return lines[0];
        return fallback;
    }
}
