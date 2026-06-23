using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class Virus9GameplaySceneRepairBuilder
{
    private readonly struct VolumeComponentSnapshot
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly HideFlags HideFlags;
        public readonly string Json;

        public VolumeComponentSnapshot(VolumeComponent component)
        {
            Type = component.GetType();
            Name = component.name;
            HideFlags = component.hideFlags;
            Json = EditorJsonUtility.ToJson(component);
        }
    }

    private const string ExteriorScenePath = "Assets/Scenes/Playable/LOCATION_01_EXTERIOR_DAY.unity";
    private const string NightScenePath = "Assets/Scenes/Playable/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string FinalScenePath = "Assets/Scenes/Playable/LOCATION_03_GATE_FINAL.unity";
    private const string NominatedMusicFolder = "Assets/Audio/Music/Nominated";
    private const string SourceMusicFolder = @"C:\Users\ACER\Desktop\Nominated";
    private const string SourceDownloadMusicFolder = @"C:\Users\ACER\Downloads";
    private const string CalmTrackPath = NominatedMusicFolder + "/Dreams of Quiet Waters.mp3";
    private const string CalmFallbackTrackPath = NominatedMusicFolder + "/S.mp3.mp3";
    private const string EscapeTrackPath = NominatedMusicFolder + "/World in a Cup.mp3";
    private const string NightTrackPath = NominatedMusicFolder + "/Haunted House.mp3";
    private const string NightFallbackTrackPath = NominatedMusicFolder + "/Tired of the Fire.mp3";
    private const string FinalTrackPath = NominatedMusicFolder + "/Untitled (4).mp3";
    private const string PreviewPrefabPath = "Assets/Art/Characters/Player/Prefabs/PlayerHumanoidRetargetPreview.prefab";
    private const string PlayerModelPath = "Assets/Art/Characters/Player/Models/DEAD2.fbx";
    private const string PlayerControllerPath = "Assets/Art/Characters/Player/Controllers/PlayerHumanoid.controller";
    private const string DefaultVolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
    private const string MissingScriptMarker = "m_Script: {fileID: 0}";

    [MenuItem("VIRUS9/Repair Gameplay Scenes")]
    public static void RepairGameplayScenes()
    {
        EnsureNominatedMusicImported();
        CleanDefaultVolumeProfileMissingScripts();
        RepairScene(ExteriorScenePath, SceneIds.Exterior);
        RepairScene(NightScenePath, SceneIds.Night);
        RepairScene(FinalScenePath, SceneIds.Final);
        EnsureRetargetPreviewPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VIRUS9 gameplay scenes repaired: music, return zones, climb assists, colliders, night training target, and retarget preview.");
    }

    public static void RepairGameplayScenesBatch()
    {
        RepairGameplayScenes();
    }

    [MenuItem("VIRUS9/Repair Default Volume Profile Missing Scripts")]
    public static void CleanDefaultVolumeProfileMissingScriptsMenu()
    {
        int removed = CleanDefaultVolumeProfileMissingScripts();
        if (removed == 0) Debug.Log("Default volume profile has no missing script components to remove.");
    }

    private static int CleanDefaultVolumeProfileMissingScripts()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultVolumeProfilePath);
        if (profile == null) return 0;

        int removed = profile.components != null
            ? profile.components.RemoveAll(component => component == null)
            : 0;

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(DefaultVolumeProfilePath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            UnityEngine.Object subAsset = subAssets[i];
            if (subAsset == null || subAsset == profile) continue;

            SerializedObject serialized = new SerializedObject(subAsset);
            SerializedProperty script = serialized.FindProperty("m_Script");
            if (script == null || script.objectReferenceValue != null) continue;

            UnityEngine.Object.DestroyImmediate(subAsset, true);
            removed++;
        }

        List<long> missingLocalIds = FindMissingScriptLocalIds(DefaultVolumeProfilePath);
        string guid = AssetDatabase.AssetPathToGUID(DefaultVolumeProfilePath);
        for (int i = 0; i < missingLocalIds.Count; i++)
        {
            UnityEngine.Object missingSubAsset = TryLoadAssetByGlobalObjectId(guid, missingLocalIds[i]);
            if (missingSubAsset == null || missingSubAsset == profile) continue;

            UnityEngine.Object.DestroyImmediate(missingSubAsset, true);
            removed++;
        }

        if (removed <= 0 && missingLocalIds.Count > 0)
        {
            removed = RebuildDefaultVolumeProfileAsset(profile, missingLocalIds.Count);
            if (removed > 0) return removed;
        }

        if (removed <= 0)
        {
            if (missingLocalIds.Count > 0)
            {
                Debug.LogWarning(
                    $"{DefaultVolumeProfilePath} contains {missingLocalIds.Count} stale missing-script records, but Unity did not expose them as removable sub-assets.");
            }

            return 0;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(DefaultVolumeProfilePath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"Removed {removed} missing script component references from {DefaultVolumeProfilePath}.");
        return removed;
    }

    private static int RebuildDefaultVolumeProfileAsset(VolumeProfile profile, int staleRecordCount)
    {
        if (profile == null || profile.components == null) return 0;

        List<VolumeComponentSnapshot> snapshots = new List<VolumeComponentSnapshot>();
        for (int i = 0; i < profile.components.Count; i++)
        {
            VolumeComponent component = profile.components[i];
            if (component != null) snapshots.Add(new VolumeComponentSnapshot(component));
        }

        string profileName = profile.name;
        HideFlags profileHideFlags = profile.hideFlags;

        FileUtil.DeleteFileOrDirectory(DefaultVolumeProfilePath);

        VolumeProfile cleanProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        cleanProfile.name = profileName;
        cleanProfile.hideFlags = profileHideFlags;
        AssetDatabase.CreateAsset(cleanProfile, DefaultVolumeProfilePath);
        cleanProfile.components.Clear();

        for (int i = 0; i < snapshots.Count; i++)
        {
            VolumeComponentSnapshot snapshot = snapshots[i];
            VolumeComponent component = ScriptableObject.CreateInstance(snapshot.Type) as VolumeComponent;
            if (component == null) continue;

            EditorJsonUtility.FromJsonOverwrite(snapshot.Json, component);
            component.name = snapshot.Name;
            component.hideFlags = snapshot.HideFlags;
            AssetDatabase.AddObjectToAsset(component, cleanProfile);
            cleanProfile.components.Add(component);
        }

        EditorUtility.SetDirty(cleanProfile);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(DefaultVolumeProfilePath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"Rebuilt {DefaultVolumeProfilePath} and dropped {staleRecordCount} stale missing-script records.");
        return staleRecordCount;
    }

    private static UnityEngine.Object TryLoadAssetByGlobalObjectId(string guid, long localId)
    {
        Type globalObjectIdType = typeof(Editor).Assembly.GetType("UnityEditor.GlobalObjectId");
        if (globalObjectIdType == null) return null;

        System.Reflection.MethodInfo tryParse = globalObjectIdType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "TryParse" && method.GetParameters().Length == 2);
        System.Reflection.MethodInfo toObject = globalObjectIdType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "GlobalObjectIdentifierToObjectSlow" && method.GetParameters().Length == 1);
        if (tryParse == null || toObject == null) return null;

        for (int identifierType = 1; identifierType <= 3; identifierType++)
        {
            string candidate = $"GlobalObjectId_V1-{identifierType}-{guid}-{localId}-0";
            object[] parseArgs = { candidate, null };
            if (!(bool)tryParse.Invoke(null, parseArgs)) continue;

            UnityEngine.Object resolved = toObject.Invoke(null, new[] { parseArgs[1] }) as UnityEngine.Object;
            if (resolved != null) return resolved;
        }

        return null;
    }

    private static List<long> FindMissingScriptLocalIds(string assetPath)
    {
        List<long> localIds = new List<long>();
        if (!File.Exists(assetPath)) return localIds;

        long currentLocalId = 0;
        string[] lines = File.ReadAllLines(assetPath);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("--- !u!114 &", StringComparison.Ordinal))
            {
                string localIdText = line.Substring("--- !u!114 &".Length).Trim();
                long.TryParse(localIdText, out currentLocalId);
                continue;
            }

            if (currentLocalId != 0 && line.Contains(MissingScriptMarker))
            {
                localIds.Add(currentLocalId);
            }
        }

        return localIds;
    }

    private static void EnsureNominatedMusicImported()
    {
        Directory.CreateDirectory(NominatedMusicFolder);
        CopyTrackWithFallback(SourceDownloadMusicFolder, "Dreams of Quiet Waters.mp3", CalmTrackPath, SourceMusicFolder, "S.mp3.mp3", CalmFallbackTrackPath);
        CopyTrackWithFallback(SourceMusicFolder, "World in a Cup.mp3", EscapeTrackPath);
        CopyTrackWithFallback(SourceDownloadMusicFolder, "Haunted House.mp3", NightTrackPath, SourceMusicFolder, "Tired of the Fire.mp3", NightFallbackTrackPath);
        CopyTrackWithFallback(SourceMusicFolder, "Untitled (4).mp3", FinalTrackPath);
        AssetDatabase.Refresh();
    }

    private static void CopyTrackWithFallback(
        string primaryFolder,
        string primaryFileName,
        string destinationAssetPath,
        string fallbackFolder = null,
        string fallbackFileName = null,
        string fallbackDestinationAssetPath = null)
    {
        string sourcePath = Path.Combine(primaryFolder, primaryFileName);
        string finalDestinationAssetPath = destinationAssetPath;
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"Nominated music track missing: {sourcePath}");
            if (!string.IsNullOrWhiteSpace(fallbackFolder) && !string.IsNullOrWhiteSpace(fallbackFileName))
            {
                string fallbackPath = Path.Combine(fallbackFolder, fallbackFileName);
                if (File.Exists(fallbackPath))
                {
                    sourcePath = fallbackPath;
                    finalDestinationAssetPath = string.IsNullOrWhiteSpace(fallbackDestinationAssetPath)
                        ? destinationAssetPath
                        : fallbackDestinationAssetPath;
                    Debug.LogWarning($"Using fallback music track: {fallbackPath}");
                }
                else
                {
                    Debug.LogWarning($"Fallback music track missing: {fallbackPath}");
                    return;
                }
            }
            else
            {
                return;
            }
        }

        string fullDestination = Path.GetFullPath(finalDestinationAssetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDestination) ?? NominatedMusicFolder);
        File.Copy(sourcePath, fullDestination, true);
        AssetDatabase.ImportAsset(finalDestinationAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RepairScene(string scenePath, string sceneId)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogWarning($"Gameplay scene not found: {scenePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RemoveMissingScriptsInScene(scene);
        EnsurePlayerStability();
        EnsureGameplayMusic(sceneId);
        if (sceneId == SceneIds.Exterior) EnsureExteriorGateCutscene();
        EnsureReturnZone(sceneId);
        if (sceneId == SceneIds.Night)
        {
            EnsureNightTrainingTarget();
            EnsureNightEncounterReferences();
        }
        EnsureEnvironmentColliders(sceneId);
        EnsureClimbAssists(sceneId);
        EnsureTraversalControllers();
        if (sceneId == SceneIds.Exterior) EnsureExteriorPursuerPressure();
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
            AudioClip calmClip = LoadClip(CalmTrackPath) != null ? LoadClip(CalmTrackPath) : LoadClip(CalmFallbackTrackPath);
            SetObject(serialized, "sceneClip", calmClip);
            SetObject(serialized, "exteriorCalmClip", calmClip);
            SetObject(serialized, "exteriorEscapeClip", LoadClip(EscapeTrackPath));
            source.clip = calmClip;
        }
        else if (sceneId == SceneIds.Night)
        {
            AudioClip nightClip = LoadClip(NightTrackPath) != null ? LoadClip(NightTrackPath) : LoadClip(NightFallbackTrackPath);
            SetObject(serialized, "sceneClip", nightClip);
            SetObject(serialized, "exteriorCalmClip", null);
            SetObject(serialized, "exteriorEscapeClip", null);
            source.clip = nightClip;
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

    private static void EnsurePlayerStability()
    {
        foreach (PlayerHealthController playerHealth in UnityEngine.Object.FindObjectsByType<PlayerHealthController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (playerHealth == null) continue;

            SerializedObject healthSerialized = new SerializedObject(playerHealth);
            SerializedProperty maxHealth = healthSerialized.FindProperty("maxHealth");
            if (maxHealth != null) maxHealth.intValue = 5;
            healthSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerHealth);

            CombatantHealth combatantHealth = playerHealth.GetComponent<CombatantHealth>();
            if (combatantHealth == null) continue;

            SerializedObject combatantSerialized = new SerializedObject(combatantHealth);
            SerializedProperty combatantMaxHealth = combatantSerialized.FindProperty("maxHealth");
            if (combatantMaxHealth != null) combatantMaxHealth.intValue = 5;
            combatantSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combatantHealth);
        }
    }

    private static void EnsureReturnZone(string sceneId)
    {
        if (sceneId != SceneIds.Night && sceneId != SceneIds.Final) return;

        string oldGateName = sceneId == SceneIds.Night ? "VIRUS9_ReturnGate_To_Exterior" : "VIRUS9_ReturnGate_To_Night";
        string zoneName = sceneId == SceneIds.Night ? "VIRUS9_ReturnZone_To_Exterior" : "VIRUS9_ReturnZone_To_Night";
        string targetScene = sceneId == SceneIds.Night ? SceneIds.Exterior : SceneIds.Night;
        bool requireExterior = sceneId == SceneIds.Night;
        bool requireNight = false;

        GameObject zone = FindSceneGameObject(zoneName);
        GameObject oldGate = FindSceneGameObject(oldGateName);
        if (zone == null && oldGate != null)
        {
            zone = oldGate;
            zone.name = zoneName;
        }
        else if (oldGate != null && oldGate != zone)
        {
            UnityEngine.Object.DestroyImmediate(oldGate);
        }

        if (zone == null)
        {
            zone = new GameObject(zoneName);
            zone.transform.position = ResolveGroundedPlacement(sceneId == SceneIds.Night ? new Vector3(-4f, 0f, -3f) : new Vector3(4f, 0f, -3f));
            zone.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        CleanupReturnZoneVisuals(zone);

        BoxCollider trigger = GetOrAdd<BoxCollider>(zone);
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.65f, 0f);
        trigger.size = new Vector3(6.5f, 3.3f, 4.0f);

        ReturnGateController controller = GetOrAdd<ReturnGateController>(zone);
        controller.Configure(targetScene, requireExterior, requireNight);

        CorruptionGateHitReceiver receiver = zone.GetComponent<CorruptionGateHitReceiver>();
        if (receiver != null) UnityEngine.Object.DestroyImmediate(receiver);

        SerializedObject serialized = new SerializedObject(controller);
        SetBool(serialized, "triggerOnPlayerEnter", false);
        SetObject(serialized, "gateLight", null);
        SetArraySize(serialized, "renderers", 0);
        SetFloat(serialized, "triggerTransitionDelay", 0.55f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Configured return trigger zone {zone.name} -> {targetScene} size {trigger.size}.");
        EditorUtility.SetDirty(zone);
    }

    private static void EnsureExteriorGateCutscene()
    {
        LocationTransition transition = ResolveTransitionToScene(SceneIds.Night);
        if (transition == null) return;

        GameObject gateRoot = transition.gameObject;
        BoxCollider trigger = GetOrAdd<BoxCollider>(gateRoot);
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.55f, 0f);
        trigger.size = new Vector3(4.8f, 3.1f, 3.0f);

        ExteriorGateCutsceneController cutscene = GetOrAdd<ExteriorGateCutsceneController>(gateRoot);
        SquarePortalController portal = transition.Portal != null
            ? transition.Portal
            : gateRoot.GetComponentInChildren<SquarePortalController>(true);
        ExteriorHuntController hunt = UnityEngine.Object.FindFirstObjectByType<ExteriorHuntController>(FindObjectsInactive.Include);
        cutscene.Configure(transition, portal, hunt);

        SerializedObject serialized = new SerializedObject(cutscene);
        SetObject(serialized, "transition", transition);
        SetObject(serialized, "portal", portal);
        SetObject(serialized, "hunt", hunt);
        SetObject(serialized, "playerGateLight", null);
        SetObject(serialized, "playerGateDirectionLight", null);
        SetObject(serialized, "gateGlow", EnsureExteriorGateGlow(portal != null ? portal.transform : gateRoot.transform));
        SetFloat(serialized, "revealDistance", 16f);
        SetFloat(serialized, "guideDistance", 140f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (portal != null)
        {
            EnsureExteriorPortalTriggerVisibility(portal);
            EditorUtility.SetDirty(portal.gameObject);
        }

        EditorUtility.SetDirty(gateRoot);
    }

    private static Light EnsureExteriorGateGlow(Transform parent)
    {
        Transform lightTransform = parent.Find("ExteriorGateCutsceneGlow");
        GameObject lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject("ExteriorGateCutsceneGlow");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.2f, -0.4f);
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Point;
        light.range = 9f;
        light.shadows = LightShadows.None;
        light.color = new Color(1f, 0.42f, 0.22f, 1f);
        light.enabled = false;
        EditorUtility.SetDirty(lightObject);
        return light;
    }

    private static void EnsureExteriorPortalTriggerVisibility(SquarePortalController portal)
    {
        foreach (Collider collider in portal.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            if (collider is BoxCollider box && box.isTrigger)
            {
                box.size = new Vector3(Mathf.Max(box.size.x, 4.6f), Mathf.Max(box.size.y, 3f), Mathf.Max(box.size.z, 2.5f));
                box.center = new Vector3(box.center.x, Mathf.Max(box.center.y, 1.5f), box.center.z);
                EditorUtility.SetDirty(box);
            }
        }
    }

    private static void EnsureReturnGateVisuals(GameObject gate)
    {
        EnsurePrimitiveChild(gate.transform, "LeftPost", PrimitiveType.Cube, new Vector3(-1.95f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 0.22f));
        EnsurePrimitiveChild(gate.transform, "RightPost", PrimitiveType.Cube, new Vector3(1.95f, 1.45f, 0f), new Vector3(0.22f, 2.9f, 0.22f));
        EnsurePrimitiveChild(gate.transform, "TopBar", PrimitiveType.Cube, new Vector3(0f, 2.84f, 0f), new Vector3(4.12f, 0.18f, 0.24f));
        EnsurePrimitiveChild(gate.transform, "Threshold", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(4.35f, 0.1f, 0.36f));
        EnsurePrimitiveChild(gate.transform, "BackLine", PrimitiveType.Cube, new Vector3(0f, 1.45f, 0.16f), new Vector3(3.65f, 0.08f, 0.08f));

        Transform lightTransform = gate.transform.Find("ReturnGateLight");
        GameObject lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject("ReturnGateLight");
        lightObject.transform.SetParent(gate.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.7f, -0.25f);
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Point;
        light.range = 7.2f;
        light.shadows = LightShadows.None;
        light.color = new Color(0.85f, 0.72f, 0.32f, 1f);
    }

    private static void EnsureReturnGateLight(Transform gate)
    {
        Transform lightTransform = gate.Find("ReturnGateLight");
        GameObject lightObject = lightTransform != null ? lightTransform.gameObject : new GameObject("ReturnGateLight");
        lightObject.transform.SetParent(gate, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.7f, -0.25f);
        Light light = GetOrAdd<Light>(lightObject);
        light.type = LightType.Point;
        light.range = 7.2f;
        light.shadows = LightShadows.None;
        light.color = new Color(0.85f, 0.72f, 0.32f, 1f);
        EditorUtility.SetDirty(lightObject);
    }

    private static void CleanupReturnZoneVisuals(GameObject zone)
    {
        foreach (CorruptionGateHitReceiver receiver in zone.GetComponentsInChildren<CorruptionGateHitReceiver>(true))
        {
            if (receiver != null) UnityEngine.Object.DestroyImmediate(receiver);
        }

        string[] generatedChildren =
        {
            "LeftPost",
            "RightPost",
            "TopBar",
            "Threshold",
            "BackLine",
            "ReturnGateLight"
        };

        for (int i = 0; i < generatedChildren.Length; i++)
        {
            Transform child = zone.transform.Find(generatedChildren[i]);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        Renderer[] renderers = zone.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject == zone) continue;
            if (renderer.transform.parent == zone.transform)
            {
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            }
        }

        Light[] lights = zone.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light != null && light.transform.parent == zone.transform)
            {
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
        }
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

    private static void EnsureNightEncounterReferences()
    {
        NightFragmentEncounter encounter = UnityEngine.Object.FindFirstObjectByType<NightFragmentEncounter>(FindObjectsInactive.Include);
        if (encounter == null)
        {
            Debug.LogWarning("NightFragmentEncounter not found in night scene.");
            return;
        }

        LightFragmentPickup fragment = FindNightFragmentPickup();
        Transform dropPoint = EnsureNightFragmentDropPoint();
        LocationTransition finalTransition = ResolveTransitionToScene(SceneIds.Final);
        SquarePortalController finalPortal = finalTransition != null ? finalTransition.Portal : null;
        CorruptionTrainingTarget[] targets = UnityEngine.Object.FindObjectsByType<CorruptionTrainingTarget>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        SerializedObject serialized = new SerializedObject(encounter);
        SetObject(serialized, "innerNightFragment", fragment);
        SetObject(serialized, "mercyDropPoint", dropPoint);
        SetObject(serialized, "exitPortal", finalPortal);
        SetObjectArray(serialized, "trainingTargets", targets);
        SetBool(serialized, "waitForTrainingBeforeAggro", true);
        SetFloat(serialized, "initialAggroFallbackDelay", 30f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (fragment != null)
        {
            fragment.Configure(LightFragmentPickup.FragmentKind.InnerNight);
            EditorUtility.SetDirty(fragment);
        }

        EditorUtility.SetDirty(encounter);
    }

    private static LightFragmentPickup FindNightFragmentPickup()
    {
        LightFragmentPickup[] fragments = UnityEngine.Object.FindObjectsByType<LightFragmentPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < fragments.Length; i++)
        {
            LightFragmentPickup fragment = fragments[i];
            if (fragment != null && fragment.Kind == LightFragmentPickup.FragmentKind.InnerNight) return fragment;
        }

        for (int i = 0; i < fragments.Length; i++)
        {
            LightFragmentPickup fragment = fragments[i];
            if (fragment != null) return fragment;
        }

        return null;
    }

    private static Transform EnsureNightFragmentDropPoint()
    {
        GameObject drop = FindOrCreateRoot("VIRUS9_NightFragmentDropPoint");
        drop.transform.position = ResolveGroundedPlacement(new Vector3(1.5f, 0f, 2.6f)) + Vector3.up * 0.35f;
        drop.transform.rotation = Quaternion.identity;
        EditorUtility.SetDirty(drop);
        return drop.transform;
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
            if (HasSolidColliderOnSelf(renderer)) continue;

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
                box.size = WorldSizeToLocalSize(renderer.transform, renderer.bounds.size);
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

    private static bool HasSolidColliderOnSelf(Renderer renderer)
    {
        Collider[] colliders = renderer.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider != null && !collider.isTrigger) return true;
        }

        return false;
    }

    private static Vector3 WorldSizeToLocalSize(Transform transform, Vector3 worldSize)
    {
        Vector3 scale = transform.lossyScale;
        return new Vector3(
            SafeDivide(worldSize.x, scale.x),
            SafeDivide(worldSize.y, scale.y),
            SafeDivide(worldSize.z, scale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        divisor = Mathf.Abs(divisor);
        return divisor <= 0.0001f ? value : value / divisor;
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
            .Take(sceneId == SceneIds.Exterior ? 16 : 28)
            .ToArray();

        for (int i = 0; i < candidates.Length; i++)
        {
            CreateClimbAssist(root.transform, candidates[i], player, i);
        }

        EditorUtility.SetDirty(root);
    }

    private static void EnsureTraversalControllers()
    {
        int added = 0;
        added += EnsureTraversalControllersFor(UnityEngine.Object.FindObjectsByType<ExteriorPursuer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        added += EnsureTraversalControllersFor(UnityEngine.Object.FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        added += EnsureTraversalControllersFor(UnityEngine.Object.FindObjectsByType<GuardianController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        if (added > 0) Debug.Log($"Added {added} traversal controllers.");
    }

    private static int EnsureTraversalControllersFor<T>(T[] components) where T : Component
    {
        int added = 0;
        foreach (T component in components)
        {
            if (component == null) continue;
            EnemyJumpController traversal = component.GetComponent<EnemyJumpController>();
            if (traversal != null)
            {
                ConfigureTraversalController(traversal);
                continue;
            }

            traversal = component.gameObject.AddComponent<EnemyJumpController>();
            ConfigureTraversalController(traversal);
            EditorUtility.SetDirty(component.gameObject);
            added++;
        }

        return added;
    }

    private static void ConfigureTraversalController(EnemyJumpController traversal)
    {
        if (traversal == null) return;

        SerializedObject serialized = new SerializedObject(traversal);
        SetFloat(serialized, "dropDistance", 6.5f);
        SetFloat(serialized, "dropLandingSampleRadius", 4.5f);
        SetFloat(serialized, "dropCooldown", 0.65f);
        SetFloat(serialized, "vaultDistance", 2.9f);
        SetFloat(serialized, "vaultLandingSampleRadius", 2.4f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(traversal);
    }

    private static void EnsureExteriorPursuerPressure()
    {
        foreach (ExteriorPursuer pursuer in UnityEngine.Object.FindObjectsByType<ExteriorPursuer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pursuer == null) continue;

            SerializedObject serialized = new SerializedObject(pursuer);
            SetFloat(serialized, "chaseSpeed", 5.05f);
            SetFloat(serialized, "catchDistance", 1.42f);
            SetFloat(serialized, "maxCatchHeightDifference", 2.25f);
            SetFloat(serialized, "destinationRefreshInterval", 0.14f);
            SetFloat(serialized, "stuckRepathAfter", 0.35f);
            SetFloat(serialized, "stuckSideStepDistance", 2.25f);
            SetFloat(serialized, "personalSpaceRadius", 1.15f);
            SetFloat(serialized, "personalSpaceWeight", 0.8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pursuer);
        }
    }

    private static bool ShouldConsiderClimbCandidate(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) return false;
        Bounds bounds = renderer.bounds;
        if (bounds.size.x * bounds.size.z < 1.4f) return false;
        if (bounds.size.y < 0.45f || bounds.size.y > 4.6f) return false;
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
        Vector3 landingBottom = bounds.center - towardObject * Mathf.Max(0.2f, extent - 0.95f);
        landingBottom.y = bounds.max.y + 0.04f;

        GameObject assist = new GameObject($"ClimbAssist_{index:00}_{renderer.gameObject.name}");
        assist.transform.SetParent(parent, false);
        assist.transform.position = outsideFace;
        assist.transform.rotation = Quaternion.LookRotation(towardObject, Vector3.up);

        BoxCollider trigger = assist.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(3.4f, Mathf.Clamp(bounds.size.y + 1.1f, 1.6f, 3.6f), 2.0f);
        trigger.center = new Vector3(0f, trigger.size.y * 0.15f, 0f);

        GameObject landing = new GameObject("LandingPoint");
        landing.transform.SetParent(assist.transform, false);
        landing.transform.position = landingBottom;

        ClimbAssistVolume volume = assist.AddComponent<ClimbAssistVolume>();
        volume.Configure(landing.transform, 3.4f);
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
        GameObject existing = FindSceneGameObject(objectName);
        if (existing != null) return existing;
        return new GameObject(objectName);
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.name == objectName) return candidate;
        }

        return null;
    }

    private static GameObject ResolveExistingGateRoot(string targetScene)
    {
        LocationTransition transition = ResolveTransitionToScene(targetScene);
        if (transition != null) return transition.Portal != null ? transition.Portal.gameObject : transition.gameObject;

        string[] nameHints = targetScene == SceneIds.Exterior
            ? new[] { "return", "exterior", "back", "gate" }
            : new[] { "return", "night", "back", "gate" };
        GameObject best = null;
        int bestScore = 0;
        foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null) continue;
            string lower = candidate.name.ToLowerInvariant();
            int score = 0;
            for (int i = 0; i < nameHints.Length; i++)
            {
                if (lower.Contains(nameHints[i])) score++;
            }

            if (score <= bestScore) continue;
            if (candidate.GetComponentInChildren<Renderer>(true) == null && candidate.GetComponentInChildren<Light>(true) == null) continue;
            best = candidate;
            bestScore = score;
        }

        return bestScore >= 2 ? best : null;
    }

    private static LocationTransition ResolveTransitionToScene(string targetScene)
    {
        LocationTransition[] transitions = UnityEngine.Object.FindObjectsByType<LocationTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transitions.Length; i++)
        {
            LocationTransition transition = transitions[i];
            if (transition == null) continue;

            SerializedObject serialized = new SerializedObject(transition);
            SerializedProperty sceneProperty = serialized.FindProperty("targetScene");
            if (sceneProperty == null || sceneProperty.stringValue != targetScene) continue;
            return transition;
        }

        return null;
    }

    private static void RemoveGeneratedGateRoot(string generatedName, GameObject selectedGate)
    {
        GameObject generated = FindSceneGameObject(generatedName);
        if (generated == null || generated == selectedGate) return;
        UnityEngine.Object.DestroyImmediate(generated);
    }

    private static void RemoveMissingScriptsInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        int removed = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            removed += RemoveMissingScriptsRecursive(roots[i]);
        }

        if (removed > 0) Debug.Log($"Removed {removed} missing script references in {scene.name}.");
    }

    private static int RemoveMissingScriptsRecursive(GameObject gameObject)
    {
        if (gameObject == null) return 0;

        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            removed += RemoveMissingScriptsRecursive(gameObject.transform.GetChild(i).gameObject);
        }

        if (removed > 0) EditorUtility.SetDirty(gameObject);
        return removed;
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

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.floatValue = value;
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
    }

    private static void SetArraySize(SerializedObject serialized, string propertyName, int size)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null && property.isArray) property.arraySize = Mathf.Max(0, size);
    }

    private static void SetObjectArray<T>(SerializedObject serialized, string propertyName, IReadOnlyList<T> values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray) return;

        int count = values != null ? values.Count : 0;
        property.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
