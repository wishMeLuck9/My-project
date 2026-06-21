using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerHumanoidControllerClimbBuilder
{
    private const string ControllerPath = "Assets/Art/Characters/Player/Controllers/PlayerHumanoid.controller";
    private const string PlayerAnimationsFolder = "Assets/Art/Characters/Player/Animations";
    private const string CourseAnimationsPath = "Assets/Course Library/_Source_Files/FBX/Animations.fbx";
    private const string ClimbParameter = "Climb";
    private const string ClimbStateName = "Climb";
    private const string LocomotionStateName = "Locomotion";
    private const string FallbackClipName = "Standing_Jump";

    [MenuItem("VIRUS9/Ensure Player Climb Animator State")]
    public static void EnsureClimbState()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            throw new InvalidOperationException($"Animator controller not found at {ControllerPath}.");
        }

        AnimationClip climbClip = FindPreferredClimbClip() ?? FindClipByExactName(CourseAnimationsPath, FallbackClipName);
        if (climbClip == null)
        {
            throw new InvalidOperationException($"Neither a Climb clip nor fallback {FallbackClipName} was found.");
        }

        EnsureParameter(controller);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState locomotionState = FindState(stateMachine, LocomotionStateName);
        AnimatorState climbState = FindState(stateMachine, ClimbStateName) ?? stateMachine.AddState(ClimbStateName, new Vector3(520f, 120f, 0f));
        climbState.motion = climbClip;
        climbState.writeDefaultValues = true;

        EnsureAnyStateTransition(stateMachine, climbState);
        EnsureClimbExitTransition(climbState, locomotionState);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Ensured {ClimbStateName} state on {ControllerPath} using clip {climbClip.name}.");
    }

    public static void EnsureClimbStateBatch()
    {
        EnsureClimbState();
    }

    private static void EnsureParameter(AnimatorController controller)
    {
        if (controller.parameters.Any(parameter => parameter.name == ClimbParameter)) return;
        controller.AddParameter(ClimbParameter, AnimatorControllerParameterType.Trigger);
    }

    private static void EnsureAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState climbState)
    {
        AnimatorStateTransition transition = stateMachine.anyStateTransitions
            .FirstOrDefault(candidate =>
                candidate != null &&
                candidate.destinationState == climbState &&
                candidate.conditions.Any(condition => condition.parameter == ClimbParameter));

        if (transition == null)
        {
            transition = stateMachine.AddAnyStateTransition(climbState);
            transition.AddCondition(AnimatorConditionMode.If, 0f, ClimbParameter);
        }

        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
    }

    private static void EnsureClimbExitTransition(AnimatorState climbState, AnimatorState locomotionState)
    {
        if (locomotionState == null) return;

        AnimatorStateTransition transition = climbState.transitions
            .FirstOrDefault(candidate => candidate != null && candidate.destinationState == locomotionState);

        if (transition == null)
        {
            transition = climbState.AddTransition(locomotionState);
        }

        transition.hasExitTime = true;
        transition.exitTime = 0.88f;
        transition.hasFixedDuration = true;
        transition.duration = 0.08f;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        return stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == stateName);
    }

    private static AnimationClip FindPreferredClimbClip()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { PlayerAnimationsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    candidate.name.IndexOf("climb", StringComparison.OrdinalIgnoreCase) >= 0);
            if (clip != null) return clip;
        }

        return null;
    }

    private static AnimationClip FindClipByExactName(string assetPath, string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && clip.name == clipName);
    }
}
