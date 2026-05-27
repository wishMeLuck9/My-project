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
        EnsureCamera(player.transform, new Vector3(0f, 10f, -15f), 1.4f, 58f);
        EnsureFloor("FloorCollider", Vector3.zero, new Vector3(80f, 1f, 80f));

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
        EnsureNavigation("ExteriorNavigation", new Vector3(0f, -0.55f, -4f), new Vector3(10f, 0.1f, 30f));
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureNightScene(GameObject visualPrefab, RuntimeAnimatorController animatorController)
    {
        Scene scene = EditorSceneManager.OpenScene(NightScene, OpenSceneMode.Single);

        GameObject environment = FindObject("Virus9_OldTown_ProtectedAlleys_Blockout2_before_origin_to_geometry_20260526_162356")
            ?? FindObject("Location02_ProtectedAlleysNight");
        if (environment != null) environment.name = "Location02_ProtectedAlleysNight";

        GameObject player = EnsurePlayer(new Vector3(-11.46f, -0.17f, 1.5f), true, visualPrefab, animatorController);
        EnsureCamera(player.transform, new Vector3(0f, 5.8f, -8.5f), 1.1f, 52f);
        EnsureFloor("FloorCollider", new Vector3(-13.55f, -1.67f, 19.81f), new Vector3(41.5f, 1f, 47.5f));
        EnsureBox("COL_L2_BackBoundary", new Vector3(-11.46f, 0.83f, -5.25f), new Vector3(62f, 4f, 1f));
        EnsureBox("COL_L2_FrontBoundary", new Vector3(-11.46f, 0.83f, 44.25f), new Vector3(62f, 4f, 1f));
        EnsureBox("COL_L2_LeftBoundary", new Vector3(-40.75f, 0.83f, 19.5f), new Vector3(1f, 4f, 50f));
        EnsureBox("COL_L2_RightBoundary", new Vector3(17.85f, 0.83f, 19.5f), new Vector3(1f, 4f, 50f));
        EnsureNightArchitectureColliders();

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
        GameObject player = EnsurePlayer(new Vector3(0f, 1f, -12f), true, visualPrefab, animatorController);
        EnsureCamera(player.transform, new Vector3(0f, 6f, -12f), 1.5f, 55f);
        EnsureFloor("FloorCollider", new Vector3(0f, -0.5f, 0f), new Vector3(42f, 1f, 42f));
        EnsureBox("COL_L3_BackBoundary", new Vector3(0f, 2f, -19f), new Vector3(22f, 4f, 1f));
        EnsureBox("COL_L3_GateBackstop", new Vector3(0f, 2f, 14f), new Vector3(14f, 4f, 1f));
        EnsureBox("COL_L3_LeftBoundary", new Vector3(-11f, 2f, -2.5f), new Vector3(1f, 4f, 33f));
        EnsureBox("COL_L3_RightBoundary", new Vector3(11f, 2f, -2.5f), new Vector3(1f, 4f, 33f));

        GameObject evaluatorObject = RequireObject("FinalEvaluator");
        FinalStateEvaluator previousEvaluator = evaluatorObject.GetComponent<FinalStateEvaluator>();
        if (previousEvaluator != null) UnityEngine.Object.DestroyImmediate(previousEvaluator);
        FinalGateOutcomeController outcome = EnsureComponent<FinalGateOutcomeController>(evaluatorObject);

        GameObject respawn = EnsureMarker("FinalArenaRespawn", new Vector3(0f, 1f, -12f));
        SetReference(outcome, "arenaRespawnPoint", respawn.transform);

        EnsureGuardian("GUARDIAN_Force", "GUARDIAN_FORCE", true);
        EnsureGuardian("GUARDIAN_Memory", "GUARDIAN_MEMORY", false);
        EnsureNavigation("FinalNavigation", new Vector3(0f, -0.55f, -2f), new Vector3(20f, 0.1f, 30f));
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
        EnsureComponent<PlayerController3D>(player);
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
        Animator animator = EnsureComponent<Animator>(visual);
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false;
        EnsureComponent<PlayerVisualAnimator>(visual);
        return player;
    }

    private static void EnsureCamera(Transform player, Vector3 offset, float lookAtHeight, float fov)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

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

    private static void EnsureNightArchitectureColliders()
    {
        foreach (MeshFilter meshFilter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string objectName = meshFilter.name;
            bool isArchitecture = objectName.StartsWith("ARCH_", StringComparison.Ordinal) &&
                (objectName.Contains("_OldStoneBody", StringComparison.Ordinal) ||
                 objectName.Contains("_StoneThreshold", StringComparison.Ordinal) ||
                 objectName.Contains("_Door_Heavy", StringComparison.Ordinal));
            bool isSolidProp = objectName.Contains("_TinySanctuary", StringComparison.Ordinal) ||
                objectName.Contains("_TinyFountain_Basin", StringComparison.Ordinal);
            if ((!isArchitecture && !isSolidProp) || meshFilter.sharedMesh == null) continue;

            MeshCollider collider = EnsureComponent<MeshCollider>(meshFilter.gameObject);
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            meshFilter.gameObject.isStatic = true;
        }
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

        if (path == NightScene)
        {
            int architectureColliders = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(collider => collider.name.StartsWith("ARCH_", StringComparison.Ordinal));
            if (architectureColliders < 5)
            {
                issues.Add(Path.GetFileName(path) + " lacks collision on night architecture.");
            }
        }
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
