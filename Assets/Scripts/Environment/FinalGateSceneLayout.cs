using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalGateSceneLayout
{
    private const string FinalSceneName = "LOCATION_03_GATE_FINAL";
    private static Material supportMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyIfFinalScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyIfFinalScene(scene);
    }

    private static void ApplyIfFinalScene(Scene scene)
    {
        if (scene.name != FinalSceneName) return;

        if (HasImportedGateVisuals())
        {
            RemoveGeneratedGateSupports();
        }
        else
        {
            EnsureGateSupports();
        }

        EnsureGateEntryTrigger();
    }

    private static bool HasImportedGateVisuals()
    {
        return GameObject.Find("Location03_GateFinal") != null
            || GameObject.Find("Atmos_Door_Left") != null
            || GameObject.Find("Gate_Left_Pier") != null;
    }

    private static void RemoveGeneratedGateSupports()
    {
        RemoveObject("GATE_Support_Left");
        RemoveObject("GATE_Support_Right");
        RemoveObject("GATE_UpperLintel");
        RemoveObject("GATE_Base_Platform");
        RemoveObject("GATE_Step_Front");
    }

    private static void RemoveObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null) return;

        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }

    private static void EnsureGateSupports()
    {
        EnsureSupport("GATE_Support_Left", new Vector3(4.8f, 4f, 7.85f), new Vector3(1.2f, 8f, 1.4f));
        EnsureSupport("GATE_Support_Right", new Vector3(-4.8f, 4f, 7.85f), new Vector3(1.2f, 8f, 1.4f));
        EnsureSupport("GATE_UpperLintel", new Vector3(0f, 8.25f, 7.85f), new Vector3(10.8f, 1.2f, 1.5f));
        EnsureSupport("GATE_Base_Platform", new Vector3(0f, 0.2f, 8.6f), new Vector3(11f, 0.4f, 4.5f));
        EnsureSupport("GATE_Step_Front", new Vector3(0f, 0.45f, 10.7f), new Vector3(8f, 0.35f, 1.2f));
    }

    private static void EnsureSupport(string name, Vector3 position, Vector3 scale)
    {
        GameObject support = GameObject.Find(name);
        if (support == null)
        {
            support = GameObject.CreatePrimitive(PrimitiveType.Cube);
            support.name = name;
        }

        support.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
        support.transform.localScale = scale;

        Renderer renderer = support.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetSupportMaterial();
    }

    private static Material GetSupportMaterial()
    {
        if (supportMaterial != null) return supportMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        supportMaterial = new Material(shader)
        {
            name = "Runtime_GateSupport_DarkStone",
            color = new Color(0.08f, 0.08f, 0.1f, 1f)
        };

        return supportMaterial;
    }

    private static void EnsureGateEntryTrigger()
    {
        GameObject trigger = GameObject.Find("GATE_FinalEntryTrigger");
        if (trigger == null)
        {
            trigger = new GameObject("GATE_FinalEntryTrigger");
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            trigger.AddComponent<FinalGateEntryTrigger>();
        }

        trigger.transform.SetPositionAndRotation(new Vector3(0f, 2f, 6.8f), Quaternion.Euler(0f, 180f, 0f));
        trigger.transform.localScale = Vector3.one;

        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.center = Vector3.zero;
            triggerCollider.size = new Vector3(4.5f, 4f, 1.4f);
            triggerCollider.isTrigger = true;
        }
    }
}
