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
using UnityEngine.SceneManagement;

public static class PlayableSceneMigration
{
    private const string ExteriorScene = "Assets/Scenes/LOCATION_01_EXTERIOR_DAY.unity";
    private const string LegacyNightScene = "Assets/Scenes/LOCATION_02_INNER_NIGHT_SQUARE.unity";
    private const string SourceNightScene = "Assets/Scenes/LOcate2.unity";
    private const string NightScene = "Assets/Scenes/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string FinalScene = "Assets/Scenes/LOCATION_03_GATE_FINAL.unity";
    private const string SourceEnvironment = "Assets/Scenes/Virus9_OldTown_ProtectedAlleys_Blockout2_before_origin_to_geometry_20260526_162356.fbx";
    private const string NightArtFolder = "Assets/Art/Locations/Location02_ProtectedAlleysNight";
    private const string NightEnvironment = NightArtFolder + "/Location02_ProtectedAlleysNight.fbx";
    private const string SourcePlayerFbx = "Assets/Scripts/Player/Fragmento_Walk.fbx";
    private const string PlayerFolder = "Assets/Art/Characters/Player";
    private const string PlayerFbx = PlayerFolder + "/Fragmento_Walk.fbx";
    private const string PlayerController = PlayerFolder + "/PlayerLocomotion.controller";
    private static readonly Vector3 SharedCameraOffset = new Vector3(0f, 5.8f, -8.5f);
    private const float SharedCameraLookAtHeight = 1.1f;
    private const float SharedCameraFieldOfView = 52f;
    private const float SharedPlayerMoveSpeed = 5f;
    private const float SharedPlayerRotationSpeed = 10f;
    private const bool SharedPlayerJumpEnabled = true;

    [MenuItem("Tools/Virus 9/Apply Playable Scene Migration")]
    public static void Apply()
    {
        EnsureFolders();
        MoveAssetIfNeeded(SourceNightScene, NightScene);
        MoveAssetIfNeeded(SourceEnvironment, NightEnvironment);
        MoveAssetIfNeeded(SourcePlayerFbx, PlayerFbx);
        AssetDatabase.Refresh();

        ConfigureWalkImport();
        RuntimeAnimatorController animatorController = EnsureAnimatorController();
        GameObject playerVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerFbx);
        if (playerVisual == null) throw new InvalidOperationException("Player FBX could not be loaded after migration.");

        ConfigureExteriorScene(playerVisual, animatorController);
        ConfigureNightScene(playerVisual, animatorController);
        ConfigureFinalScene(playerVisual, animatorController);
        DeleteLegacyNightAssetsWhenUnused();
        UpdateBuildRoute();

        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.Refresh();
        Debug.Log("Playable scene migration applied: exterior chase, protected alleys route, final gate outcome, and animated player visual.");
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

        if (issues.Count > 0)
        {
            throw new InvalidOperationException("Playable scene validation failed:\n- " + string.Join("\n- ", issues));
        }

