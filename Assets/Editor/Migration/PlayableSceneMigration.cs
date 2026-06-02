using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PlayableSceneMigration
{
    private const string LegacyExteriorScene = "Assets/Scenes/LOCATION_01_EXTERIOR_DAY.unity";
    private const string LegacyNightPlayableScene = "Assets/Scenes/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string LegacyFinalScene = "Assets/Scenes/LOCATION_03_GATE_FINAL.unity";
    private const string ExteriorScene = "Assets/Scenes/Playable/LOCATION_01_EXTERIOR_DAY.unity";
    private const string ExteriorArchiveScene = "Assets/Scenes/Archive/LOCATION_01_EXTERIOR_DAY_ARCHIVE.unity";
    private const string LegacyNightScene = "Assets/Scenes/LOCATION_02_INNER_NIGHT_SQUARE.unity";
    private const string SourceNightScene = "Assets/Scenes/LOcate2.unity";
    private const string NightScene = "Assets/Scenes/Playable/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string FinalScene = "Assets/Scenes/Playable/LOCATION_03_GATE_FINAL.unity";
    private const string SourceExteriorEnvironment = "Assets/Scenes/DAYMINICITY.fbx";
    private const string ExteriorArtFolder = "Assets/Art/Locations/Location01_DayMiniCity";
    private const string LegacyExteriorEnvironment = ExteriorArtFolder + "/DAYMINICITY.fbx";
    private const string ExteriorEnvironment = ExteriorArtFolder + "/Models/DAYMINICITY.fbx";
    private const string SourceEnvironment = "Assets/Scenes/Virus9_OldTown_ProtectedAlleys_Blockout2_before_origin_to_geometry_20260526_162356.fbx";
    private const string NightArtFolder = "Assets/Art/Locations/Location02_ProtectedAlleysNight";
    private const string LegacyNightEnvironment = NightArtFolder + "/Location02_ProtectedAlleysNight.fbx";
    private const string NightEnvironment = NightArtFolder + "/Models/Location02_ProtectedAlleysNight.fbx";
    private const string PlayerFolder = "Assets/Art/Characters/Player";
    private const string LegacyPlayerFbx = PlayerFolder + "/DEAD2.fbx";
    private const string LegacyPlayerStaticAnimationsFbx = PlayerFolder + "/Animations_Static.fbx";
    private const string LegacyPlayerController = PlayerFolder + "/PlayerHumanoid.controller";
    private const string PlayerFbx = PlayerFolder + "/Models/DEAD2.fbx";
    private const string PlayerStaticAnimationsFbx = PlayerFolder + "/Animations/Animations_Static.fbx";
    private const string CourseAnimationsFbx = "Assets/Course Library/_Source_Files/FBX/Animations.fbx";
    private const string PlayerController = PlayerFolder + "/Controllers/PlayerHumanoid.controller";
    private const string MoveSpeedParameter = "MoveSpeed";
    private const string GroundedParameter = "Grounded";
    private const string JumpParameter = "Jump";
    private const string RunningJumpParameter = "RunningJump";
    private const string AttackParameter = "Attack";
    private const string InputActionsAsset = "Assets/InputSystem_Actions.inputactions";
    private static readonly Vector3 SharedCameraOffset = new Vector3(0f, 5.8f, -8.5f);
    private const float SharedCameraLookAtHeight = 1.1f;
    private const float SharedCameraFieldOfView = 52f;
    private const float SharedPlayerMoveSpeed = 5f;
    private const float SharedPlayerRotationSpeed = 10f;
    private const bool SharedPlayerJumpEnabled = true;
    private static readonly string[] FinalGateVisualParts =
    {
        "Atmos_Door_Left",
        "Atmos_Door_Right",
        "Atmos_Door_Slit",
        "Atmos_Frame_Left",
        "Atmos_Frame_Right",
        "Atmos_Frame_Top",
        "Gate_Left_Pier",
        "Gate_Right_Pier",
        "Atmos_Relief_Left",
        "Atmos_Relief_Right"
    };

    [MenuItem("Tools/Virus 9/Apply Playable Scene Migration")]
    public static void Apply()
    {
        EnsureFolders();
        MoveAssetIfNeeded(LegacyExteriorScene, ExteriorScene);
        MoveAssetIfNeeded(LegacyNightPlayableScene, NightScene);
        MoveAssetIfNeeded(LegacyFinalScene, FinalScene);
        MoveAssetIfNeeded(SourceExteriorEnvironment, ExteriorEnvironment);
        MoveAssetIfNeeded(LegacyExteriorEnvironment, ExteriorEnvironment);
        MoveAssetIfNeeded(SourceNightScene, NightScene);
        MoveAssetIfNeeded(SourceEnvironment, NightEnvironment);
        MoveAssetIfNeeded(LegacyNightEnvironment, NightEnvironment);
        MoveAssetIfNeeded(LegacyPlayerFbx, PlayerFbx);
        MoveAssetIfNeeded(LegacyPlayerStaticAnimationsFbx, PlayerStaticAnimationsFbx);
        MoveAssetIfNeeded(LegacyPlayerController, PlayerController);
        AssetDatabase.Refresh();
        EnsureExteriorEnvironmentImportSettings();
        EnsurePauseInputAction();

        RuntimeAnimatorController animatorController = EnsureAnimatorController();
        GameObject playerVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
        if (playerVisual == null) throw new InvalidOperationException("Player FBX could not be loaded after migration.");

        ConfigureExteriorScene(playerVisual, animatorController);
        ConfigureNightScene(playerVisual, animatorController);
        ConfigureFinalScene(playerVisual, animatorController);
        DeleteUnusedPlayerSetupAssets();
        DeleteLegacyNightAssetsWhenUnused();
        UpdateBuildRoute();

        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.Refresh();
        Debug.Log("Playable scene migration applied: exterior chase, protected alleys route, final gate outcome, and animated player visual.");
    }

    [MenuItem("Tools/Virus 9/Refresh Exterior Day Mini City")]
    public static void RefreshExteriorDayMiniCity()
    {
        EnsureFolders();
        MoveAssetIfNeeded(SourceExteriorEnvironment, ExteriorEnvironment);
        MoveAssetIfNeeded(LegacyExteriorEnvironment, ExteriorEnvironment);
        AssetDatabase.Refresh();
        EnsureExteriorEnvironmentImportSettings();

        RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
        if (animatorController == null) animatorController = EnsureAnimatorController();
        GameObject playerVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
        if (playerVisual == null) throw new InvalidOperationException("Player FBX could not be loaded while refreshing exterior scene.");

        ConfigureExteriorScene(playerVisual, animatorController);
        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.Refresh();
        Debug.Log("Exterior day mini city refreshed: clean import, preview camera, gameplay camera, daylight, and selective colliders.");
    }

    public static void ApplyFromCommandLine()
    {
        Apply();
        Validate();
    }

    [MenuItem("Tools/Virus 9/Validate Playable Scenes")]
    public static void Validate()
    {
        List<string> issues = new List<string>();

        string[] route = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        string[] expected = { ExteriorScene, NightScene, FinalScene };
        if (!route.SequenceEqual(expected))
        {
            issues.Add("Build Settings route is not exterior -> protected alleys -> final gate.");
        }

        ValidateScene(ExteriorScene, issues, "Player_Fragment", "LIGHT_Fragment_01", "BUILDING_LivingSquare_Entrance", "ExteriorHunt");
        ValidateScene(NightScene, issues, "Player_Fragment", "FRAGMENT_InnerNight", "EXIT_To_FinalGate_Exit", "MERCY_WITNESS_Trigger");
        ValidateScene(FinalScene, issues, "Player_Fragment", "GATE_FinalEntryTrigger", "FinalEvaluator", "GUARDIAN_Force", "GUARDIAN_Memory");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyNightScene) != null)
        {
            issues.Add("Legacy night scene still exists after migration.");
        }

        ValidateAnimatorController(issues);

        if (issues.Count > 0)
        {
            throw new InvalidOperationException("Playable scene validation failed:\n- " + string.Join("\n- ", issues));
        }

        Debug.Log("Playable scene validation passed.");
    }

    private static void ConfigureExteriorScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(ExteriorScene, OpenSceneMode.Single);
        EnsureExteriorNarrativeRoots(scene);
        GameObject environment = EnsureExteriorEnvironment();
        EnsureExteriorLighting();
        GameObject player = EnsurePlayer(new Vector3(-1.7f, 1f, -20f), false, visualPrefab, animatorController);
        EnsureCamera(player.transform, SharedCameraOffset, SharedCameraLookAtHeight, SharedCameraFieldOfView);
        EnsureFloor("FloorCollider", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f));
        EnsureExteriorEnvironmentColliders(environment);

        LightFragmentPickup fragment = RequireComponent<LightFragmentPickup>("LIGHT_Fragment_01");
        fragment.transform.position = new Vector3(-3f, 0.5f, 2f);
        fragment.Configure(LightFragmentPickup.FragmentKind.Exterior);
        EnsureFragmentVisual(fragment, new Color(1f, 0.72f, 0.22f), 2.1f);

        LocationTransition transition = EnsureComponent<LocationTransition>(
            EnsureMarker("BUILDING_LivingSquare_Entrance", new Vector3(15.7f, 1.2f, 25f)));
        transition.Configure("LOCATION_02_PROTECTED_ALLEYS_NIGHT", true, true, false);
        EnsureTrigger(transition.gameObject, new Vector3(3.5f, 3f, 5f));
        SquarePortalController portal = EnsureExteriorPortal();
        transition.ConfigurePortal(portal);

        GameObject respawn = EnsureMarker("ExteriorRespawn", new Vector3(-1.7f, 1f, -20f));
        ExteriorHuntController hunt = EnsureComponent<ExteriorHuntController>(EnsureMarker("ExteriorHunt", Vector3.zero));
        SetReference(hunt, "player", player.GetComponent<PlayerController3D>());
        SetReference(hunt, "playerAttack", player.GetComponent<PlayerAttackController>());
        SetReference(hunt, "respawnPoint", respawn.transform);
        SetReference(hunt, "exteriorFragment", fragment);

        EnsurePursuer("SHADOW_Queue_01");
        EnsurePursuer("SHADOW_Witness_01");
        RequireObject("SHADOW_Queue_01").transform.position = new Vector3(-2f, 0.5f, 8f);
        RequireObject("SHADOW_Witness_01").transform.position = new Vector3(2f, 0.5f, 12f);
        SnapActorsToFloor<ExteriorPursuer>();
        EnsureNavigationVolume("ExteriorNavigation", new Vector3(0f, 3f, 0f), new Vector3(60f, 8f, 60f));
        SnapActorsToNavigation<ExteriorPursuer>(3f);
        OrganizeSceneHierarchy(environment);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureExteriorNarrativeRoots(Scene exteriorScene)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExteriorArchiveScene) == null) return;

        Scene archiveScene = EditorSceneManager.OpenScene(ExteriorArchiveScene, OpenSceneMode.Additive);
        try
        {
            foreach (string objectName in new[] { "LIGHT_Fragment_01", "SHADOW_Queue_01", "SHADOW_Witness_01" })
            {
                if (FindObject(objectName) != null) continue;

                GameObject source = FindObjectInScene(archiveScene, objectName);
                if (source == null) throw new InvalidOperationException("Archive exterior scene misses " + objectName + ".");
                GameObject clone = UnityEngine.Object.Instantiate(source);
                clone.name = objectName;
                SceneManager.MoveGameObjectToScene(clone, exteriorScene);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(archiveScene, true);
        }
    }

    private static GameObject EnsureExteriorEnvironment()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExteriorEnvironment);
        if (prefab == null) throw new InvalidOperationException("Exterior FBX could not be loaded: " + ExteriorEnvironment);

        Scene scene = SceneManager.GetActiveScene();
        GameObject environment = FindObject("Location01_DayMiniCity") ?? FindObject("DAYMINICITY");
        if (environment != null &&
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(environment) != ExteriorEnvironment)
        {
            environment = null;
        }

        if (environment == null)
        {
            environment = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        foreach (GameObject candidate in scene.GetRootGameObjects()
                     .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                     .Select(transform => transform.gameObject)
                     .ToArray())
        {
            if (candidate == environment) continue;
            if (candidate.name == "Location01_DayMiniCity" || candidate.name == "DAYMINICITY")
            {
                UnityEngine.Object.DestroyImmediate(candidate);
            }
        }

        environment.name = "Location01_DayMiniCity";
        environment.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        environment.transform.localScale = Vector3.one;
        return environment;
    }

    private static void EnsureExteriorEnvironmentImportSettings()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ExteriorEnvironment) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Exterior model importer not found: " + ExteriorEnvironment);
        if (importer.addCollider && !importer.importCameras && !importer.importLights) return;

        importer.addCollider = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.SaveAndReimport();
    }

    private static void EnsureExteriorLighting()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Light daylight = lights.FirstOrDefault(light => light.type == LightType.Directional && light.name == "Directional Light")
            ?? lights.FirstOrDefault(light => light.type == LightType.Directional);
        if (daylight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            daylight = lightObject.AddComponent<Light>();
        }

        foreach (Light light in lights)
        {
            if (light != daylight) light.enabled = false;
        }

        daylight.gameObject.name = "Directional Light";
        daylight.type = LightType.Directional;
        daylight.color = new Color(0.92f, 0.96f, 1f);
        daylight.intensity = 0.9f;
        daylight.shadows = LightShadows.Soft;
        daylight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.75f;
        RenderSettings.reflectionIntensity = 0.6f;
    }

    private static SquarePortalController EnsureExteriorPortal()
    {
        Scene exteriorScene = SceneManager.GetActiveScene();
        GameObject portalObject = EnsureMarker("ExteriorSquarePortal", new Vector3(17.2f, 0f, 25f));
        portalObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        portalObject.transform.localScale = Vector3.one * 0.55f;

        CloneFinalGateVisuals(exteriorScene, portalObject.transform);
        bool hasClonedGateVisuals = HasClonedGateVisuals(portalObject.transform);
        if (hasClonedGateVisuals)
        {
            AlignClonedGateVisuals(portalObject.transform);
            DestroyPortalFrameFallback(portalObject.transform);
        }
        else
        {
            EnsurePortalFrameFallback(portalObject.transform, 7.6f, 5.5f);
        }

        Transform leftDoor = FindDescendant(portalObject.transform, "Atmos_Door_Left")
            ?? EnsurePortalVisualCube(portalObject.transform, "Atmos_Door_Left", new Vector3(-1.7f, 2.2f, 0f), new Vector3(3.2f, 4.4f, 0.55f));
        Transform rightDoor = FindDescendant(portalObject.transform, "Atmos_Door_Right")
            ?? EnsurePortalVisualCube(portalObject.transform, "Atmos_Door_Right", new Vector3(1.7f, 2.2f, 0f), new Vector3(3.2f, 4.4f, 0.55f));

        BoxCollider blocker = EnsurePortalBlocker(portalObject.transform, new Vector3(0f, 2.2f, 0f), new Vector3(7.1f, 4.6f, 0.9f));
        SquarePortalController portal = EnsureComponent<SquarePortalController>(portalObject);
        portal.Configure(LightFragmentPickup.FragmentKind.Exterior, true, false, leftDoor, rightDoor, blocker, 2.5f);
        return portal;
    }

    private static SquarePortalController EnsureNightPortal(GameObject transitionObject)
    {
        Renderer transitionRenderer = transitionObject.GetComponent<Renderer>();
        if (transitionRenderer != null) transitionRenderer.enabled = false;

        GameObject portalObject = EnsureMarker("NightSquarePortal", transitionObject.transform.position);
        portalObject.transform.SetPositionAndRotation(transitionObject.transform.position, Quaternion.identity);
        Transform leftDoor = EnsurePortalVisualCube(portalObject.transform, "NightDoor_Left", new Vector3(-0.82f, 1.35f, 0f), new Vector3(1.55f, 2.7f, 0.35f));
        Transform rightDoor = EnsurePortalVisualCube(portalObject.transform, "NightDoor_Right", new Vector3(0.82f, 1.35f, 0f), new Vector3(1.55f, 2.7f, 0.35f));
        EnsurePortalFrameFallback(portalObject.transform, 4.4f, 3.7f);
        ApplyPortalGlow(portalObject, new Color(0.18f, 0.72f, 1f));
        BoxCollider blocker = EnsurePortalBlocker(portalObject.transform, new Vector3(0f, 1.35f, 0f), new Vector3(3.4f, 2.8f, 0.65f));

        SquarePortalController portal = EnsureComponent<SquarePortalController>(portalObject);
        portal.Configure(LightFragmentPickup.FragmentKind.InnerNight, true, false, leftDoor, rightDoor, blocker, 1.2f);
        return portal;
    }

    private static void CloneFinalGateVisuals(Scene targetScene, Transform portalRoot)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FinalScene) == null) return;

        Scene sourceScene = EditorSceneManager.OpenScene(FinalScene, OpenSceneMode.Additive);
        try
        {
            foreach (string partName in FinalGateVisualParts)
            {
                GameObject source = FindObjectInScene(sourceScene, partName);
                if (source == null || FindDescendant(portalRoot, partName) != null) continue;

                GameObject clone = UnityEngine.Object.Instantiate(source);
                clone.name = partName;
                SceneManager.MoveGameObjectToScene(clone, targetScene);
                clone.transform.SetParent(portalRoot, false);
                clone.transform.localPosition = source.transform.localPosition;
                clone.transform.localRotation = source.transform.localRotation;
                clone.transform.localScale = source.transform.localScale;
                foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(sourceScene, true);
        }
    }

    private static bool HasClonedGateVisuals(Transform portalRoot)
    {
        return FinalGateVisualParts.Any(partName => FindDescendant(portalRoot, partName) != null);
    }

    private static void AlignClonedGateVisuals(Transform portalRoot)
    {
        Transform[] parts = FinalGateVisualParts
            .Select(partName => FindDescendant(portalRoot, partName))
            .Where(part => part != null)
            .ToArray();
        Renderer[] renderers = parts
            .SelectMany(part => part.GetComponentsInChildren<Renderer>(true))
            .ToArray();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
        {
            bounds.Encapsulate(renderer.bounds);
        }

        Vector3 worldOffset = new Vector3(
            portalRoot.position.x - bounds.center.x,
            portalRoot.position.y - bounds.min.y,
            portalRoot.position.z - bounds.center.z);
        Vector3 localOffset = portalRoot.InverseTransformVector(worldOffset);
        foreach (Transform part in parts.Where(part => part.parent == portalRoot))
        {
            part.localPosition += localOffset;
        }
    }

    private static void DestroyPortalFrameFallback(Transform parent)
    {
        foreach (string name in new[] { "PortalFrame_Left", "PortalFrame_Right", "PortalFrame_Top" })
        {
            Transform existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static Transform EnsurePortalVisualCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale)
    {
        Transform existing = parent.Find(name);
        GameObject visual = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = name;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = localScale;
        foreach (Collider collider in visual.GetComponents<Collider>())
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        return visual.transform;
    }

    private static void ApplyPortalGlow(GameObject portalObject, Color color)
    {
        Material material = EnsureEmissiveMaterial("Assets/Materials/Runtime/NightPortalGlow.mat", color, 2.4f);
        foreach (Renderer renderer in portalObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = material;
        }

        Transform beaconTransform = portalObject.transform.Find("NightPortalBeacon");
        GameObject beaconObject = beaconTransform != null ? beaconTransform.gameObject : new GameObject("NightPortalBeacon");
        beaconObject.transform.SetParent(portalObject.transform, false);
        beaconObject.transform.localPosition = new Vector3(0f, 2.2f, -0.35f);
        Light beacon = EnsureComponent<Light>(beaconObject);
        beacon.type = LightType.Point;
        beacon.enabled = true;
        beacon.color = color;
        beacon.intensity = 2.2f;
        beacon.range = 8f;
        beacon.shadows = LightShadows.None;
    }

    private static void EnsurePortalFrameFallback(Transform parent, float width, float height)
    {
        EnsurePortalVisualCube(parent, "PortalFrame_Left", new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.45f, height, 0.75f));
        EnsurePortalVisualCube(parent, "PortalFrame_Right", new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.45f, height, 0.75f));
        EnsurePortalVisualCube(parent, "PortalFrame_Top", new Vector3(0f, height, 0f), new Vector3(width, 0.45f, 0.75f));
    }

    private static BoxCollider EnsurePortalBlocker(Transform parent, Vector3 localPosition, Vector3 size)
    {
        Transform existing = parent.Find("PortalPhysicalBlocker");
        GameObject blockerObject = existing != null ? existing.gameObject : new GameObject("PortalPhysicalBlocker");
        blockerObject.transform.SetParent(parent, false);
        blockerObject.transform.localPosition = localPosition;
        blockerObject.transform.localRotation = Quaternion.identity;
        blockerObject.transform.localScale = Vector3.one;
        BoxCollider blocker = EnsureComponent<BoxCollider>(blockerObject);
        blocker.isTrigger = false;
        blocker.size = size;
        return blocker;
    }

    private static void ConfigureNightScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(NightScene, OpenSceneMode.Single);

        GameObject environment = FindObject("Virus9_OldTown_ProtectedAlleys_Blockout2_before_origin_to_geometry_20260526_162356")
            ?? FindObject("Location02_ProtectedAlleysNight");
        if (environment != null) environment.name = "Location02_ProtectedAlleysNight";

        GameObject player = EnsurePlayer(new Vector3(-11.46f, -0.17f, 1.5f), true, visualPrefab, animatorController);
        EnsureCamera(player.transform, SharedCameraOffset, SharedCameraLookAtHeight, SharedCameraFieldOfView);
        EnsureFloor("FloorCollider", new Vector3(-13.55f, -1.67f, 19.81f), new Vector3(41.5f, 1f, 47.5f));
        EnsureBox("COL_L2_BackBoundary", new Vector3(-11.46f, 3.33f, -5.25f), new Vector3(62f, 9f, 1f));
        EnsureBox("COL_L2_FrontBoundary", new Vector3(-11.46f, 3.33f, 44.25f), new Vector3(62f, 9f, 1f));
        EnsureBox("COL_L2_LeftBoundary", new Vector3(-40.75f, 3.33f, 19.5f), new Vector3(1f, 9f, 50f));
        EnsureBox("COL_L2_RightBoundary", new Vector3(17.85f, 3.33f, 19.5f), new Vector3(1f, 9f, 50f));
        EnsureSolidEnvironmentColliders();
        DisableNightDecorativeColliders();
        SnapActorsToFloor<PrototypeShadowActor>();
        EnsureShadowJumpers();
        EnsureNightShadowNavigation();
        EnsureNavigationVolume("NightNavigation", new Vector3(-11.46f, 3.33f, 19.5f), new Vector3(62f, 9f, 50f));
        SnapActorsToNavigation<PrototypeShadowActor>(3f);

        DestroyIfPresent("PRICE_Altar");
        EnsureComponent<NightPhaseController>(EnsureMarker("NightPhaseController", Vector3.zero));

        ShadowNPC pleading = RequireComponent<ShadowNPC>("SHADOW_Pleading_01");
        pleading.Configure("SHADOW_PLEADING", new[] { "Не гонись за мной ради удара. Смотри, что случится дальше." }, false);
        PrototypeShadowActor afraidActor = RequireComponent<PrototypeShadowActor>("SHADOW_Afraid_01");
        PrototypeShadowActor helperActor = RequireComponent<PrototypeShadowActor>("SHADOW_Ally_01");
        PrototypeShadowActor enemyOne = RequireComponent<PrototypeShadowActor>("SHADOW_Enemy_01");
        PrototypeShadowActor enemyTwo = RequireComponent<PrototypeShadowActor>("SHADOW_Enemy_02");
        afraidActor.Configure(PrototypeShadowActor.ShadowRole.Afraid, 2);
        helperActor.Configure(PrototypeShadowActor.ShadowRole.Ally, 2);
        enemyOne.Configure(PrototypeShadowActor.ShadowRole.Enemy, 2);
        enemyTwo.Configure(PrototypeShadowActor.ShadowRole.Enemy, 2);
        afraidActor.transform.position = new Vector3(-8f, -1.09f, 20f);
        helperActor.transform.position = new Vector3(-9f, -1.09f, 22f);
        EnsureComponent<ShadowNPC>(afraidActor.gameObject).ConfigureNightReaction(
            "SHADOW_AFRAID",
            "Не подходи резко. Я все еще пытаюсь встать.",
            "Ты не ударил. Значит, здесь еще можно подняться.",
            "Я видел, что ты сделал. Не называй это выходом.");
        EnsureComponent<ShadowNPC>(helperActor.gameObject).ConfigureNightReaction(
            "SHADOW_ALLY",
            "К воротам ведет улица, но выбор идет рядом с тобой.",
            "Фрагмент появился не из смерти. Сохрани это до ворот.",
            "Врата узнают, сколько теней ты оставил лежать.");

        GameObject fragmentObject = EnsureSphere("FRAGMENT_InnerNight", new Vector3(-8f, -0.2f, 20f), 0.9f);
        LightFragmentPickup fragment = EnsureComponent<LightFragmentPickup>(fragmentObject);
        fragment.Configure(LightFragmentPickup.FragmentKind.InnerNight);
        EnsureFragmentVisual(fragment, new Color(0.22f, 0.85f, 1f), 2.35f);
        EnsureTrigger(fragmentObject, Vector3.one * 1.35f);
        fragmentObject.SetActive(false);

        GameObject witness = EnsureMarker("MERCY_WITNESS_Trigger", new Vector3(-9f, -0.17f, 17.8f));
        EnsureTrigger(witness, new Vector3(9f, 2.5f, 4.2f));
        NightFragmentEncounter encounter = EnsureComponent<NightFragmentEncounter>(witness);
        SetReference(encounter, "helper", helperActor);
        SetReference(encounter, "afraid", afraidActor);
        SetReference(encounter, "innerNightFragment", fragment);
        SetReference(encounter, "mercyDropPoint", fragmentObject.transform);
        SetReferenceArray(
            encounter,
            "allShadows",
            enemyOne,
            enemyTwo);

        GameObject nonStep = RequireObject("NON_STEP_Trigger");
        nonStep.transform.position = new Vector3(10f, -0.17f, 22f);

        LocationTransition transition = RequireComponent<LocationTransition>("EXIT_To_FinalGate_Exit");
        transition.transform.position = new Vector3(2.5f, -0.17f, 31f);
        transition.Configure("LOCATION_03_GATE_FINAL", false, true, true);
        SquarePortalController portal = EnsureNightPortal(transition.gameObject);
        transition.ConfigurePortal(portal);
        SetReference(encounter, "exitPortal", portal);
        SnapActorsToNavigation<PrototypeShadowActor>(3f);
        OrganizeSceneHierarchy(environment);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureFinalScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(FinalScene, OpenSceneMode.Single);
        GameObject environment = FindObject("Location03_GateFinal");
        GameObject player = EnsurePlayer(new Vector3(0f, 1.2f, -12f), true, visualPrefab, animatorController);
        EnsureCamera(player.transform, SharedCameraOffset, SharedCameraLookAtHeight, SharedCameraFieldOfView);
        EnsureFloor("FloorCollider", new Vector3(0f, -0.3f, 0f), new Vector3(42f, 1f, 42f));
        EnsureBox("COL_L3_BackBoundary", new Vector3(0f, 4f, -19f), new Vector3(22f, 8f, 1f));
        EnsureBox("COL_L3_GateBackstop", new Vector3(0f, 4f, 14f), new Vector3(14f, 8f, 1f));
        EnsureBox("COL_L3_LeftBoundary", new Vector3(-11f, 4f, -2.5f), new Vector3(1f, 8f, 33f));
        EnsureBox("COL_L3_RightBoundary", new Vector3(11f, 4f, -2.5f), new Vector3(1f, 8f, 33f));
        EnsureSolidEnvironmentColliders();

        GameObject evaluatorObject = RequireObject("FinalEvaluator");
        FinalStateEvaluator previousEvaluator = evaluatorObject.GetComponent<FinalStateEvaluator>();
        if (previousEvaluator != null) UnityEngine.Object.DestroyImmediate(previousEvaluator);
        FinalGateOutcomeController outcome = EnsureComponent<FinalGateOutcomeController>(evaluatorObject);

        GameObject respawn = EnsureMarker("FinalArenaRespawn", new Vector3(0f, 1.2f, -12f));
        SetReference(outcome, "arenaRespawnPoint", respawn.transform);
        SetReference(outcome, "leftGateDoor", RequireObject("Atmos_Door_Left").transform);
        SetReference(outcome, "rightGateDoor", RequireObject("Atmos_Door_Right").transform);
        SetReference(outcome, "finalEntryTrigger", RequireObject("GATE_FinalEntryTrigger").GetComponent<Collider>());

        GuardianController forceGuardian = EnsureGuardian("GUARDIAN_Force", "GUARDIAN_FORCE", true);
        GuardianController memoryGuardian = EnsureGuardian("GUARDIAN_Memory", "GUARDIAN_MEMORY", false);
        SetReferenceArray(outcome, "guardians", forceGuardian, memoryGuardian);
        SnapActorsToFloor<GuardianController>();
        EnsureNavigation("FinalNavigation", new Vector3(0f, 0.15f, -2f), new Vector3(20f, 0.1f, 30f));
        SnapActorsToNavigation<GuardianController>(3f);
        OrganizeSceneHierarchy(environment);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject EnsurePlayer(Vector3 position, bool canAttack, GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        GameObject player = FindObject("Player_Fragment");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Fragment";
        }

        player.tag = "Player";
        player.transform.SetPositionAndRotation(position, Quaternion.identity);
        Rigidbody body = EnsureComponent<Rigidbody>(player);
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        EnsureComponent<CapsuleCollider>(player);
        PlayerInputReader input = EnsureComponent<PlayerInputReader>(player);
        input.Configure(LoadInputActionsAsset());
        PlayerController3D movement = EnsureComponent<PlayerController3D>(player);
        movement.ConfigureLocomotion(SharedPlayerMoveSpeed, SharedPlayerRotationSpeed);
        movement.ConfigureTraversal(SharedPlayerJumpEnabled);
        PlayerAttackController attack = EnsureComponent<PlayerAttackController>(player);
        attack.SetSceneAttackEnabled(canAttack);
        EnsureComponent<InteractionController>(player);

        Renderer placeholder = player.GetComponent<Renderer>();
        if (placeholder != null) placeholder.enabled = false;

        DestroyLegacyPlayerVisuals(player);
        DestroyLooseDead2Roots();

        Transform visualTransform = player.transform.Find("PlayerVisual_DEAD2");
        GameObject visual = visualTransform == null ? null : visualTransform.gameObject;
        if (visual == null)
        {
            visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
            visual.name = "PlayerVisual_DEAD2";
            visual.transform.SetParent(player.transform, false);
        }

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        AlignVisualToPlayer(player, visual);
        Animator animator = EnsureComponent<Animator>(visual);
        animator.avatar = LoadPlayerAvatar();
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;
        EnsureComponent<PlayerVisualAnimator>(visual);
        return player;
    }

    private static void EnsureCamera(Transform player, Vector3 offset, float lookAtHeight, float fov)
    {
        Camera camera = Camera.main ?? UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.transform.parent == null);
        if (camera != null && camera.transform.parent != null && PrefabUtility.IsPartOfPrefabInstance(camera.gameObject))
        {
            camera.enabled = false;
            AudioListener embeddedListener = camera.GetComponent<AudioListener>();
            if (embeddedListener != null) embeddedListener.enabled = false;
            camera.gameObject.tag = "Untagged";
            camera.gameObject.name = "Embedded_Camera_Disabled";
            camera = null;
        }

        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.gameObject.name = "Main Camera";
        camera.gameObject.tag = "MainCamera";
        if (camera.transform.parent != null) camera.transform.SetParent(null, true);
        if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
        camera.fieldOfView = fov;
        EnsureComponent<PrototypeCameraFollow>(camera.gameObject).Configure(player, offset, lookAtHeight);
    }

    private static void EnsurePursuer(string objectName)
    {
        GameObject shadow = RequireObject(objectName);
        EnsureComponent<ExteriorPursuer>(shadow);
        NavMeshAgent agent = EnsureComponent<NavMeshAgent>(shadow);
        agent.radius = 0.35f;
        agent.height = 1.5f;
        EnsureNavigationActorVisual(shadow);
        EnsureComponent<EnemyJumpController>(shadow).Configure(true);
    }

    private static GuardianController EnsureGuardian(string objectName, string displayName, bool isForce)
    {
        GameObject guardian = RequireObject(objectName);
        GuardianController controller = EnsureComponent<GuardianController>(guardian);
        controller.Configure(displayName, isForce);
        NavMeshAgent agent = EnsureComponent<NavMeshAgent>(guardian);
        agent.radius = 0.4f;
        agent.height = 2f;
        EnsureNavigationActorVisual(guardian);
        EnsureComponent<EnemyJumpController>(guardian).Configure(true);
        return controller;
    }

    private static void EnsureShadowJumpers()
    {
        foreach (PrototypeShadowActor shadow in UnityEngine.Object.FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            EnsureComponent<EnemyJumpController>(shadow.gameObject).Configure(true);
        }
    }

    private static void EnsureNightShadowNavigation()
    {
        foreach (PrototypeShadowActor shadow in UnityEngine.Object.FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            NavMeshAgent agent = EnsureComponent<NavMeshAgent>(shadow.gameObject);
            agent.radius = 0.35f;
            agent.height = 1.5f;
            EnsureNavigationActorVisual(shadow.gameObject);
        }
    }

    private static void EnsureNavigationActorVisual(GameObject actor)
    {
        MeshFilter rootFilter = actor.GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = actor.GetComponent<MeshRenderer>();
        if (rootFilter == null || rootFilter.sharedMesh == null || rootRenderer == null) return;

        Transform visualTransform = actor.transform.Find("NavigationVisual");
        rootRenderer.enabled = true;
        float lossyScaleY = Mathf.Max(0.001f, Mathf.Abs(actor.transform.lossyScale.y));
        float bottomOffset = Mathf.Max(0.1f, rootRenderer.bounds.extents.y / lossyScaleY);
        if (visualTransform != null) UnityEngine.Object.DestroyImmediate(visualTransform.gameObject);

        foreach (Collider collider in actor.GetComponents<Collider>())
        {
            if (collider is BoxCollider box)
            {
                box.center = new Vector3(box.center.x, 0f, box.center.z);
            }
            else if (collider is CapsuleCollider capsule)
            {
                capsule.center = new Vector3(capsule.center.x, 0f, capsule.center.z);
            }
            else if (collider is SphereCollider sphere)
            {
                sphere.center = new Vector3(sphere.center.x, 0f, sphere.center.z);
            }
        }

        NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
        if (agent != null) agent.baseOffset = bottomOffset;
    }

    private static void EnsureNavigation(string name, Vector3 walkableCenter, Vector3 walkableSize)
    {
        GameObject navigation = EnsureMarker(name, Vector3.zero);
        Transform existingWalkable = navigation.transform.Find("NAV_Walkable");
        GameObject walkable = existingWalkable != null ? existingWalkable.gameObject : new GameObject("NAV_Walkable");
        walkable.transform.SetParent(navigation.transform, true);
        walkable.transform.position = walkableCenter;
        BoxCollider walkableCollider = EnsureComponent<BoxCollider>(walkable);
        walkableCollider.size = walkableSize;

        NavMeshSurface surface = EnsureComponent<NavMeshSurface>(navigation);
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }

    private static void EnsureNavigationVolume(string name, Vector3 center, Vector3 size)
    {
        GameObject navigation = EnsureMarker(name, Vector3.zero);
        DestroyIfPresent("NAV_Walkable");

        NavMeshSurface surface = EnsureComponent<NavMeshSurface>(navigation);
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.center = center;
        surface.size = size;
        surface.BuildNavMesh();
    }

    private static void EnsureSolidEnvironmentColliders()
    {
        foreach (MeshFilter meshFilter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsSolidEnvironmentMesh(meshFilter.name) || meshFilter.sharedMesh == null) continue;

            MeshCollider collider = EnsureComponent<MeshCollider>(meshFilter.gameObject);
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            meshFilter.gameObject.isStatic = true;
        }
    }

    private static void DisableNightDecorativeColliders()
    {
        foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ShouldDisableNightCollider(collider.name))
            {
                collider.enabled = false;
            }
        }
    }

    private static bool ShouldDisableNightCollider(string objectName)
    {
        return IsVisualOnlyEnvironmentMesh(objectName) ||
               objectName.Contains("Roof", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Lantern", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Ring", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("DoNotUseAsCollider", StringComparison.OrdinalIgnoreCase) ||
               objectName.StartsWith("LAMP_", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureExteriorEnvironmentColliders(GameObject environment)
    {
        foreach (MeshFilter meshFilter in environment.GetComponentsInChildren<MeshFilter>(true))
        {
            foreach (Collider collider in meshFilter.GetComponents<Collider>())
            {
                collider.enabled = !ShouldDisableExteriorCollider(meshFilter.name);
            }

            if (ShouldDisableExteriorCollider(meshFilter.name) ||
                !IsExteriorObstacleMesh(meshFilter.name) ||
                meshFilter.sharedMesh == null ||
                meshFilter.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshFilter.gameObject.isStatic = true;
        }
    }

    private static bool ShouldDisableExteriorCollider(string objectName)
    {
        return IsVisualOnlyEnvironmentMesh(objectName) ||
               objectName.Contains("Roof", StringComparison.OrdinalIgnoreCase) ||
               objectName.EndsWith("_R", StringComparison.Ordinal) ||
               objectName.EndsWith("_Win", StringComparison.Ordinal) ||
               objectName.EndsWith("_In", StringComparison.Ordinal) ||
               objectName.StartsWith("Drift", StringComparison.Ordinal) ||
               objectName.StartsWith("StreetDrift", StringComparison.Ordinal) ||
               objectName.StartsWith("Ice", StringComparison.Ordinal) ||
               objectName.StartsWith("Icicle_", StringComparison.Ordinal);
    }

    private static bool IsExteriorObstacleMesh(string objectName)
    {
        if (IsVisualOnlyEnvironmentMesh(objectName) ||
            objectName == "Ground" ||
            objectName.StartsWith("Drift", StringComparison.Ordinal) ||
            objectName.StartsWith("StreetDrift", StringComparison.Ordinal) ||
            objectName.StartsWith("Ice", StringComparison.Ordinal) ||
            objectName.StartsWith("Icicle_", StringComparison.Ordinal) ||
            objectName.StartsWith("St_", StringComparison.Ordinal) ||
            objectName.EndsWith("_R", StringComparison.Ordinal) ||
            objectName.EndsWith("_D", StringComparison.Ordinal) ||
            objectName.EndsWith("_W", StringComparison.Ordinal) ||
            objectName.EndsWith("_In", StringComparison.Ordinal) ||
            objectName.Contains("_Ch", StringComparison.Ordinal) ||
            objectName.Contains("_C", StringComparison.Ordinal) ||
            objectName.Contains(".", StringComparison.Ordinal))
        {
            return false;
        }

        return IsSolidEnvironmentMesh(objectName) ||
               IsExteriorBuildingCore(objectName) ||
               objectName == "BigTank" ||
               objectName == "CtrlRoom" ||
               objectName == "PumpHouse" ||
               objectName.StartsWith("Div_", StringComparison.Ordinal) ||
               objectName.StartsWith("FB_", StringComparison.Ordinal) ||
               objectName.StartsWith("FP_", StringComparison.Ordinal) ||
               objectName.StartsWith("Lamp_Post_", StringComparison.Ordinal) ||
               objectName.StartsWith("Pipe", StringComparison.Ordinal) ||
               objectName.StartsWith("PS_", StringComparison.Ordinal) ||
               objectName.StartsWith("Tank_", StringComparison.Ordinal) ||
               objectName.StartsWith("Silo_", StringComparison.Ordinal) ||
               objectName.StartsWith("Wall_", StringComparison.Ordinal);
    }

    private static bool IsExteriorBuildingCore(string objectName)
    {
        return (objectName.StartsWith("WH_", StringComparison.Ordinal) ||
                objectName.StartsWith("WS_", StringComparison.Ordinal) ||
                objectName.StartsWith("Res_", StringComparison.Ordinal)) &&
               objectName.Count(character => character == '_') == 1;
    }

    private static bool IsSolidEnvironmentMesh(string objectName)
    {
        if (IsVisualOnlyEnvironmentMesh(objectName)) return false;

        return objectName.StartsWith("BUILDING_", StringComparison.Ordinal) ||
               objectName.StartsWith("ARCH_", StringComparison.Ordinal) ||
               objectName.StartsWith("GATE_", StringComparison.Ordinal) ||
               objectName.StartsWith("Gate_", StringComparison.Ordinal) ||
               objectName.StartsWith("Fence_", StringComparison.Ordinal) ||
               objectName.StartsWith("FBar_", StringComparison.Ordinal) ||
               objectName.StartsWith("Barrel_", StringComparison.Ordinal) ||
               objectName.StartsWith("BarrelL_", StringComparison.Ordinal) ||
               objectName.StartsWith("Fount_", StringComparison.Ordinal) ||
               objectName.StartsWith("BALCONY_", StringComparison.Ordinal) ||
               objectName.StartsWith("Atmos_Frame", StringComparison.Ordinal) ||
               objectName.StartsWith("Atmos_Door", StringComparison.Ordinal) ||
               objectName.StartsWith("Broken_Pillar", StringComparison.Ordinal) ||
               objectName.StartsWith("Obelisk_Base", StringComparison.Ordinal) ||
               objectName.StartsWith("Obelisk_Shaft", StringComparison.Ordinal) ||
               objectName.StartsWith("Guardian_Cloak", StringComparison.Ordinal) ||
               objectName.StartsWith("Guardian_Head", StringComparison.Ordinal) ||
               objectName.StartsWith("Guardian_Chest", StringComparison.Ordinal) ||
               objectName.Contains("TreeTrunk", StringComparison.Ordinal) ||
               objectName.Contains("_TinySanctuary", StringComparison.Ordinal) ||
               objectName.Contains("_TinyFountain_Basin", StringComparison.Ordinal);
    }

    private static bool IsVisualOnlyEnvironmentMesh(string objectName)
    {
        return objectName.Contains("Glow", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Glass", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Window", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Moss", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Wet", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Reflect", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Translucent", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("ProtectionVolume", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("InnerColorFloor", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Cobblestone", StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Pavers", StringComparison.OrdinalIgnoreCase);
    }

    private static void SnapActorsToFloor<T>() where T : Component
    {
        Physics.SyncTransforms();
        Collider floor = FindObject("FloorCollider")?.GetComponent<Collider>();
        if (floor == null) return;

        float floorTop = floor.bounds.max.y;
        foreach (T actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Renderer renderer = actor.GetComponentsInChildren<Renderer>(true).FirstOrDefault(candidate => candidate.enabled);
            if (renderer == null) continue;

            Vector3 position = actor.transform.position;
            position.y += floorTop - renderer.bounds.min.y;
            actor.transform.position = position;
        }
    }

    private static void SnapActorsToNavigation<T>(float maxDistance) where T : Component
    {
        foreach (T actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!NavMesh.SamplePosition(actor.transform.position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas)) continue;

            NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
            float worldBaseOffset = agent != null
                ? agent.baseOffset * Mathf.Abs(actor.transform.lossyScale.y)
                : 0f;
            actor.transform.position = hit.position + Vector3.up * worldBaseOffset;
        }
    }

    private static void AlignVisualToPlayer(GameObject player, GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Collider playerCollider = player.GetComponent<Collider>();
        float targetBottom = playerCollider == null ? player.transform.position.y : playerCollider.bounds.min.y;
        Vector3 centerOffset = bounds.center - player.transform.position;
        visual.transform.position -= new Vector3(centerOffset.x, bounds.min.y - targetBottom, centerOffset.z);
    }

    private static void UpdateBuildRoute()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ExteriorScene, true),
            new EditorBuildSettingsScene(NightScene, true),
            new EditorBuildSettingsScene(FinalScene, true)
        };
    }

    private static void EnsurePauseInputAction()
    {
        InputActionAsset actions = LoadInputActionsAsset();
        InputActionMap playerMap = actions.FindActionMap("Player", true);
        InputAction pause = playerMap.FindAction("Pause", false) ?? playerMap.AddAction("Pause", InputActionType.Button);
        if (!pause.bindings.Any(binding => binding.path == "<Keyboard>/escape"))
        {
            pause.AddBinding("<Keyboard>/escape");
        }

        if (!pause.bindings.Any(binding => binding.path == "<Gamepad>/start"))
        {
            pause.AddBinding("<Gamepad>/start");
        }

        EnsureUngroupedBinding(playerMap, "Sprint", "<Keyboard>/leftShift");
        EnsureUngroupedBinding(playerMap, "Jump", "<Keyboard>/space");
        EnsureUngroupedBinding(playerMap, "Attack", "<Mouse>/leftButton");
        EnsureUngroupedBinding(playerMap, "Interact", "<Keyboard>/e");

        File.WriteAllText(InputActionsAsset, actions.ToJson());
        AssetDatabase.ImportAsset(InputActionsAsset, ImportAssetOptions.ForceUpdate);
    }

    private static void EnsureUngroupedBinding(InputActionMap map, string actionName, string bindingPath)
    {
        InputAction action = map.FindAction(actionName, true);
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.path != bindingPath || string.IsNullOrEmpty(binding.groups)) continue;

            action.ChangeBinding(i).Erase();
            action.AddBinding(bindingPath);
            return;
        }
    }

    private static RuntimeAnimatorController EnsureAnimatorController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController) != null)
            AssetDatabase.DeleteAsset(PlayerController);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerController);
        controller.AddParameter(MoveSpeedParameter, AnimatorControllerParameterType.Float);
        controller.AddParameter(GroundedParameter, AnimatorControllerParameterType.Bool);
        controller.AddParameter(JumpParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(RunningJumpParameter, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

        AnimationClip idle = LoadAnimationClip(CourseAnimationsFbx, "Idle");
        AnimationClip walk = LoadAnimationClip(PlayerStaticAnimationsFbx, "Walk_Static");
        AnimationClip run = LoadAnimationClip(PlayerStaticAnimationsFbx, "Run_Static");
        AnimationClip jump = LoadAnimationClip(CourseAnimationsFbx, "Standing_Jump");
        AnimationClip runningJump = LoadAnimationClip(CourseAnimationsFbx, "Running_Jump");
        AnimationClip attack = LoadAnimationClip(CourseAnimationsFbx, "GrenadeThrow");
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState locomotion = machine.AddState("Locomotion");
        BlendTree locomotionTree = new BlendTree
        {
            name = "LocomotionBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = MoveSpeedParameter,
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(locomotionTree, controller);
        locomotionTree.AddChild(idle, 0f);
        locomotionTree.AddChild(walk, 0.4f);
        locomotionTree.AddChild(run, 1f);
        locomotion.motion = locomotionTree;
        locomotion.writeDefaultValues = false;
        machine.defaultState = locomotion;

        AnimatorState jumpState = machine.AddState("Jump");
        jumpState.motion = jump;
        jumpState.writeDefaultValues = false;
        AnimatorStateTransition jumpTransition = machine.AddAnyStateTransition(jumpState);
        jumpTransition.AddCondition(AnimatorConditionMode.If, 0f, JumpParameter);
        jumpTransition.hasExitTime = false;
        jumpTransition.duration = 0.08f;
        jumpTransition.canTransitionToSelf = false;
        AnimatorStateTransition jumpReturn = jumpState.AddTransition(locomotion);
        jumpReturn.hasExitTime = true;
        jumpReturn.exitTime = 0.92f;
        jumpReturn.duration = 0.12f;

        AnimatorState runningJumpState = machine.AddState("RunningJump");
        runningJumpState.motion = runningJump;
        runningJumpState.writeDefaultValues = false;
        AnimatorStateTransition runningJumpTransition = machine.AddAnyStateTransition(runningJumpState);
        runningJumpTransition.AddCondition(AnimatorConditionMode.If, 0f, RunningJumpParameter);
        runningJumpTransition.hasExitTime = false;
        runningJumpTransition.duration = 0.08f;
        runningJumpTransition.canTransitionToSelf = false;
        AnimatorStateTransition runningJumpReturn = runningJumpState.AddTransition(locomotion);
        runningJumpReturn.hasExitTime = true;
        runningJumpReturn.exitTime = 0.92f;
        runningJumpReturn.duration = 0.12f;

        AnimatorState attackState = machine.AddState("Attack");
        attackState.motion = attack;
        attackState.speed = 1.8f;
        attackState.writeDefaultValues = false;
        AnimatorStateTransition attackTransition = machine.AddAnyStateTransition(attackState);
        attackTransition.AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);
        attackTransition.hasExitTime = false;
        attackTransition.duration = 0.05f;
        attackTransition.canTransitionToSelf = false;
        AnimatorStateTransition attackReturn = attackState.AddTransition(locomotion);
        attackReturn.hasExitTime = true;
        attackReturn.exitTime = 0.88f;
        attackReturn.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadAnimationClip(string path, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => candidate.name == clipName);
        if (clip == null) throw new InvalidOperationException("Animation clip not found: " + path + " :: " + clipName);
        return clip;
    }

    private static Avatar LoadPlayerAvatar()
    {
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(PlayerFbx).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Player FBX does not provide a valid humanoid Avatar: " + PlayerFbx);
        return avatar;
    }

    private static void DestroyLegacyPlayerVisuals(GameObject player)
    {
        foreach (Transform child in player.transform.Cast<Transform>().Where(IsLegacyPlayerVisual).ToArray())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static bool IsLegacyPlayerVisual(Transform child)
    {
        return child.name.StartsWith("Fragmento_Walk", StringComparison.Ordinal) ||
               child.name.StartsWith("Embedded_Camera_Disabled", StringComparison.Ordinal) ||
               child.name.Contains("Missing Prefab", StringComparison.Ordinal);
    }

    private static void DestroyLooseDead2Roots()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects().Where(candidate => candidate.name == "DEAD2").ToArray())
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void DeleteUnusedPlayerSetupAssets()
    {
        DeleteAssetWhenUnused(PlayerFolder + "/PlayerLocomotion.controller");
        DeleteAssetWhenUnused(PlayerFolder + "/DEAD2.controller");
        DeleteAssetWhenUnused(PlayerFolder + "/SimpleCharacter_5.0.controller");
    }

    private static void DeleteAssetWhenUnused(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null) return;

        bool referenced = AssetDatabase.GetAllAssetPaths()
            .Where(path => !string.Equals(path, assetPath, StringComparison.OrdinalIgnoreCase))
            .Any(path => AssetDatabase.GetDependencies(path, false).Contains(assetPath));
        if (!referenced) AssetDatabase.DeleteAsset(assetPath);
    }

    private static void ValidateAnimatorController(List<string> issues)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
        if (controller == null)
        {
            issues.Add("Shared humanoid Animator Controller is missing.");
            return;
        }

        HashSet<string> parameters = controller.parameters.Select(parameter => parameter.name).ToHashSet();
        foreach (string parameter in new[] { MoveSpeedParameter, GroundedParameter, JumpParameter, RunningJumpParameter, AttackParameter })
        {
            if (!parameters.Contains(parameter)) issues.Add("Animator Controller misses parameter: " + parameter);
        }

        if (controller.layers.Length == 0)
        {
            issues.Add("Animator Controller has no layers.");
            return;
        }

        HashSet<string> states = controller.layers[0].stateMachine.states.Select(child => child.state.name).ToHashSet();
        foreach (string state in new[] { "Locomotion", "Jump", "RunningJump", "Attack" })
        {
            if (!states.Contains(state)) issues.Add("Animator Controller misses state: " + state);
        }
    }

    private static void DeleteLegacyNightAssetsWhenUnused()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LegacyNightScene) != null)
        {
            AssetDatabase.DeleteAsset(LegacyNightScene);
        }

        const string oldArtFolder = "Assets/Art/Locations/Location02_OldTownNightSquare";
        string oldFbx = oldArtFolder + "/Location02_OldTownNightSquare.fbx";
        bool hasExternalReference = AssetDatabase.GetAllAssetPaths()
            .Where(path => !path.StartsWith(oldArtFolder, StringComparison.OrdinalIgnoreCase))
            .Any(path => AssetDatabase.GetDependencies(path, false).Contains(oldFbx));
        if (!hasExternalReference && AssetDatabase.IsValidFolder(oldArtFolder))
        {
            AssetDatabase.DeleteAsset(oldArtFolder);
        }
    }

    private static void ValidateScene(string path, List<string> issues, params string[] requiredObjects)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            issues.Add("Missing scene: " + path);
            return;
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        foreach (string requiredObject in requiredObjects)
        {
            if (FindObject(requiredObject) == null) issues.Add(Path.GetFileName(path) + " misses " + requiredObject + ".");
        }

        GameObject player = FindObject("Player_Fragment");
        if (player != null && player.GetComponentInChildren<PlayerVisualAnimator>(true) == null)
        {
            issues.Add(Path.GetFileName(path) + " has no animated player visual.");
        }

        if (player != null)
        {
            GameObject visual = player.transform.Find("PlayerVisual_DEAD2")?.gameObject;
            if (visual == null)
            {
                issues.Add(Path.GetFileName(path) + " has no clean DEAD2 player visual.");
            }
            else
            {
                Animator animator = visual.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                {
                    issues.Add(Path.GetFileName(path) + " has no valid humanoid player Avatar.");
                }

                if (animator == null || AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != PlayerController)
                {
                    issues.Add(Path.GetFileName(path) + " does not use the shared humanoid Animator Controller.");
                }
            }

            if (player.transform.Cast<Transform>().Any(IsLegacyPlayerVisual))
            {
                issues.Add(Path.GetFileName(path) + " still has a legacy or missing player visual.");
            }

            PlayerController3D movement = player.GetComponent<PlayerController3D>();
            PlayerInputReader input = player.GetComponent<PlayerInputReader>();
            if (input == null)
            {
                issues.Add(Path.GetFileName(path) + " has no player input reader.");
            }

            if (movement != null && movement.JumpEnabled != SharedPlayerJumpEnabled)
            {
                issues.Add(Path.GetFileName(path) + " does not use the shared player jump profile.");
            }

            if (movement != null &&
                (Mathf.Abs(movement.MoveSpeed - SharedPlayerMoveSpeed) > 0.01f ||
                 Mathf.Abs(movement.RotationSpeed - SharedPlayerRotationSpeed) > 0.01f))
            {
                issues.Add(Path.GetFileName(path) + " does not use the shared player movement profile.");
            }

            Collider floor = FindObject("FloorCollider")?.GetComponent<Collider>();
            Collider playerCollider = player.GetComponent<Collider>();
            if (floor != null && playerCollider != null &&
                Mathf.Abs(playerCollider.bounds.min.y - floor.bounds.max.y) > 0.08f)
            {
                issues.Add(Path.GetFileName(path) + " has player collider above or below the floor.");
            }

            Renderer visualRenderer = player.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(renderer => renderer.gameObject != player);
            if (visualRenderer != null)
            {
                Vector3 centerOffset = visualRenderer.bounds.center - player.transform.position;
                centerOffset.y = 0f;
                if (centerOffset.magnitude > 0.08f)
                {
                    issues.Add(Path.GetFileName(path) + " has off-center player visual.");
                }

                if (floor != null && Mathf.Abs(visualRenderer.bounds.min.y - floor.bounds.max.y) > 0.08f)
                {
                    issues.Add(Path.GetFileName(path) + " has player visual above or below the floor.");
                }
            }
        }

        if (SceneManager.GetActiveScene().GetRootGameObjects().Any(root => root.name == "DEAD2"))
        {
            issues.Add(Path.GetFileName(path) + " still has a loose DEAD2 scene root.");
        }

        Camera mainCamera = Camera.main;
        PrototypeCameraFollow cameraFollow = mainCamera == null ? null : mainCamera.GetComponent<PrototypeCameraFollow>();
        if (mainCamera == null || cameraFollow == null)
        {
            issues.Add(Path.GetFileName(path) + " has no configured shared gameplay camera.");
        }
        else
        {
            Vector3 cameraOffset = GetSerializedVector3(cameraFollow, "offset");
            float lookAtHeight = GetSerializedFloat(cameraFollow, "lookAtHeight");
            if (Vector3.Distance(cameraOffset, SharedCameraOffset) > 0.01f ||
                Mathf.Abs(lookAtHeight - SharedCameraLookAtHeight) > 0.01f ||
                Mathf.Abs(mainCamera.fieldOfView - SharedCameraFieldOfView) > 0.01f)
            {
                issues.Add(Path.GetFileName(path) + " does not use the shared gameplay camera profile.");
            }
        }

        Collider sceneFloor = FindObject("FloorCollider")?.GetComponent<Collider>();
        Collider walkable = FindObject("NAV_Walkable")?.GetComponent<Collider>();
        if (sceneFloor != null && walkable != null &&
            Mathf.Abs(walkable.bounds.max.y - sceneFloor.bounds.max.y) > 0.08f)
        {
            issues.Add(Path.GetFileName(path) + " has navigation below or above the floor.");
        }

        ValidateSolidEnvironmentColliders(path, issues);

        if (path == ExteriorScene)
        {
            ValidateActorPlacement<ExteriorPursuer>(path, issues, 0.08f);
            ValidateActorsClearOfEnvironment<ExteriorPursuer>(path, issues);
            ValidateEnemyJumpers<ExteriorPursuer>(path, issues);
        }

        if (path == NightScene)
        {
            ValidateActorPlacement<PrototypeShadowActor>(path, issues, 0.08f);
            ValidateActorsClearOfEnvironment<PrototypeShadowActor>(path, issues);
            ValidateEnemyJumpers<PrototypeShadowActor>(path, issues);
        }

        if (path == FinalScene)
        {
            FinalGateOutcomeController outcome = FindObject("FinalEvaluator")?.GetComponent<FinalGateOutcomeController>();
            if (outcome == null ||
                !HasSerializedReference(outcome, "leftGateDoor") ||
                !HasSerializedReference(outcome, "rightGateDoor") ||
                !HasSerializedReference(outcome, "finalEntryTrigger"))
            {
                issues.Add(Path.GetFileName(path) + " has unassigned final gate references.");
            }

            ValidateActorPlacement<GuardianController>(path, issues, 0.08f);
            ValidateEnemyJumpers<GuardianController>(path, issues);
        }
    }

    private static void ValidateSolidEnvironmentColliders(string path, List<string> issues)
    {
        List<string> missing = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(meshFilter => IsSolidEnvironmentMesh(meshFilter.name) &&
                                 meshFilter.sharedMesh != null &&
                                 meshFilter.GetComponentInParent<SquarePortalController>() == null &&
                                 meshFilter.GetComponent<Collider>() == null &&
                                 meshFilter.GetComponentInParent<Collider>() == null &&
                                 meshFilter.GetComponentInChildren<Collider>(true) == null)
            .Select(meshFilter => meshFilter.name)
            .Take(8)
            .ToList();

        if (missing.Count > 0)
        {
            issues.Add(Path.GetFileName(path) + " has solid environment meshes without colliders: " + string.Join(", ", missing));
        }
    }

    private static void ValidateEnemyJumpers<T>(string path, List<string> issues) where T : Component
    {
        foreach (T enemy in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            EnemyJumpController jumper = enemy.GetComponent<EnemyJumpController>();
            if (jumper == null || !jumper.JumpEnabled)
            {
                issues.Add(Path.GetFileName(path) + " has enemy without jump ability: " + enemy.name);
            }
        }
    }

    private static void ValidateActorPlacement<T>(string path, List<string> issues, float tolerance) where T : Component
    {
        foreach (T actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Renderer renderer = actor.GetComponentsInChildren<Renderer>(true).FirstOrDefault(candidate => candidate.enabled);
            if (renderer == null) continue;

            if (!NavMesh.SamplePosition(actor.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas) ||
                Mathf.Abs(renderer.bounds.min.y - hit.position.y) > Mathf.Max(tolerance, 0.15f))
            {
                issues.Add(Path.GetFileName(path) + " has misplaced actor " + actor.name + ".");
            }
        }
    }

    private static void ValidateActorsClearOfEnvironment<T>(string path, List<string> issues) where T : Component
    {
        Physics.SyncTransforms();
        foreach (T actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Collider[] blockers = Physics.OverlapCapsule(
                    actor.transform.position + Vector3.up * 0.15f,
                    actor.transform.position + Vector3.up * 1.7f,
                    0.42f,
                    ~0,
                    QueryTriggerInteraction.Ignore)
                .Where(collider => collider != null &&
                                   collider.name != "FloorCollider" &&
                                   collider.bounds.max.y > actor.transform.position.y + 0.28f &&
                                   collider.GetComponentInParent<T>() == null)
                .ToArray();
            if (blockers.Length > 0)
            {
                issues.Add(Path.GetFileName(path) + " has actor inside solid environment: " +
                           actor.name + " intersects " + string.Join(", ", blockers.Select(blocker => blocker.name).Distinct()) + ".");
            }
        }
    }

    private static Vector3 GetSerializedVector3(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property == null ? Vector3.zero : property.vector3Value;
    }

    private static float GetSerializedFloat(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property == null ? 0f : property.floatValue;
    }

    private static bool HasSerializedReference(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null && property.objectReferenceValue != null;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art/Characters");
        EnsureFolder(PlayerFolder);
        EnsureFolder(PlayerFolder + "/Models");
        EnsureFolder(PlayerFolder + "/Animations");
        EnsureFolder(PlayerFolder + "/Controllers");
        EnsureFolder(ExteriorArtFolder);
        EnsureFolder(ExteriorArtFolder + "/Models");
        EnsureFolder(NightArtFolder);
        EnsureFolder(NightArtFolder + "/Models");
        EnsureFolder("Assets/Art/Locations/Location03_GateFinal/Models");
        EnsureFolder("Assets/Scenes/Playable");
        EnsureFolder("Assets/Scenes/Archive");
        EnsureFolder("Assets/Materials/Runtime");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void MoveAssetIfNeeded(string source, string destination)
    {
        if (AssetDatabase.LoadMainAssetAtPath(destination) != null) return;
        if (AssetDatabase.LoadMainAssetAtPath(source) == null) return;

        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }

    private static GameObject EnsureMarker(string name, Vector3 position)
    {
        GameObject marker = FindObject(name) ?? new GameObject(name);
        marker.transform.position = position;
        return marker;
    }

    private static GameObject EnsureSphere(string name, Vector3 position, float scale)
    {
        GameObject obj = FindObject(name);
        if (obj == null)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
        }

        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * scale;
        return obj;
    }

    private static void EnsureFragmentVisual(LightFragmentPickup fragment, Color color, float lightIntensity)
    {
        Vector3 scale = fragment.transform.localScale;
        fragment.transform.localScale = new Vector3(
            Mathf.Max(scale.x, 0.85f),
            Mathf.Max(scale.y, 0.85f),
            Mathf.Max(scale.z, 0.85f));

        Renderer renderer = fragment.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            string materialPath = fragment.Kind == LightFragmentPickup.FragmentKind.Exterior
                ? "Assets/Materials/Runtime/ExteriorFragmentGlow.mat"
                : "Assets/Materials/Runtime/NightFragmentGlow.mat";
            renderer.sharedMaterial = EnsureEmissiveMaterial(materialPath, color, 2.2f);
        }

        Transform glowTransform = fragment.transform.Find("FragmentGlow");
        GameObject glowObject = glowTransform != null ? glowTransform.gameObject : new GameObject("FragmentGlow");
        glowObject.transform.SetParent(fragment.transform, false);
        glowObject.transform.localPosition = Vector3.zero;
        Light glow = EnsureComponent<Light>(glowObject);
        glow.type = LightType.Point;
        glow.enabled = true;
        glow.color = color;
        glow.intensity = lightIntensity;
        glow.range = 6f;
        glow.shadows = LightShadows.None;
        EnsureComponent<FragmentVisualPulse>(fragment.gameObject).Configure(glow);
    }

    private static Material EnsureEmissiveMaterial(string path, Color color, float emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No Lit shader found for runtime materials.");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * emission);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void OrganizeSceneHierarchy(GameObject environment)
    {
        Transform environmentGroup = EnsureHierarchyGroup("_ENVIRONMENT");
        Transform lightingGroup = EnsureHierarchyGroup("_LIGHTING");
        Transform playerGroup = EnsureHierarchyGroup("_PLAYER");
        Transform npcGroup = EnsureHierarchyGroup("_NPC");
        Transform fragmentsGroup = EnsureHierarchyGroup("_FRAGMENTS");
        Transform portalsGroup = EnsureHierarchyGroup("_PORTALS");
        Transform triggersGroup = EnsureHierarchyGroup("_TRIGGERS");
        Transform spawnsGroup = EnsureHierarchyGroup("_SPAWN_POINTS");
        Transform camerasGroup = EnsureHierarchyGroup("_CAMERAS");
        Transform systemsGroup = EnsureHierarchyGroup("_SYSTEMS");
        Transform navigationGroup = EnsureHierarchyGroup("_NAVIGATION");

        ParentUnder(environment, environmentGroup);
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects().ToArray())
        {
            if (root.name.StartsWith("_", StringComparison.Ordinal)) continue;

            Transform destination = null;
            if (root.GetComponent<Camera>() != null) destination = camerasGroup;
            else if (root.GetComponent<Light>() != null) destination = lightingGroup;
            else if (root.GetComponent<PlayerController3D>() != null) destination = playerGroup;
            else if (root.GetComponent<ExteriorPursuer>() != null ||
                     root.GetComponent<PrototypeShadowActor>() != null ||
                     root.GetComponent<GuardianController>() != null) destination = npcGroup;
            else if (root.GetComponent<LightFragmentPickup>() != null) destination = fragmentsGroup;
            else if (root.GetComponent<SquarePortalController>() != null) destination = portalsGroup;
            else if (root.GetComponent<NavMeshSurface>() != null) destination = navigationGroup;
            else if (root.name.Contains("Respawn", StringComparison.OrdinalIgnoreCase)) destination = spawnsGroup;
            else if (root.GetComponent<LocationTransition>() != null ||
                     root.name.Contains("Trigger", StringComparison.OrdinalIgnoreCase)) destination = triggersGroup;
            else if (root.GetComponent<ExteriorHuntController>() != null ||
                     root.GetComponent<NightPhaseController>() != null ||
                     root.GetComponent<FinalGateOutcomeController>() != null) destination = systemsGroup;
            else if (root.name == "FloorCollider" ||
                     root.name.StartsWith("COL_", StringComparison.Ordinal)) destination = environmentGroup;

            ParentUnder(root, destination);
        }
    }

    private static Transform EnsureHierarchyGroup(string name)
    {
        GameObject group = FindObject(name);
        if (group == null)
        {
            group = new GameObject(name);
        }

        group.transform.SetParent(null, false);
        group.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    private static void ParentUnder(GameObject obj, Transform parent)
    {
        if (obj == null || parent == null || obj.transform == parent) return;
        obj.transform.SetParent(parent, true);
    }

    private static void EnsureFloor(string name, Vector3 position, Vector3 scale)
    {
        GameObject floor = FindObject(name);
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
        }

        floor.transform.position = position;
        floor.transform.localScale = scale;
        Renderer renderer = floor.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private static void EnsureBox(string name, Vector3 position, Vector3 size)
    {
        GameObject box = EnsureMarker(name, position);
        BoxCollider collider = EnsureComponent<BoxCollider>(box);
        collider.isTrigger = false;
        collider.size = size;
    }

    private static void EnsureTrigger(GameObject obj, Vector3 size)
    {
        foreach (Collider collider in obj.GetComponents<Collider>())
        {
            collider.isTrigger = true;
        }

        BoxCollider trigger = EnsureComponent<BoxCollider>(obj);
        trigger.isTrigger = true;
        trigger.size = size;
    }

    private static void DestroyIfPresent(string objectName)
    {
        GameObject obj = FindObject(objectName);
        if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
    }

    private static GameObject RequireObject(string objectName)
    {
        GameObject obj = FindObject(objectName);
        if (obj == null) throw new InvalidOperationException("Scene object not found: " + objectName);
        return obj;
    }

    private static T RequireComponent<T>(string objectName) where T : Component
    {
        return EnsureComponent<T>(RequireObject(objectName));
    }

    private static T EnsureComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private static InputActionAsset LoadInputActionsAsset()
    {
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAsset);
        if (actions == null) throw new InvalidOperationException("Input actions asset not found: " + InputActionsAsset);
        return actions;
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object reference)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException("Serialized field missing: " + propertyName);
        property.objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReferenceArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] references)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray) throw new InvalidOperationException("Serialized array field missing: " + propertyName);

        property.arraySize = references.Length;
        for (int i = 0; i < references.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == objectName);
            if (match != null) return match.gameObject;
        }

        return null;
    }

    private static GameObject FindObjectInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == objectName);
            if (match != null) return match.gameObject;
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name == objectName);
    }
}
