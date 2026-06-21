using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public static class MenuBootSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Frontend/MENU_BOOT.unity";
    private const string FrontendFolderPath = "Assets/Scenes/Frontend";
    private const string FrontendMenuPrefabPath = "Assets/Resources/UI/FrontendMenu.prefab";
    private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";

    [MenuItem("VIRUS9/Rebuild Menu Boot Scene")]
    public static void BuildMenuBootScene()
    {
        EnsureFrontendFolder();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateSystemsRoot();
        CreateEventSystem();
        CreateMenuCameraRoot();
        CreateUiRoot();

        if (!EditorSceneManager.SaveScene(scene, MenuScenePath))
        {
            throw new InvalidOperationException($"Could not save menu boot scene at {MenuScenePath}.");
        }

        AssetDatabase.ImportAsset(MenuScenePath);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Rebuilt {MenuScenePath} and set it as the first enabled build scene.");
    }

    public static void BuildMenuBootSceneBatch()
    {
        BuildMenuBootScene();
    }

    private static void EnsureFrontendFolder()
    {
        if (AssetDatabase.IsValidFolder(FrontendFolderPath)) return;

        Directory.CreateDirectory(FrontendFolderPath);
        AssetDatabase.Refresh();
    }

    private static void CreateSystemsRoot()
    {
        new GameObject("_SYSTEMS");
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
        if (inputActions != null)
        {
            inputModule.actionsAsset = inputActions;
        }
        else
        {
            Debug.LogWarning($"Input action asset not found at {InputActionsAssetPath}; UI module will use defaults.");
        }
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void CreateMenuCameraRoot()
    {
        GameObject camerasRoot = new GameObject("_CAMERAS");
        GameObject cameraObject = new GameObject("Menu Camera");
        cameraObject.transform.SetParent(camerasRoot.transform, false);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.008f, 0.014f, 0.025f, 1f);
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.fieldOfView = 60f;
        camera.allowHDR = true;
        camera.allowMSAA = true;
    }

    private static void CreateUiRoot()
    {
        GameObject uiRoot = new GameObject("_UI_ROOT");
        GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrontendMenuPrefabPath);
        if (menuPrefab == null)
        {
            throw new InvalidOperationException($"Frontend menu prefab not found at {FrontendMenuPrefabPath}.");
        }

        GameObject menuInstance = PrefabUtility.InstantiatePrefab(menuPrefab, uiRoot.transform) as GameObject;
        if (menuInstance == null)
        {
            throw new InvalidOperationException("Could not instantiate FrontendMenu prefab.");
        }

        menuInstance.name = "FrontendMenu";
        menuInstance.transform.localPosition = Vector3.zero;
        menuInstance.transform.localRotation = Quaternion.identity;
        menuInstance.transform.localScale = Vector3.one;

        RectTransform rect = menuInstance.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true)
        };

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene == null || string.IsNullOrWhiteSpace(scene.path)) continue;
            if (string.Equals(scene.path, MenuScenePath, StringComparison.OrdinalIgnoreCase)) continue;

            scenes.Add(new EditorBuildSettingsScene(scene.path, scene.enabled));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
