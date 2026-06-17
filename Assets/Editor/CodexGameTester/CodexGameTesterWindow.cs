#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CodexUnityGameTester
{
    public sealed class CodexGameTesterWindow : EditorWindow
    {
        private readonly List<UiElementInfo> uiElements = new List<UiElementInfo>();
        private readonly List<string> actionLog = new List<string>();
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private Vector2 relationScroll;
        private string filter = string.Empty;
        private bool includeInactive = true;
        private bool autoRefresh = true;
        private bool interactableOnly;
        private double nextRefreshTime;
        private GameObject selectedObject;
        private int tab;

        [MenuItem("Tools/Codex/Game Tester")]
        public static void Open()
        {
            GetWindow<CodexGameTesterWindow>("Codex Game Tester");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!autoRefresh || EditorApplication.timeSinceStartup < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = EditorApplication.timeSinceStartup + 0.5d;
            Refresh();
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                selectedObject = Selection.activeGameObject;
            }
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawUiList();
            DrawDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(Application.isPlaying ? "Play Mode" : "Edit Mode", EditorStyles.boldLabel, GUILayout.Width(90));
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto refresh", GUILayout.Width(110));
            includeInactive = GUILayout.Toggle(includeInactive, "Inactive", GUILayout.Width(80));
            interactableOnly = GUILayout.Toggle(interactableOnly, "Interactable", GUILayout.Width(100));

            if (GUILayout.Button("Refresh", GUILayout.Width(75)))
            {
                Refresh();
            }

            if (GUILayout.Button("Game View", GUILayout.Width(85)))
            {
                OpenGameView();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"UI: {uiElements.Count}", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            filter = EditorGUILayout.TextField("Filter", filter);
            EditorGUILayout.EndVertical();
        }

        private void DrawUiList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Min(420, Mathf.Max(300, position.width * 0.38f))));
            GUILayout.Label("Live UI Elements", EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            foreach (UiElementInfo element in uiElements)
            {
                if (!MatchesFilter(element))
                {
                    continue;
                }

                if (interactableOnly && !element.IsInteractable)
                {
                    continue;
                }

                GUIStyle style = selectedObject == element.GameObject ? EditorStyles.helpBox : EditorStyles.miniButton;
                EditorGUILayout.BeginVertical(style);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(element.TypeLabel, EditorStyles.boldLabel, GUILayout.Width(95));
                GUILayout.Label(element.NamePath, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(element.StateLabel, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Width(55)))
                {
                    Select(element.GameObject);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical();
            selectedObject = selectedObject != null ? selectedObject : Selection.activeGameObject;
            tab = GUILayout.Toolbar(tab, new[] { "Selected", "Events", "Scripts", "References", "Log" });
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Select a UI element or scene GameObject to inspect it.", MessageType.Info);
            }
            else if (tab == 0)
            {
                DrawSelected();
            }
            else if (tab == 1)
            {
                DrawEvents(selectedObject);
            }
            else if (tab == 2)
            {
                DrawScripts(selectedObject);
            }
            else if (tab == 3)
            {
                DrawReferences(selectedObject);
            }
            else
            {
                DrawLog();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelected()
        {
            EditorGUILayout.LabelField("Object", selectedObject.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path", BuildPath(selectedObject));
            EditorGUILayout.LabelField("Scene", selectedObject.scene.IsValid() ? selectedObject.scene.name : "No scene");
            EditorGUILayout.LabelField("Active", selectedObject.activeInHierarchy ? "Active in hierarchy" : "Inactive in hierarchy");

            RectTransform rectTransform = selectedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                EditorGUILayout.Space();
                GUILayout.Label("RectTransform", EditorStyles.boldLabel);
                EditorGUILayout.Vector2Field("Anchored position", rectTransform.anchoredPosition);
                EditorGUILayout.Vector2Field("Size", rectTransform.rect.size);
                EditorGUILayout.LabelField("World bounds", $"{corners[0]} to {corners[2]}");
            }

            DrawControlActions(selectedObject);
        }

        private void DrawControlActions(GameObject gameObject)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Behavior Checks", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode before invoking UI behavior. This avoids mutating gameplay state in Edit Mode.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                Button button = gameObject.GetComponent<Button>();
                if (button != null)
                {
                    using (new EditorGUI.DisabledScope(!button.interactable))
                    {
                        if (GUILayout.Button("Invoke Button.onClick"))
                        {
                            button.onClick.Invoke();
                            Log($"Invoked Button.onClick on {BuildPath(gameObject)}");
                        }
                    }
                }

                Toggle toggle = gameObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    if (GUILayout.Button(toggle.isOn ? "Set Toggle Off" : "Set Toggle On"))
                    {
                        toggle.isOn = !toggle.isOn;
                        Log($"Changed Toggle on {BuildPath(gameObject)} to {toggle.isOn}");
                    }
                }

                Slider slider = gameObject.GetComponent<Slider>();
                if (slider != null)
                {
                    float next = EditorGUILayout.Slider("Slider value", slider.value, slider.minValue, slider.maxValue);
                    if (!Mathf.Approximately(next, slider.value))
                    {
                        slider.value = next;
                        Log($"Changed Slider on {BuildPath(gameObject)} to {slider.value}");
                    }
                }

                Dropdown dropdown = gameObject.GetComponent<Dropdown>();
                if (dropdown != null && dropdown.options.Count > 0)
                {
                    if (GUILayout.Button("Advance Dropdown"))
                    {
                        dropdown.value = (dropdown.value + 1) % dropdown.options.Count;
                        Log($"Changed Dropdown on {BuildPath(gameObject)} to {dropdown.value}");
                    }
                }

                Component tmpDropdown = GetComponentByTypeName(gameObject, "TMP_Dropdown");
                if (tmpDropdown != null && TryGetIntProperty(tmpDropdown, "value", out int tmpDropdownValue))
                {
                    int optionCount = GetOptionsCount(tmpDropdown);
                    if (optionCount > 0 && GUILayout.Button("Advance TMP_Dropdown"))
                    {
                        int next = (tmpDropdownValue + 1) % optionCount;
                        TrySetProperty(tmpDropdown, "value", next);
                        Log($"Changed TMP_Dropdown on {BuildPath(gameObject)} to {next}");
                    }
                }

                InputField input = gameObject.GetComponent<InputField>();
                if (input != null)
                {
                    string next = EditorGUILayout.TextField("Input text", input.text);
                    if (next != input.text)
                    {
                        input.text = next;
                        Log($"Changed InputField on {BuildPath(gameObject)}");
                    }
                }

                Component tmpInput = GetComponentByTypeName(gameObject, "TMP_InputField");
                if (tmpInput != null && TryGetStringProperty(tmpInput, "text", out string tmpText))
                {
                    string next = EditorGUILayout.TextField("TMP input text", tmpText);
                    if (next != tmpText)
                    {
                        TrySetProperty(tmpInput, "text", next);
                        Log($"Changed TMP_InputField on {BuildPath(gameObject)}");
                    }
                }
            }
        }

        private void DrawEvents(GameObject gameObject)
        {
            StringBuilder builder = new StringBuilder();

            Button button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                AppendUnityEvent(builder, "Button.onClick", button.onClick);
            }

            Toggle toggle = gameObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                AppendUnityEvent(builder, "Toggle.onValueChanged", toggle.onValueChanged);
            }

            Slider slider = gameObject.GetComponent<Slider>();
            if (slider != null)
            {
                AppendUnityEvent(builder, "Slider.onValueChanged", slider.onValueChanged);
            }

            Dropdown dropdown = gameObject.GetComponent<Dropdown>();
            if (dropdown != null)
            {
                AppendUnityEvent(builder, "Dropdown.onValueChanged", dropdown.onValueChanged);
            }

            Component tmpDropdown = GetComponentByTypeName(gameObject, "TMP_Dropdown");
            if (tmpDropdown != null)
            {
                AppendReflectedUnityEvent(builder, "TMP_Dropdown.onValueChanged", tmpDropdown, "onValueChanged");
            }

            InputField input = gameObject.GetComponent<InputField>();
            if (input != null)
            {
                AppendUnityEvent(builder, "InputField.onValueChanged", input.onValueChanged);
                AppendUnityEvent(builder, "InputField.onEndEdit", input.onEndEdit);
            }

            Component tmpInput = GetComponentByTypeName(gameObject, "TMP_InputField");
            if (tmpInput != null)
            {
                AppendReflectedUnityEvent(builder, "TMP_InputField.onValueChanged", tmpInput, "onValueChanged");
                AppendReflectedUnityEvent(builder, "TMP_InputField.onEndEdit", tmpInput, "onEndEdit");
                AppendReflectedUnityEvent(builder, "TMP_InputField.onSubmit", tmpInput, "onSubmit");
            }

            EventTrigger trigger = gameObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                builder.AppendLine("EventTrigger");
                foreach (EventTrigger.Entry entry in trigger.triggers)
                {
                    builder.AppendLine($"- {entry.eventID}");
                    AppendUnityEvent(builder, "  callback", entry.callback);
                }
            }

            if (builder.Length == 0)
            {
                EditorGUILayout.HelpBox("No supported Unity UI events found on the selected object.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.TextArea(builder.ToString(), GUILayout.ExpandHeight(true));
            }
        }

        private void DrawScripts(GameObject gameObject)
        {
            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            if (behaviours.Length == 0)
            {
                EditorGUILayout.HelpBox("No MonoBehaviour scripts are attached to this object.", MessageType.Info);
                return;
            }

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    EditorGUILayout.HelpBox("Missing script component.", MessageType.Error);
                    continue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                Type type = behaviour.GetType();
                GUILayout.Label(type.Name, EditorStyles.boldLabel);
                MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
                if (script != null)
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    EditorGUILayout.LabelField("Script asset", path);
                    if (GUILayout.Button("Open Script", GUILayout.Width(100)))
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                }

                DrawSerializedReferences(behaviour);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawReferences(GameObject gameObject)
        {
            GUILayout.Label("Scene Reverse References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("References are discovered from serialized object fields in loaded scene MonoBehaviours.", MessageType.Info);

            HashSet<UnityEngine.Object> targets = new HashSet<UnityEngine.Object>();
            targets.Add(gameObject);
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component != null)
                {
                    targets.Add(component);
                }
            }

            relationScroll = EditorGUILayout.BeginScrollView(relationScroll);
            int count = 0;
            foreach (MonoBehaviour behaviour in SceneComponents<MonoBehaviour>(includeInactive))
            {
                if (behaviour == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(behaviour);
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                    {
                        continue;
                    }

                    if (!targets.Contains(property.objectReferenceValue))
                    {
                        continue;
                    }

                    EditorGUILayout.LabelField(
                        $"{BuildPath(behaviour.gameObject)}",
                        $"{behaviour.GetType().Name}.{property.displayName}");
                    count++;
                    if (count >= 100)
                    {
                        EditorGUILayout.HelpBox("Reference list truncated at 100 entries.", MessageType.Info);
                        break;
                    }
                }

                if (count >= 100)
                {
                    break;
                }
            }

            if (count == 0)
            {
                EditorGUILayout.LabelField("No serialized scene references found.");
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLog()
        {
            if (actionLog.Count == 0)
            {
                EditorGUILayout.LabelField("No actions yet.");
                return;
            }

            foreach (string item in actionLog)
            {
                EditorGUILayout.LabelField(item, EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawSerializedReferences(MonoBehaviour behaviour)
        {
            SerializedObject serialized = new SerializedObject(behaviour);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            int count = 0;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                {
                    continue;
                }

                EditorGUILayout.ObjectField(property.displayName, property.objectReferenceValue, typeof(UnityEngine.Object), true);
                count++;
            }

            if (count == 0)
            {
                EditorGUILayout.LabelField("No serialized object references.");
            }
        }

        private void Refresh()
        {
            uiElements.Clear();
            foreach (GameObject gameObject in SceneGameObjects(includeInactive))
            {
                UiElementInfo info = UiElementInfo.TryCreate(gameObject);
                if (info != null)
                {
                    uiElements.Add(info);
                }
            }

            uiElements.Sort((a, b) => string.Compare(a.NamePath, b.NamePath, StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesFilter(UiElementInfo info)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            return info.NamePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                   || info.TypeLabel.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Select(GameObject gameObject)
        {
            selectedObject = gameObject;
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        private void Log(string message)
        {
            actionLog.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
            if (actionLog.Count > 100)
            {
                actionLog.RemoveAt(actionLog.Count - 1);
            }
        }

        private static void OpenGameView()
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                EditorWindow.GetWindow(gameViewType);
            }
        }

        private static IEnumerable<GameObject> SceneGameObjects(bool includeInactiveObjects)
        {
            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject == null || !gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!includeInactiveObjects && !gameObject.activeInHierarchy)
                {
                    continue;
                }

                yield return gameObject;
            }
        }

        private static IEnumerable<T> SceneComponents<T>(bool includeInactiveObjects) where T : Component
        {
            foreach (GameObject gameObject in SceneGameObjects(includeInactiveObjects))
            {
                foreach (T component in gameObject.GetComponents<T>())
                {
                    if (component != null)
                    {
                        yield return component;
                    }
                }
            }
        }

        private static string BuildPath(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return "<null>";
            }

            List<string> names = new List<string>();
            Transform current = gameObject.transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            string path = string.Join("/", names);
            return gameObject.scene.IsValid() ? $"{gameObject.scene.name}:{path}" : path;
        }

        private static void AppendUnityEvent(StringBuilder builder, string label, UnityEventBase unityEvent)
        {
            if (unityEvent == null)
            {
                return;
            }

            int count = unityEvent.GetPersistentEventCount();
            builder.AppendLine($"{label} ({count} persistent listener{(count == 1 ? string.Empty : "s")})");
            if (count == 0)
            {
                builder.AppendLine("- No persistent listeners.");
                return;
            }

            for (int index = 0; index < count; index++)
            {
                UnityEngine.Object target = unityEvent.GetPersistentTarget(index);
                string method = unityEvent.GetPersistentMethodName(index);
                string targetName = target != null ? target.name : "<missing target>";
                string targetType = target != null ? target.GetType().Name : "<unknown>";
                builder.AppendLine($"- {targetName} ({targetType}).{method}");
            }
        }

        private static void AppendReflectedUnityEvent(StringBuilder builder, string label, Component component, string memberName)
        {
            object value = GetMemberValue(component, memberName);
            if (value is UnityEventBase unityEvent)
            {
                AppendUnityEvent(builder, label, unityEvent);
            }
        }

        private static Component GetComponentByTypeName(GameObject gameObject, string typeName)
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    return component;
                }
            }

            return null;
        }

        private static object GetMemberValue(object instance, string memberName)
        {
            if (instance == null)
            {
                return null;
            }

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance, null);
            }

            FieldInfo field = type.GetField(memberName, Flags);
            return field != null ? field.GetValue(instance) : null;
        }

        private static bool TrySetProperty(object instance, string propertyName, object value)
        {
            if (instance == null)
            {
                return false;
            }

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = instance.GetType().GetProperty(propertyName, Flags);
            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(instance, value, null);
            return true;
        }

        private static bool TryGetIntProperty(object instance, string propertyName, out int value)
        {
            object raw = GetMemberValue(instance, propertyName);
            if (raw is int intValue)
            {
                value = intValue;
                return true;
            }

            value = 0;
            return false;
        }

        private static bool TryGetStringProperty(object instance, string propertyName, out string value)
        {
            object raw = GetMemberValue(instance, propertyName);
            if (raw is string stringValue)
            {
                value = stringValue;
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static int GetOptionsCount(object dropdown)
        {
            object options = GetMemberValue(dropdown, "options");
            return options is IList list ? list.Count : 0;
        }

        private sealed class UiElementInfo
        {
            public readonly GameObject GameObject;
            public readonly string NamePath;
            public readonly string TypeLabel;
            public readonly string StateLabel;
            public readonly bool IsInteractable;

            private UiElementInfo(GameObject gameObject, string typeLabel, bool isInteractable)
            {
                GameObject = gameObject;
                NamePath = BuildPath(gameObject);
                TypeLabel = typeLabel;
                IsInteractable = isInteractable;
                StateLabel = BuildStateLabel(gameObject, isInteractable);
            }

            public static UiElementInfo TryCreate(GameObject gameObject)
            {
                string label = GetTypeLabel(gameObject);
                if (string.IsNullOrEmpty(label))
                {
                    return null;
                }

                Selectable selectable = gameObject.GetComponent<Selectable>();
                bool interactable = selectable == null || selectable.interactable;
                return new UiElementInfo(gameObject, label, interactable);
            }

            private static string GetTypeLabel(GameObject gameObject)
            {
                if (gameObject.GetComponent<Button>() != null)
                {
                    return "Button";
                }

                if (gameObject.GetComponent<Toggle>() != null)
                {
                    return "Toggle";
                }

                if (gameObject.GetComponent<Slider>() != null)
                {
                    return "Slider";
                }

                if (gameObject.GetComponent<Dropdown>() != null)
                {
                    return "Dropdown";
                }

                if (GetComponentByTypeName(gameObject, "TMP_Dropdown") != null)
                {
                    return "TMP_Dropdown";
                }

                if (gameObject.GetComponent<InputField>() != null)
                {
                    return "InputField";
                }

                if (GetComponentByTypeName(gameObject, "TMP_InputField") != null)
                {
                    return "TMP_InputField";
                }

                if (gameObject.GetComponent<EventTrigger>() != null)
                {
                    return "EventTrigger";
                }

                if (gameObject.GetComponent<Canvas>() != null)
                {
                    return "Canvas";
                }

                if (gameObject.GetComponent<Selectable>() != null)
                {
                    return "Selectable";
                }

                if (gameObject.GetComponent<Graphic>() != null)
                {
                    return "Graphic";
                }

                return gameObject.GetComponent<RectTransform>() != null ? "RectTransform" : string.Empty;
            }

            private static string BuildStateLabel(GameObject gameObject, bool isInteractable)
            {
                Graphic graphic = gameObject.GetComponent<Graphic>();
                CanvasGroup group = gameObject.GetComponentInParent<CanvasGroup>();
                string active = gameObject.activeInHierarchy ? "active" : "inactive";
                string interactable = isInteractable ? "interactable" : "not interactable";
                string raycast = graphic != null && graphic.raycastTarget ? "raycast target" : "not raycast target";
                string groupState = group != null ? $"canvasGroup alpha {group.alpha:0.##}, blocks {group.blocksRaycasts}" : "no canvas group";
                return $"{active}, {interactable}, {raycast}, {groupState}";
            }
        }
    }
}
#endif
