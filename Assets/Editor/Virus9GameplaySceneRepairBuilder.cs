using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Virus9GameplaySceneRepairBuilder
{
    private const string ExteriorScenePath = "Assets/Scenes/Playable/LOCATION_01_EXTERIOR_DAY.unity";
    private const string NightScenePath = "Assets/Scenes/Playable/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string FinalScenePath = "Assets/Scenes/Playable/LOCATION_03_GATE_FINAL.unity";
    private const string NominatedMusicFolder = "Assets/Audio/Music/Nominated";
    private const string SourceMusicFolder = @"C:\Users\ACER\Desktop\Nominated";
    private const string CalmTrackPath = NominatedMusicFolder + "/S.mp3.mp3";
    private const string EscapeTrackPath = NominatedMusicFolder + "/World in a Cup.mp3";
    private const string NightTrackPath = NominatedMusicFolder + "/Tired of the Fire.mp3";
    private const string FinalTrackPath = NominatedMusicFolder + "/Untitled (4).mp3";
    private const string PreviewPrefabPath = "Assets/Art/Characters/Player/Prefabs/PlayerHumanoidRetargetPreview.prefab";
    private const string PlayerModelPath = "Assets/Art/Characters/Player/Models/DEAD2.fbx";
    private const string PlayerControllerPath = "Assets/Art/Characters/Player/Controllers/PlayerHumanoid.controller";

    [MenuItem("VIRUS9/Repair Gameplay Scenes")]
    public static void RepairGameplayScenes()
    {
        EnsureNominatedMusicImported();
        RepairScene(ExteriorScenePath, SceneIds.Exterior);
        RepairScene(NightScenePath, SceneIds.Night);
        RepairScene(FinalScenePath, SceneIds.Final);
        EnsureRetargetPreviewPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VIRUS9 gameplay scenes repaired: music, return gates, climb assists, colliders, night training target, and retarget preview.");
    }

    public static void RepairGameplayScenesBatch()
    {
        RepairGameplayScenes();
    }

    private static void EnsureNominatedMusicImported()
    {
        Directory.CreateDirectory(NominatedMusicFolder);
        CopyTrack("S.mp3.mp3", CalmTrackPath);
        CopyTrack("World in a Cup.mp3", EscapeTrackPath);
        CopyTrack("Tired of the Fire.mp3", NightTrackPath);
        CopyTrack("Untitled (4).mp3", FinalTrackPath);
        AssetDatabase.Refresh();
    }

    private static void CopyTrack(string sourceFileName, string destinationAssetPath)
    {
        string sourcePath = Path.Combine(SourceMusicFolder, sourceFileName);
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"Nominated music track missing: {sourcePath}");
            return;
        }

        string fullDestination = Path.GetFullPath(destinationAssetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDestination) ?? NominatedMusicFolder);
        File.Copy(sourcePath, fullDestination, true);
        AssetDatabase.ImportAsset(destinationAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RepairScene(string scenePath, string sceneId)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning($"Gameplay scene not found: {scenePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        EnsureGameplayMusic(sceneId);
        EnsureReturnGate(sceneId);
        if (sceneId == SceneIds.Night) EnsureNightTrainingTarget();
        EnsureEnvironmentColliders(sceneId);
        EnsureClimbAssists(sceneId);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureGameplayMusic(string sceneId)
    {
        GameObject root = FindOrCreateRoot("VIRUS9_GameplayMusic");
        AudioSource source = GetOrAdd<AudioSource>(root);
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        GameplayMusicController controller = GetOrAdd<GameplayMusicController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        if (sceneId == SceneIds.Exterior)
        {
            SetObject(serialized, "sceneClip", LoadClip(CalmTrackPath));
            SetObject(serialized, "exteriorCalmClip", LoadClip(CalmTrackPath));
            SetObject(serialized, "exteriorEscapeClip", LoadClip(EscapeTrackPath));
            source.clip = LoadClip(CalmTrackPath);
        }
        else if (sceneId == SceneIds.Night)
        {
            SetObject(serialized, "sceneClip", LoadClip(NightTrackPath));
            SetObject(serialized, "exteriorCalmClip", null);
            SetObject(serialized, "exteriorEscapeClip", null);
            source.clip = LoadClip(NightTrackPath);
        }
        else
        {
            SetObject(serialized, "sceneClip", LoadClip(FinalTrackPath));
            SetObject(serialized, "exteriorCalmClip", null);
            SetObject(serialized, "exteriorEscapeClip", null);
            source.clip = LoadClip(FinalTrackPath);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(root);
    }

    private static AudioClip LoadClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void EnsureReturnGate(string sceneId)
    {
        if (sceneId != SceneIds.Night && sceneId != SceneIds.Final) return;

        string gateName = sceneId == SceneIds.Night ? "VIRUS9_ReturnGate_To_Exterior" : "VIRUS9_ReturnGate_To_Night";
        string targetScene = sceneId == SceneIds.Night ? SceneIds.Exterior : SceneIds.Night;
        bool requireExterior = sceneId == SceneIds.Night;
        bool requireNight = false;

        GameObject gate = FindOrCreateRoot(gateName);
        gate.transform.position = ResolveGroundedPlacement(sceneId == SceneIds.Night ? new Vector3(-4f, 0f, -3f) : new Vector3(4f, 0f, -3f));
        gate.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        BoxCollider trigger = GetOrAdd<BoxCollider>(gate);
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.3f, 0f);
        trigger.size = new Vector3(2.6f, 2.6f, 1.4f);

        ReturnGateController controller = GetOrAdd<ReturnGateController>(gate);
        controller.Configure(targetScene, requireExterior, requireNight);
        EnsureReturnGateVisuals(gate);
        EditorUtility.SetDirty(gate);
    }

    private static void EnsureReturnGateVisuals(GameObject gate)
    {
        EnsurePrimitiveChild(gate.transform, "LeftPost", PrimitiveType.Cube, new Vector3(-0.85f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f));
        EnsurePrimitiveChild(gate.transform, "RightPost", PrimitiveType.Cube, new Vector3(0.85f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f));
        EnsurePrimitiveChild(gate.transform, "TopBar", PrimitiveType.Cube, new Vector3(0f, 2.36f, 0f), new Vector3(1.9f, 0.16f, 0.18f));
        EnsurePrimitiveChild(gate.transform, "Threshold", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(2.1f, 0.1f, 0.28f));

        Transform lightTransform = gate.transform.Find("ReturnGateLight");
        GameObject lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject("ReturnGateLight");
        lightObject.transform.SetParent(gate.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.5f, -0.25f);
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Point;
        light.range = 5.5f;
        light.shadows = LightShadows.None;
        light.color = new Color(0.85f, 0.72f, 0.32f, 1f);
    }

    private static void EnsureNightTrainingTarget()
    {
        GameObject target = FindOrCreateRoot("VIRUS9_NightSpellTrainingTarget");
        target.transform.position = ResolveGroundedPlacement(new Vector3(3f, 0f, 6f)) + Vector3.up * 0.85f;
        target.transform.rotation = Quaternion.identity;

        CapsuleCollider collider = GetOrAdd<CapsuleCollider>(target);
        collider.center = Vector3.zero;
        collider.height = 1.7f;
        collider.radius = 0.38f;

        if (target.GetComponentInChildren<Renderer>(true) == null)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "TrainingTargetVisual";
            visual.transform.SetParent(target.transform, false);
            visual.transform.localPosition = Vector3.zero;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        }

        CorruptionTrainingTarget trainingTarget = GetOrAdd<CorruptionTrainingTarget>(target);
        SerializedObject serialized = new SerializedObject(trainingTarget);
        SetObject(serialized, "feedbackLight", EnsureTrainingTargetLight(target.transform));
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static Light EnsureTrainingTargetLight(Transform parent)
    {
        Transform child = parent.Find("TrainingTargetLight");
        GameObject lightObject = child != null ? child.gameObject : new GameObject("TrainingTargetLight");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Point;
        light.range = 4f;
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;
        return light;
    }

    private static void EnsureEnvironmentColliders(string sceneId)
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!ShouldAddEnvironmentCollider(renderer)) continue;
            if (renderer.GetComponentInParent<Collider>() != null || renderer.GetComponentInChildren<Collider>() != null) continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
            }
            else
            {
                BoxCollider box = renderer.gameObject.AddComponent<BoxCollider>();
                box.center = renderer.transform.InverseTransformPoint(renderer.bounds.center);
                box.size = renderer.bounds.size;
            }

            added++;
            EditorUtility.SetDirty(renderer.gameObject);
        }

        if (added > 0) Debug.Log($"Added {added} environment colliders in {sceneId}.");
    }

    private static bool ShouldAddEnvironmentCollider(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;
        if (bounds.size.magnitude < 1.1f) return false;
        if (bounds.size.y < 0.08f && bounds.size.x * bounds.size.z < 1.5f) return false;

        string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();
        string[] blocked =
        {
            "player", "shadow", "guardian", "fragment", "portal", "gate", "vfx", "light",
            "camera", "ui", "training", "return", "assist", "hud", "canvas", "particle"
        };

        return !blocked.Any(path.Contains);
    }

    private static void EnsureClimbAssists(string sceneId)
    {
        GameObject root = FindOrCreateRoot("VIRUS9_ClimbAssists");
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        Transform player = FindPlayerTransform();
        if (player == null) return;

        Renderer[] candidates = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(ShouldConsiderClimbCandidate)
            .OrderBy(renderer => Vector3.SqrMagnitude(renderer.bounds.center - player.position))
            .Take(sceneId == SceneIds.Exterior ? 5 : 10)
            .ToArray();

        for (int i = 0; i < candidates.Length; i++)
        {
            CreateClimbAssist(root.transform, candidates[i], player, i);
        }

        EditorUtility.SetDirty(root);
    }

    private static bool ShouldConsiderClimbCandidate(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) return false;
        Bounds bounds = renderer.bounds;
        if (bounds.size.x * bounds.size.z < 1.4f) return false;
        if (bounds.size.y < 0.45f || bounds.size.y > 4.2f) return false;
        string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();
        string[] blocked = { "player", "shadow", "guardian", "fragment", "portal", "gate", "vfx", "light", "camera", "training", "return", "assist" };
        return !blocked.Any(path.Contains);
    }

    private static void CreateClimbAssist(Transform parent, Renderer renderer, Transform player, int index)
    {
        Bounds bounds = renderer.bounds;
        Vector3 towardObject = bounds.center - player.position;
        towardObject.y = 0f;
        if (towardObject.sqrMagnitude <= 0.001f) towardObject = Vector3.forward;
        towardObject.Normalize();

        float extent = Mathf.Abs(towardObject.x) > Mathf.Abs(towardObject.z) ? bounds.extents.x : bounds.extents.z;
        Vector3 outsideFace = bounds.center - towardObject * (extent + 0.45f);
        outsideFace.y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.45f);
        Vector3 landingBottom = bounds.center - towardObject * Mathf.Max(0.2f, extent - 0.65f);
        landingBottom.y = bounds.max.y + 0.04f;

        GameObject assist = new GameObject($"ClimbAssist_{index:00}_{renderer.gameObject.name}");
        assist.transform.SetParent(parent, false);
        assist.transform.position = outsideFace;
        assist.transform.rotation = Quaternion.LookRotation(towardObject, Vector3.up);

        BoxCollider trigger = assist.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(2.4f, Mathf.Clamp(bounds.size.y + 0.8f, 1.4f, 3.2f), 1.4f);
        trigger.center = new Vector3(0f, trigger.size.y * 0.15f, 0f);

        GameObject landing = new GameObject("LandingPoint");
        landing.transform.SetParent(assist.transform, false);
        landing.transform.position = landingBottom;

        ClimbAssistVolume volume = assist.AddComponent<ClimbAssistVolume>();
        volume.Configure(landing.transform, 2.4f);
    }

    private static Vector3 ResolveGroundedPlacement(Vector3 offsetFromPlayer)
    {
        Transform player = FindPlayerTransform();
        Vector3 position = player != null
            ? player.position + player.TransformDirection(offsetFromPlayer)
            : offsetFromPlayer;

        Vector3 rayOrigin = position + Vector3.up * 12f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y;
        }

        return position;
    }

    private static Transform FindPlayerTransform()
    {
        PlayerController3D player = UnityEngine.Object.FindFirstObjectByType<PlayerController3D>(FindObjectsInactive.Include);
        return player != null ? player.transform : null;
    }

    private static GameObject FindOrCreateRoot(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) return existing;
        return new GameObject(objectName);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static GameObject EnsurePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale)
    {
        Transform existing = parent.Find(childName);
        GameObject child = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitiveType);
        child.name = childName;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = localScale;
        Collider collider = child.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        return child;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null) return string.Empty;

        List<string> names = new List<string>();
        while (transform != null)
        {
            names.Add(transform.name);
            transform = transform.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static void EnsureRetargetPreviewPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        if (model == null)
        {
            Debug.LogWarning($"Retarget preview skipped; model missing at {PlayerModelPath}.");
            return;
        }

        EnsureAssetFolder("Assets/Art/Characters/Player/Prefabs");
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null) return;

        instance.name = "PlayerHumanoidRetargetPreview";
        Animator animator = GetOrAdd<Animator>(instance);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
        animator.applyRootMotion = false;

        Rigidbody body = GetOrAdd<Rigidbody>(instance);
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        CapsuleCollider capsule = GetOrAdd<CapsuleCollider>(instance);
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.35f;

        PrefabUtility.SaveAsPrefabAsset(instance, PreviewPrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void EnsureAssetFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }
}