        Debug.Log("Playable scene validation passed.");
    }

    private static void ConfigureExteriorScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(ExteriorScene, OpenSceneMode.Single);
        GameObject player = EnsurePlayer(new Vector3(-1.4f, 1f, -18f), false, visualPrefab, animatorController);
        EnsureCamera(player.transform, SharedCameraOffset, SharedCameraLookAtHeight, SharedCameraFieldOfView);
        EnsureFloor("FloorCollider", new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f));
        EnsureSolidEnvironmentColliders();

        LightFragmentPickup fragment = RequireComponent<LightFragmentPickup>("LIGHT_Fragment_01");
        fragment.Configure(LightFragmentPickup.FragmentKind.Exterior);

        LocationTransition transition = RequireComponent<LocationTransition>("BUILDING_LivingSquare_Entrance");
        transition.Configure("LOCATION_02_PROTECTED_ALLEYS_NIGHT", true, true, false);

        GameObject respawn = EnsureMarker("ExteriorRespawn", new Vector3(-1.4f, 1f, -18f));
        ExteriorHuntController hunt = EnsureComponent<ExteriorHuntController>(EnsureMarker("ExteriorHunt", Vector3.zero));
        SetReference(hunt, "player", player.GetComponent<PlayerController3D>());
        SetReference(hunt, "playerAttack", player.GetComponent<PlayerAttackController>());
        SetReference(hunt, "respawnPoint", respawn.transform);
        SetReference(hunt, "exteriorFragment", fragment);

        EnsurePursuer("SHADOW_Queue_01");
        EnsurePursuer("SHADOW_Witness_01");
        SnapActorsToFloor<ExteriorPursuer>();
        EnsureNavigation("ExteriorNavigation", new Vector3(0f, -0.05f, -4f), new Vector3(10f, 0.1f, 30f));
        EditorSceneManager.SaveScene(scene);
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
        SnapActorsToFloor<PrototypeShadowActor>();
        EnsureShadowJumpers();

        DestroyIfPresent("PRICE_Altar");
        EnsureComponent<NightPhaseController>(EnsureMarker("NightPhaseController", Vector3.zero));

        ShadowNPC pleading = RequireComponent<ShadowNPC>("SHADOW_Pleading_01");
        pleading.Configure("SHADOW_PLEADING", new[] { "Не гонись за мной ради удара. Смотри, что случится дальше." }, false);
        EnsureComponent<ShadowNPC>(RequireObject("SHADOW_Afraid_01")).ConfigureNightReaction(
            "SHADOW_AFRAID",
            "Не подходи резко. Я все еще пытаюсь встать.",
            "Ты не ударил. Значит, здесь еще можно подняться.",
            "Я видел, что ты сделал. Не называй это выходом.");
        EnsureComponent<ShadowNPC>(RequireObject("SHADOW_Ally_01")).ConfigureNightReaction(
            "SHADOW_ALLY",
            "К воротам ведет улица, но выбор идет рядом с тобой.",
            "Фрагмент появился не из смерти. Сохрани это до ворот.",
            "Врата узнают, сколько теней ты оставил лежать.");

        GameObject fragmentObject = EnsureSphere("FRAGMENT_InnerNight", new Vector3(-5.8f, -0.2f, 29f), 0.55f);
        LightFragmentPickup fragment = EnsureComponent<LightFragmentPickup>(fragmentObject);
        fragment.Configure(LightFragmentPickup.FragmentKind.InnerNight);
        EnsureTrigger(fragmentObject, Vector3.one * 1.35f);
        fragmentObject.SetActive(false);

        GameObject witness = EnsureMarker("MERCY_WITNESS_Trigger", new Vector3(-9f, -0.17f, 19f));
        EnsureTrigger(witness, new Vector3(9f, 2.5f, 5f));
        NightFragmentEncounter encounter = EnsureComponent<NightFragmentEncounter>(witness);
        SetReference(encounter, "innerNightFragment", fragment);
        SetReference(encounter, "mercyDropPoint", fragmentObject.transform);

        GameObject nonStep = RequireObject("NON_STEP_Trigger");
        nonStep.transform.position = new Vector3(10f, -0.17f, 22f);

        LocationTransition transition = RequireComponent<LocationTransition>("EXIT_To_FinalGate_Exit");
        transition.Configure("LOCATION_03_GATE_FINAL", false, true, true);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureFinalScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(FinalScene, OpenSceneMode.Single);
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

        EnsureGuardian("GUARDIAN_Force", "GUARDIAN_FORCE", true);
        EnsureGuardian("GUARDIAN_Memory", "GUARDIAN_MEMORY", false);
        SnapActorsToFloor<GuardianController>();
        EnsureNavigation("FinalNavigation", new Vector3(0f, 0.15f, -2f), new Vector3(20f, 0.1f, 30f));
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
        PlayerController3D movement = EnsureComponent<PlayerController3D>(player);
        movement.ConfigureLocomotion(SharedPlayerMoveSpeed, SharedPlayerRotationSpeed);
        movement.ConfigureTraversal(SharedPlayerJumpEnabled);
        PlayerAttackController attack = EnsureComponent<PlayerAttackController>(player);
        attack.SetSceneAttackEnabled(canAttack);
        EnsureComponent<InteractionController>(player);

        Renderer placeholder = player.GetComponent<Renderer>();
        if (placeholder != null) placeholder.enabled = false;

        GameObject visual = FindObject("Fragmento_Walk");
        if (visual == null || visual.transform.parent == null || visual.transform.parent.gameObject != player)
        {
            if (visual == null)
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
                visual.name = "Fragmento_Walk";
            }

            visual.transform.SetParent(player.transform, false);
        }

        visual.transform.localPosition = new Vector3(0f, -1f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        CenterVisualOverPlayer(player, visual);
        Animator animator = EnsureComponent<Animator>(visual);
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
        agent.baseOffset = 0f;
        EnsureComponent<EnemyJumpController>(shadow).Configure(true);
    }

    private static void EnsureGuardian(string objectName, string displayName, bool isForce)
    {
        GameObject guardian = RequireObject(objectName);
        GuardianController controller = EnsureComponent<GuardianController>(guardian);
        controller.Configure(displayName, isForce);
        NavMeshAgent agent = EnsureComponent<NavMeshAgent>(guardian);
        agent.radius = 0.4f;
        agent.height = 2f;
        agent.baseOffset = 0f;
        EnsureComponent<EnemyJumpController>(guardian).Configure(true);
    }

    private static void EnsureShadowJumpers()
    {
        foreach (PrototypeShadowActor shadow in UnityEngine.Object.FindObjectsByType<PrototypeShadowActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            EnsureComponent<EnemyJumpController>(shadow.gameObject).Configure(true);
        }
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
            Renderer renderer = actor.GetComponent<Renderer>();
            if (renderer == null) continue;

            Vector3 position = actor.transform.position;
            position.y += floorTop - renderer.bounds.min.y;
            actor.transform.position = position;
        }
    }

    private static void CenterVisualOverPlayer(GameObject player, GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offset = bounds.center - player.transform.position;
        visual.transform.position -= new Vector3(offset.x, 0f, offset.z);
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

    private static RuntimeAnimatorController EnsureAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
        if (controller != null && controller.layers.Length == 0)
        {
            AssetDatabase.DeleteAsset(PlayerController);
            controller = null;
        }

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerController);
        }

        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(PlayerFbx)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        if (clip == null) throw new InvalidOperationException("Fragmento_Walk has no importable animation clip.");

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
        {
            machine.RemoveState(child.state);
        }

        AnimatorState state = machine.AddState("Walk");
        state.motion = clip;
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConfigureWalkImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(PlayerFbx) as ModelImporter;
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = "Walk";
            clip.loopTime = true;
            clip.loopPose = true;
        }

        importer.clipAnimations = clips;
        // The imported rig has duplicate humanoid bone names; its native generic clip is valid.
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importCameras = false;
        importer.importLights = false;
        importer.SaveAndReimport();
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
            PlayerController3D movement = player.GetComponent<PlayerController3D>();
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

            Renderer visual = player.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(renderer => renderer.gameObject != player);
            if (visual != null)
            {
                Vector3 centerOffset = visual.bounds.center - player.transform.position;
                centerOffset.y = 0f;
                if (centerOffset.magnitude > 0.08f)
                {
                    issues.Add(Path.GetFileName(path) + " has off-center player visual.");
                }

                if (floor != null && Mathf.Abs(visual.bounds.min.y - floor.bounds.max.y) > 0.08f)
                {
                    issues.Add(Path.GetFileName(path) + " has player visual above or below the floor.");
                }
            }
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
            ValidateEnemyJumpers<ExteriorPursuer>(path, issues);
        }

        if (path == NightScene)
        {
            ValidateActorPlacement<PrototypeShadowActor>(path, issues, 0.08f);
            ValidateEnemyJumpers<PrototypeShadowActor>(path, issues);
        }

        if (path == FinalScene)
        {
            ValidateActorPlacement<GuardianController>(path, issues, 0.08f);
            ValidateEnemyJumpers<GuardianController>(path, issues);
        }
    }

    private static void ValidateSolidEnvironmentColliders(string path, List<string> issues)
    {
        List<string> missing = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(meshFilter => IsSolidEnvironmentMesh(meshFilter.name) &&
                                 meshFilter.sharedMesh != null &&
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
        Collider floor = FindObject("FloorCollider")?.GetComponent<Collider>();
        if (floor == null) return;

        float floorTop = floor.bounds.max.y;
        foreach (T actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Renderer renderer = actor.GetComponent<Renderer>();
            if (renderer == null) continue;

            float distance = Mathf.Abs(renderer.bounds.min.y - floorTop);
            if (distance > tolerance)
            {
                issues.Add(Path.GetFileName(path) + " has misplaced actor " + actor.name + ".");
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

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art/Characters");
        EnsureFolder(PlayerFolder);
        EnsureFolder(NightArtFolder);
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

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object reference)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException("Serialized field missing: " + propertyName);
        property.objectReferenceValue = reference;
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
}
