using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneBootstrap
{
    private const string SceneExteriorDay = "LOCATION_01_EXTERIOR_DAY";
    private const string SceneInnerNight = "LOcate2";
    private const string LegacySceneInnerNight = "LOCATION_02_INNER_NIGHT_SQUARE";
    private const string SceneGateFinal = "LOCATION_03_GATE_FINAL";

    private static readonly Vector3 FinalPlayerSpawn = new Vector3(0f, 1f, -12f);
    private static readonly Vector3 FinalGateFocusPoint = new Vector3(0f, 1.4f, 7.4f);
    private static readonly Vector3 FinalCameraOffset = new Vector3(0f, 6f, -12f);

    private static Material dayMarkerMaterial;
    private static Material nightMarkerMaterial;
    private static Material shadowMaterial;
    private static Material lightMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene);
    }

    private static void ApplyScene(Scene scene)
    {
        EnsureCoreManagers();

        if (scene.name == SceneExteriorDay)
        {
            ConfigureExteriorDay();
            return;
        }

        if (scene.name == SceneInnerNight || scene.name == LegacySceneInnerNight)
        {
            ConfigureInnerNight();
            return;
        }

        if (scene.name == SceneGateFinal)
        {
            ConfigureGateFinal();
        }
    }

    private static void EnsureCoreManagers()
    {
        DialogueController.EnsureInstance();

        if (WorldState.Instance == null && Object.FindFirstObjectByType<WorldState>() == null)
        {
            GameObject managers = GameObject.Find("Managers") ?? new GameObject("Managers_Runtime");
            managers.AddComponent<WorldState>();
        }

        if (GameFlowController.Instance == null && Object.FindFirstObjectByType<GameFlowController>() == null)
        {
            GameObject managers = GameObject.Find("Managers") ?? new GameObject("Managers_Runtime");
            managers.AddComponent<GameFlowController>();
        }
    }

    private static void ConfigureExteriorDay()
    {
        GameObject player = EnsurePlayer(new Vector3(-1.4f, 1f, -18f), Vector3.forward);
        ConfigureCamera(player.transform, new Vector3(0f, 10f, -15f), 1.4f, 58f);
        EnsureFloor("FloorCollider", Vector3.zero, new Vector3(80f, 1f, 80f));
        EnsureEnvironmentMeshColliders();

        EnsurePathMarker("TRACE_Path_01", new Vector3(0f, 0.04f, -13f));
        EnsurePathMarker("TRACE_Path_02", new Vector3(0f, 0.04f, -7f));
        EnsurePathMarker("TRACE_Path_03", new Vector3(0f, 0.04f, -1f));
        EnsurePathMarker("TRACE_Path_04", new Vector3(0f, 0.04f, 5f));

        ConfigureShadowNpc(
            "SHADOW_Queue_01",
            new Vector3(-2.7f, 1f, -5.5f),
            "SHADOW_QUEUE_01",
            new[]
            {
                "Ты не из очереди.",
                "Не толкайся. Здесь ждут те, кому хотя бы обещали место.",
                "У тебя даже имени нет."
            },
            false);

        ConfigureShadowNpc(
            "SHADOW_Witness_01",
            new Vector3(2.7f, 1f, -1.5f),
            "POINTING_WITNESS",
            new[]
            {
                "Смотри. Он думает, что идет.",
                "Он даже не заметил, что квадрат уже поменял правило.",
                "Мы помним тебя. Не полностью. Но достаточно, чтобы поднять цену."
            },
            false);

        GameObject light = EnsurePrimitive("LIGHT_Fragment_01", PrimitiveType.Sphere, new Vector3(-1.8f, 0.75f, -10f), Vector3.one * 0.65f, GetLightMaterial());
        GetOrAdd<LightFragmentPickup>(light);
        EnsureTriggerCollider(light, new Vector3(1.4f, 1.4f, 1.4f));

        GameObject entrance = EnsurePrimitive("BUILDING_LivingSquare_Entrance", PrimitiveType.Cube, new Vector3(0f, 1.2f, 9f), new Vector3(3.2f, 2.4f, 0.8f), GetDayMarkerMaterial());
        LocationTransition transition = GetOrAdd<LocationTransition>(entrance);
        transition.Configure(SceneInnerNight, true, true);
        EnsureTriggerCollider(entrance, new Vector3(3.2f, 2.4f, 1.4f));

        if (WorldState.Instance != null && WorldState.Instance.cycleCount == 0 && DialogueController.Instance != null)
        {
            DialogueController.Instance.ShowDialogue("SYSTEM", "Протокол квадрата активен. Фрагмент обнаружен. Имя: отсутствует. Цена будет названа позже.");
        }
    }

    private static void ConfigureInnerNight()
    {
        GameObject player = EnsurePlayer(new Vector3(0f, 1f, -12f), Vector3.forward);
        ConfigureCamera(player.transform, new Vector3(0f, 9f, -13f), 1.4f, 60f);
        EnsureFloor("FloorCollider", Vector3.zero, new Vector3(80f, 1f, 80f));
        
        // Fix visuals and collisions
        MakeEnvironmentDoubleSided("Location02_OldTownNightSquare");
        EnsureEnvironmentMeshColliders();
        CleanUpStreetObstacles("Location02_OldTownNightSquare");
        EnsureInnerNightCollisionVolumes();

        if (Object.FindFirstObjectByType<NightPhaseController>() == null)
        {
            new GameObject("NightPhaseController").AddComponent<NightPhaseController>();
        }

        GameObject altar = EnsurePrimitive("PRICE_Altar", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 5f), new Vector3(1.8f, 0.7f, 1.8f), GetNightMarkerMaterial());
        GetOrAdd<PriceAltar>(altar);
        EnsureTriggerCollider(altar, new Vector3(2.2f, 1.4f, 2.2f));

        ConfigureShadowNpc(
            "SHADOW_Pleading_01",
            new Vector3(-4f, 1f, -2f),
            "SHADOW_PLEADING",
            new[]
            {
                "Помоги мне до того, как они решат, что я полезна.",
                "Я не прошу спасения. Только один лишний цикл."
            },
            true);

        ConfigureShadowNpc(
            "SHADOW_Observer_01",
            new Vector3(4f, 1f, -2f),
            "SHADOW_OBSERVER",
            new[]
            {
                "Квадрат помнит твои шаги, даже если ты нет.",
                "Цена всегда выше ночью. Ты готов платить?"
            },
            false);

        ConfigureShadowNpc(
            "SHADOW_Whisperer_01",
            new Vector3(0f, 1f, 2f),
            "SHADOW_WHISPERER",
            new[]
            {
                "Не смотри в окна. Они пусты не просто так.",
                "Ты первый Фрагмент, добравшийся до этого цикла."
            },
            false);

        ConfigureCombatShadow("SHADOW_Enemy_01", new Vector3(6f, 1f, 8f), PrototypeShadowActor.ShadowRole.Enemy, 2);
        ConfigureCombatShadow("SHADOW_Enemy_02", new Vector3(-6f, 1f, 8f), PrototypeShadowActor.ShadowRole.Enemy, 2);
        ConfigureCombatShadow("SHADOW_Afraid_01", new Vector3(-5f, 1f, 12f), PrototypeShadowActor.ShadowRole.Afraid, 1);
        ConfigureCombatShadow("SHADOW_Ally_01", new Vector3(5f, 1f, 12f), PrototypeShadowActor.ShadowRole.Ally, 2);

        GameObject nonStep = GameObject.Find("NON_STEP_Trigger") ?? new GameObject("NON_STEP_Trigger");
        nonStep.transform.position = new Vector3(0f, 1f, 10f);
        BoxCollider nonStepCollider = GetOrAdd<BoxCollider>(nonStep);
        nonStepCollider.isTrigger = true;
        nonStepCollider.size = new Vector3(4f, 2f, 2f);
        GetOrAdd<NonStepTrigger>(nonStep);

        GameObject exit = EnsurePrimitive("EXIT_To_FinalGate_Exit", PrimitiveType.Cube, new Vector3(0f, 1.2f, 18f), new Vector3(3f, 2.4f, 0.8f), GetNightMarkerMaterial());
        LocationTransition transition = GetOrAdd<LocationTransition>(exit);
        transition.Configure(SceneGateFinal, false, false);
        EnsureTriggerCollider(exit, new Vector3(3.2f, 2.4f, 1.4f));

        ApplyProtectedAlleysLayoutIfPresent();
    }

    private static void ApplyProtectedAlleysLayoutIfPresent()
    {
        string environmentRootName = FindProtectedAlleysRootName();
        if (string.IsNullOrEmpty(environmentRootName)) return;

        const float groundY = -1.17f;
        GameObject player = EnsurePlayer(new Vector3(-11.46f, -0.17f, 1.5f), Vector3.forward);
        ConfigureCamera(player.transform, new Vector3(0f, 10f, -15f), 1.4f, 60f);
        EnsureFloor("FloorCollider", new Vector3(-13.55f, groundY - 0.5f, 19.81f), new Vector3(41.5f, 1f, 47.5f));
        DisableRendererIfPresent("FloorCollider");

        MakeEnvironmentDoubleSided(environmentRootName);
        CleanUpStreetObstacles(environmentRootName);
        EnsureProtectedAlleysCollisionVolumes(groundY);

        MoveObjectIfPresent("PRICE_Altar", new Vector3(0.04f, -0.47f, 4f));
        MoveObjectIfPresent("SHADOW_Pleading_01", new Vector3(-2.46f, -0.17f, 4.5f));
        MoveObjectIfPresent("SHADOW_Observer_01", new Vector3(-14.2f, -0.17f, 10f));
        MoveObjectIfPresent("SHADOW_Whisperer_01", new Vector3(-9f, -0.17f, 16f));
        MoveObjectIfPresent("SHADOW_Enemy_01", new Vector3(-13.6f, -0.17f, 22f));
        MoveObjectIfPresent("SHADOW_Enemy_02", new Vector3(-9.2f, -0.17f, 25f));
        MoveObjectIfPresent("SHADOW_Afraid_01", new Vector3(-5.8f, -0.17f, 29f));
        MoveObjectIfPresent("SHADOW_Ally_01", new Vector3(1.5f, -0.17f, 30f));
        MoveObjectIfPresent("NON_STEP_Trigger", new Vector3(1f, -0.17f, 30.5f));
        MoveObjectIfPresent("EXIT_To_FinalGate_Exit", new Vector3(7.04f, 0.03f, 31.1f));
    }

    private static string FindProtectedAlleysRootName()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name.StartsWith("Virus9_OldTown_ProtectedAlleys_Blockout2"))
            {
                return roots[i].name;
            }
        }

        return string.Empty;
    }

    private static void MoveObjectIfPresent(string objectName, Vector3 position)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null) obj.transform.position = position;
    }

    private static void DisableRendererIfPresent(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        Renderer renderer = obj != null ? obj.GetComponent<Renderer>() : null;
        if (renderer != null) renderer.enabled = false;
    }

    private static void MakeEnvironmentDoubleSided(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) return;

        Dictionary<Material, Material> runtimeMaterials = new Dictionary<Material, Material>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null) continue;

                if (!runtimeMaterials.TryGetValue(source, out Material runtimeMaterial))
                {
                    runtimeMaterial = new Material(source)
                    {
                        name = source.name + " (Runtime Double Sided)"
                    };
                    runtimeMaterial.SetFloat("_Cull", 0f);
                    runtimeMaterial.doubleSidedGI = true;
                    runtimeMaterials.Add(source, runtimeMaterial);
                }

                materials[i] = runtimeMaterial;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private static void CleanUpStreetObstacles(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) return;

        foreach (Transform child in root.transform)
        {
            if (!IsNonBlockingDecoration(child.gameObject)) continue;

            Collider[] colliders = child.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private static void ConfigureGateFinal()
    {
        // Final gate approach is normalized after the 180-degree scene flip:
        // player/camera stay on -Z, while the gate, trigger, and guardians sit ahead on +Z.
        GameObject player = EnsurePlayer(FinalPlayerSpawn, DirectionToward(FinalPlayerSpawn, FinalGateFocusPoint));
        ConfigureCamera(player.transform, FinalCameraOffset, 1.5f, 55f);
        EnsureFloor("FloorCollider", new Vector3(0f, -0.5f, 0f), new Vector3(42f, 1f, 42f));
        EnsureEnvironmentMeshColliders();
        EnsureGateFinalCollisionVolumes();

        ConfigureGuardian("GUARDIAN_Force", new Vector3(6.9f, 1f, 5.8f), "GUARDIAN_FORCE", true);
        ConfigureGuardian("GUARDIAN_Memory", new Vector3(-6.9f, 1f, 5.8f), "GUARDIAN_MEMORY", false);
    }

    private static GameObject EnsurePlayer(Vector3 position, Vector3 forward)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player_Fragment");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Fragment";
        }

        TrySetTag(player, "Player");
        Quaternion rotation = Quaternion.LookRotation(SafeHorizontalForward(forward), Vector3.up);

        Rigidbody rb = GetOrAdd<Rigidbody>(player);
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        player.transform.SetPositionAndRotation(position, rotation);

        GetOrAdd<CapsuleCollider>(player);
        GetOrAdd<PlayerController3D>(player);
        GetOrAdd<InteractionController>(player);
        GetOrAdd<PlayerAttackController>(player);

        return player;
    }

    private static void ConfigureCamera(Transform target, Vector3 offset, float lookAtHeight, float fieldOfView)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            TrySetTag(cameraObject, "MainCamera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.fieldOfView = fieldOfView;
        PrototypeCameraFollow follow = GetOrAdd<PrototypeCameraFollow>(camera.gameObject);
        follow.Configure(target, offset, lookAtHeight);
    }

    private static Vector3 DirectionToward(Vector3 from, Vector3 to)
    {
        return SafeHorizontalForward(to - from);
    }

    private static Vector3 SafeHorizontalForward(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return Vector3.forward;
        return direction.normalized;
    }

    private static void ConfigureShadowNpc(string objectName, Vector3 position, string npcName, string[] lines, bool hasChoices)
    {
        GameObject shadow = EnsurePrimitive(objectName, PrimitiveType.Sphere, position, new Vector3(0.9f, 1.4f, 0.9f), GetShadowMaterial());
        ShadowNPC npc = GetOrAdd<ShadowNPC>(shadow);
        npc.Configure(npcName, lines, hasChoices);
        PrototypeShadowActor actor = GetOrAdd<PrototypeShadowActor>(shadow);
        actor.Configure(hasChoices ? PrototypeShadowActor.ShadowRole.Ally : PrototypeShadowActor.ShadowRole.Neutral, 2);
        EnsureTriggerCollider(shadow, new Vector3(1.8f, 2.2f, 1.8f));
    }

    private static void ConfigureCombatShadow(string objectName, Vector3 position, PrototypeShadowActor.ShadowRole role, int health)
    {
        GameObject shadow = EnsurePrimitive(objectName, PrimitiveType.Sphere, position, new Vector3(1f, 1.5f, 1f), GetShadowMaterial());
        PrototypeShadowActor actor = GetOrAdd<PrototypeShadowActor>(shadow);
        actor.Configure(role, health);
        EnsureTriggerCollider(shadow, new Vector3(1.7f, 2f, 1.7f));
    }

    private static void ConfigureGuardian(string objectName, Vector3 position, string guardianName, bool forceGuardian)
    {
        GameObject guardian = GameObject.Find(objectName);
        if (guardian == null)
        {
            guardian = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardian.name = objectName;
        }

        guardian.transform.position = position;
        guardian.transform.rotation = Quaternion.LookRotation(DirectionToward(position, FinalPlayerSpawn), Vector3.up);
        guardian.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);

        Renderer renderer = guardian.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = !HasImportedFinalGateVisuals();

        GuardianController controller = GetOrAdd<GuardianController>(guardian);
        controller.Configure(guardianName, forceGuardian);

        Collider[] existingColliders = guardian.GetComponents<Collider>();
        for (int i = 0; i < existingColliders.Length; i++)
        {
            existingColliders[i].isTrigger = true;
        }

        BoxCollider collider = GetOrAdd<BoxCollider>(guardian);
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 0.7f, 0f);
        collider.size = new Vector3(2.2f, 3.2f, 2.2f);
    }

    private static bool HasImportedFinalGateVisuals()
    {
        return GameObject.Find("Location03_GateFinal") != null
            || GameObject.Find("Atmos_Door_Left") != null
            || GameObject.Find("Guardian_Cloak_L") != null;
    }

    private static GameObject EnsurePrimitive(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
        {
            obj = GameObject.CreatePrimitive(type);
            obj.name = objectName;
        }

        obj.transform.position = position;
        obj.transform.localScale = scale;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return obj;
    }

    private static void EnsurePathMarker(string objectName, Vector3 position)
    {
        GameObject marker = EnsurePrimitive(objectName, PrimitiveType.Cube, position, new Vector3(1.8f, 0.05f, 1.8f), GetDayMarkerMaterial());
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

    private static void EnsureFloor(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject floor = GameObject.Find(objectName);
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = objectName;
        }

        floor.transform.position = position;
        floor.transform.localScale = scale;
    }

    private static void EnsureInnerNightCollisionVolumes()
    {
        // World boundaries
        EnsureSolidBox("COL_L2_BackBoundary", new Vector3(0f, 2f, -25f), new Vector3(50f, 4f, 1f));
        EnsureSolidBox("COL_L2_FrontBoundary", new Vector3(0f, 2f, 25f), new Vector3(50f, 4f, 1f));
        EnsureSolidBox("COL_L2_LeftBoundary", new Vector3(-20f, 2f, 0f), new Vector3(1f, 4f, 50f));
        EnsureSolidBox("COL_L2_RightBoundary", new Vector3(20f, 2f, 0f), new Vector3(1f, 4f, 50f));

        // Thick blocks to prevent entering houses
        // East Row
        EnsureSolidBox("COL_L2_EastHouseBlock", new Vector3(-12f, 2f, 0f), new Vector3(12f, 4f, 50f));
        // West Row
        EnsureSolidBox("COL_L2_WestHouseBlock", new Vector3(12f, 2f, 0f), new Vector3(12f, 4f, 50f));
        // North Cross (Back side)
        EnsureSolidBox("COL_L2_NorthHouseBlock", new Vector3(0f, 2f, -20f), new Vector3(50f, 4f, 10f));
        // South Cross (Front side near exit)
        EnsureSolidBox("COL_L2_SouthHouseBlock", new Vector3(0f, 2f, 20f), new Vector3(50f, 4f, 10f));
    }

    private static void EnsureProtectedAlleysCollisionVolumes(float groundY)
    {
        float wallY = groundY + 2f;
        EnsureSolidBox("COL_L2_BackBoundary", new Vector3(-11.46f, wallY, -5.25f), new Vector3(62f, 4f, 1f));
        EnsureSolidBox("COL_L2_FrontBoundary", new Vector3(-11.46f, wallY, 44.25f), new Vector3(62f, 4f, 1f));
        EnsureSolidBox("COL_L2_LeftBoundary", new Vector3(-40.75f, wallY, 19.5f), new Vector3(1f, 4f, 50f));
        EnsureSolidBox("COL_L2_RightBoundary", new Vector3(17.85f, wallY, 19.5f), new Vector3(1f, 4f, 50f));

        DisableCollisionObject("COL_L2_EastHouseBlock");
        DisableCollisionObject("COL_L2_WestHouseBlock");
        DisableCollisionObject("COL_L2_NorthHouseBlock");
        DisableCollisionObject("COL_L2_SouthHouseBlock");
    }

    private static void DisableCollisionObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null) return;

        Collider[] colliders = obj.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static void EnsureGateFinalCollisionVolumes()
    {
        EnsureSolidBox("COL_L3_BackBoundary", new Vector3(0f, 2f, -19f), new Vector3(22f, 4f, 1f));
        EnsureSolidBox("COL_L3_GateBackstop", new Vector3(0f, 2f, 14f), new Vector3(14f, 4f, 1f));
        EnsureSolidBox("COL_L3_LeftBoundary", new Vector3(-11f, 2f, -2.5f), new Vector3(1f, 4f, 33f));
        EnsureSolidBox("COL_L3_RightBoundary", new Vector3(11f, 2f, -2.5f), new Vector3(1f, 4f, 33f));
    }

    private static void EnsureSolidBox(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject box = GameObject.Find(objectName);
        if (box == null)
        {
            box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = objectName;
        }

        box.transform.SetPositionAndRotation(position, Quaternion.identity);
        box.transform.localScale = scale;

        BoxCollider collider = GetOrAdd<BoxCollider>(box);
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = Vector3.one;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void EnsureEnvironmentMeshColliders()
    {
        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.sharedMesh == null) continue;
            GameObject obj = filter.gameObject;
            if (!ShouldAddEnvironmentCollider(obj)) continue;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.y < 0.25f && bounds.size.x < 0.75f && bounds.size.z < 0.75f) continue;

            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.convex = false;
            obj.isStatic = true;
        }
    }

    private static bool ShouldAddEnvironmentCollider(GameObject obj)
    {
        if (obj == null) return false;
        if (IsNonBlockingDecoration(obj)) return false;
        if (obj.GetComponent<Collider>() != null) return false;
        if (obj.GetComponentInParent<Interactable>() != null) return false;
        if (obj.GetComponentInParent<PlayerController3D>() != null) return false;
        if (obj.GetComponentInParent<Rigidbody>() != null) return false;
        if (obj.GetComponentInParent<Camera>() != null) return false;
        if (obj.GetComponentInParent<Light>() != null) return false;
        if (obj.GetComponentInParent<Canvas>() != null) return false;
        if (obj.name.StartsWith("TRACE_")) return false;
        if (obj.name.StartsWith("COL_")) return false;

        return true;
    }

    private static bool IsNonBlockingDecoration(GameObject obj)
    {
        string name = obj.name.ToUpperInvariant();
        return name.Contains("LAMP")
            || name.Contains("SOCKET")
            || name.Contains("WINDOW_FRAME")
            || name.Contains("PUDDLE")
            || name.Contains("STAIN")
            || name.Contains("MOSS")
            || name.Contains("VIS_")
            || name.Contains("SOUL_")
            || name.Contains("ZONE_")
            || name.Contains("CAM_");
    }

    private static void EnsureTriggerCollider(GameObject obj, Vector3 size)
    {
        Collider[] existingColliders = obj.GetComponents<Collider>();
        for (int i = 0; i < existingColliders.Length; i++)
        {
            existingColliders[i].isTrigger = true;
        }

        BoxCollider collider = GetOrAdd<BoxCollider>(obj);
        collider.isTrigger = true;
        collider.size = size;
        collider.center = Vector3.zero;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null) component = obj.AddComponent<T>();
        return component;
    }

    private static void TrySetTag(GameObject obj, string tag)
    {
        try
        {
            obj.tag = tag;
        }
        catch (UnityException)
        {
            // The default Player/MainCamera tags exist in Unity projects, but keep runtime resilient.
        }
    }

    private static Material GetDayMarkerMaterial()
    {
        return dayMarkerMaterial ??= CreateMaterial("Runtime_DayPathMarker", new Color(0.25f, 0.5f, 0.55f, 0.7f));
    }

    private static Material GetNightMarkerMaterial()
    {
        return nightMarkerMaterial ??= CreateMaterial("Runtime_NightMarker", new Color(0.18f, 0.08f, 0.25f, 0.85f));
    }

    private static Material GetShadowMaterial()
    {
        return shadowMaterial ??= CreateMaterial("Runtime_ShadowPlaceholder", new Color(0.08f, 0.08f, 0.1f, 1f));
    }

    private static Material GetLightMaterial()
    {
        return lightMaterial ??= CreateMaterial("Runtime_LightFragment", new Color(0.8f, 0.95f, 1f, 1f));
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = materialName,
            color = color
        };
    }
}
