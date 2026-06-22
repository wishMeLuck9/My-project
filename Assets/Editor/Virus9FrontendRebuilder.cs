using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class Virus9FrontendRebuilder
{
    private const string ResourcesUiFolder = "Assets/Resources/UI";
    private const string FontsFolder = "Assets/Fonts/UI";
    private const string MenuPrefabPath = "Assets/Resources/UI/FrontendMenu.prefab";
    private const string CatalogPath = "Assets/Resources/Localization/LocalizationCatalog.asset";
    private const string NotoSansRegularPath = "Assets/Fonts/UI/NotoSans-Regular.ttf";
    private const string UiFontAssetPath = "Assets/Fonts/UI/VIRUS9_NotoSans_UI.asset";
    private const string PortugueseFontAssetPath = "Assets/Fonts/UI/VIRUS9_NotoSans_Portuguese.asset";

    private static readonly Color BackgroundColor = new Color(0.006f, 0.01f, 0.014f, 1f);
    private static readonly Color PanelColor = new Color(0.025f, 0.052f, 0.058f, 0.93f);
    private static readonly Color PanelStrongColor = new Color(0.038f, 0.086f, 0.092f, 0.96f);
    private static readonly Color AccentColor = new Color(0.25f, 0.86f, 0.74f, 1f);
    private static readonly Color AccentMutedColor = new Color(0.12f, 0.38f, 0.36f, 1f);
    private static readonly Color TextColor = new Color(0.88f, 0.96f, 0.92f, 1f);
    private static readonly Color MutedTextColor = new Color(0.55f, 0.72f, 0.68f, 1f);

    [MenuItem("VIRUS9/Rebuild Frontend From Scratch")]
    public static void RebuildFrontendFromScratch()
    {
        EnsureFolders();
        AssetDatabase.Refresh();

        TMP_FontAsset uiFont = EnsureFontAsset("VIRUS9_NotoSans_UI", UiFontAssetPath);
        TMP_FontAsset portugueseFont = EnsureFontAsset("VIRUS9_NotoSans_Portuguese", PortugueseFontAssetPath);

        RebuildLocalizationCatalog();
        RebuildFrontendMenuPrefab(uiFont, portugueseFont);
        MenuBootSceneBuilder.BuildMenuBootScene();
        PlayerHumanoidControllerClimbBuilder.EnsureClimbState();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VIRUS9 frontend rebuilt from scratch: font assets, localization catalog, menu prefab, menu scene, and climb animator state.");
    }

    public static void RebuildFrontendFromScratchBatch()
    {
        RebuildFrontendFromScratch();
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(ResourcesUiFolder);
        Directory.CreateDirectory(FontsFolder);
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath) ?? "Assets/Resources/Localization");
    }

    private static TMP_FontAsset EnsureFontAsset(string assetName, string assetPath)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(NotoSansRegularPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException($"Noto Sans source font is missing at {NotoSansRegularPath}. Download it before rebuilding the frontend.");
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset != null && !HasUsableAtlas(fontAsset))
        {
            AssetDatabase.DeleteAsset(assetPath);
            fontAsset = null;
        }

        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AddFontSubAssets(fontAsset);
        }

        fontAsset.name = assetName;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        if (fontAsset.fallbackFontAssetTable != null)
        {
            fontAsset.fallbackFontAssetTable.Clear();
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    private static void AddFontSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null) return;

        Material material = fontAsset.material;
        if (material != null && !AssetDatabase.Contains(material))
        {
            material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        Texture2D atlasTexture = fontAsset.atlasTexture;
        if (atlasTexture != null && !AssetDatabase.Contains(atlasTexture))
        {
            atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }
    }

    private static bool HasUsableAtlas(TMP_FontAsset fontAsset)
    {
        return fontAsset != null &&
               fontAsset.atlasTextures != null &&
               fontAsset.atlasTextures.Length > 0 &&
               fontAsset.atlasTextures[0] != null;
    }

    private static void RebuildLocalizationCatalog()
    {
        LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LocalizationCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        List<LocalizationCatalog.Entry> entries = new List<LocalizationCatalog.Entry>(catalog.Entries);

        Upsert(entries, "app.title", "VIRUS9", "VIRUS9", "VIRUS9");
        Upsert(entries, "menu.start", "НАЧАТЬ", "START", "INICIAR");
        Upsert(entries, "menu.intro",
            "МИР ДУШ НЕ ДАЕТ ИНСТРУКЦИЙ.\nНО СИСТЕМА ЗАПИСЫВАЕТ КАЖДЫЙ ТВОЙ ШАГ.",
            "THE WORLD OF SOULS GIVES NO INSTRUCTIONS.\nBUT THE SYSTEM RECORDS EVERY STEP YOU TAKE.",
            "O MUNDO DAS ALMAS NÃO DÁ INSTRUÇÕES.\nMAS O SISTEMA REGISTA CADA PASSO TEU.");
        Upsert(entries, "menu.new_game", "НОВАЯ ИГРА", "NEW GAME", "NOVO JOGO");
        Upsert(entries, "menu.continue", "ПРОДОЛЖИТЬ", "CONTINUE", "CONTINUAR");
        Upsert(entries, "menu.load_save", "ЗАГРУЗКА / СОХРАНЕНИЕ", "LOAD / SAVE", "CARREGAR / GUARDAR");
        Upsert(entries, "menu.settings", "НАСТРОЙКИ", "SETTINGS", "DEFINIÇÕES");
        Upsert(entries, "menu.credits", "ТИТРЫ", "CREDITS", "CRÉDITOS");
        Upsert(entries, "menu.exit", "ВЫЙТИ", "EXIT", "SAIR");
        Upsert(entries, "menu.back", "НАЗАД", "BACK", "VOLTAR");
        Upsert(entries, "menu.credits_text",
            "VIRUS9\n\nСобрано заново для прототипа.",
            "VIRUS9\n\nRebuilt for the prototype.",
            "VIRUS9\n\nReconstruído para o protótipo.");
        Upsert(entries, "confirm.yes", "ДА", "YES", "SIM");
        Upsert(entries, "confirm.no", "НЕТ", "NO", "NÃO");
        Upsert(entries, "confirm.exit", "ВЫЙТИ ИЗ ИГРЫ?", "EXIT THE GAME?", "SAIR DO JOGO?");

        Upsert(entries, "settings.title", "НАСТРОЙКИ", "SETTINGS", "DEFINIÇÕES");
        Upsert(entries, "settings.language", "ЯЗЫК", "LANGUAGE", "LÍNGUA");
        Upsert(entries, "settings.audio", "АУДИО", "AUDIO", "ÁUDIO");
        Upsert(entries, "settings.video", "ВИДЕО", "VIDEO", "VÍDEO");
        Upsert(entries, "settings.controls", "УПРАВЛЕНИЕ", "CONTROLS", "CONTROLOS");
        Upsert(entries, "settings.accessibility", "ДОСТУПНОСТЬ", "ACCESSIBILITY", "ACESSIBILIDADE");
        Upsert(entries, "settings.static_controls",
            "WASD - бег\nSHIFT - ходьба\nSPACE - прыжок / карабканье у препятствия\nE - взаимодействие\nLMB - атака ночью\nESC - пауза",
            "WASD - run\nSHIFT - walk\nSPACE - jump / climb near an obstacle\nE - interact\nLMB - night attack\nESC - pause",
            "WASD - correr\nSHIFT - andar\nSPACE - saltar / trepar junto a obstáculo\nE - interagir\nLMB - atacar à noite\nESC - pausa");
        Upsert(entries, "settings.small", "МАЛЕНЬКИЙ", "SMALL", "PEQUENO");
        Upsert(entries, "settings.medium", "СРЕДНИЙ", "MEDIUM", "MÉDIO");
        Upsert(entries, "settings.large", "БОЛЬШОЙ", "LARGE", "GRANDE");
        Upsert(entries, "language.russian", "РУССКИЙ", "RUSSIAN", "RUSSO");
        Upsert(entries, "language.english", "АНГЛИЙСКИЙ", "ENGLISH", "INGLÊS");
        Upsert(entries, "language.portuguese", "ПОРТУГАЛЬСКИЙ", "PORTUGUESE", "PORTUGUÊS");

        Upsert(entries, "save.load", "ЗАГРУЗИТЬ", "LOAD", "CARREGAR");
        Upsert(entries, "save.save", "СОХРАНИТЬ", "SAVE", "GUARDAR");
        Upsert(entries, "save.autosave", "СЛОТ 1 - АВТОСОХРАНЕНИЕ", "SLOT 1 - AUTOSAVE", "SLOT 1 - AUTOGUARDAR");
        Upsert(entries, "save.manual_slot", "СЛОТ {0} - РУЧНОЕ СОХРАНЕНИЕ", "SLOT {0} - MANUAL SAVE", "SLOT {0} - GUARDAR MANUAL");
        Upsert(entries, "save.empty", "ПУСТО", "EMPTY", "VAZIO");

        Upsert(entries, "hud.boundary.exterior.blocked",
            "Дальше зона прототипа заканчивается. Возвращаю тебя внутрь.",
            "The prototype area ends here. Moving you back inside.",
            "A área do protótipo acaba aqui. Vou pôr-te de volta dentro.");
        Upsert(entries, "hud.boundary.exterior.escape",
            "Ты выпал за границы сцены. Возвращаю к безопасной точке.",
            "You left the scene bounds. Returning you to a safe point.",
            "Saíste dos limites da cena. A voltar para um ponto seguro.");

        catalog.ReplaceEntries(entries);
        EditorUtility.SetDirty(catalog);
    }

    private static void Upsert(List<LocalizationCatalog.Entry> entries, string key, string russian, string english, string portuguese)
    {
        LocalizationCatalog.Entry entry = entries.Find(candidate => candidate != null && candidate.key == key);
        if (entry == null)
        {
            entry = new LocalizationCatalog.Entry { key = key };
            entries.Add(entry);
        }

        entry.russian = russian;
        entry.english = english;
        entry.portuguese = portuguese;
    }

    private static void RebuildFrontendMenuPrefab(TMP_FontAsset uiFont, TMP_FontAsset portugueseFont)
    {
        AssetDatabase.DeleteAsset(MenuPrefabPath);

        GameObject root = CreateUiObject("FrontendMenu", null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        FrontendMenuController controller = root.AddComponent<FrontendMenuController>();

        CreateBackground(root.transform, uiFont);
        TMP_Dropdown languageDropdown = CreateLanguageDropdown(root.transform, uiFont, new Vector2(690f, 430f));
        GameObject startPanel = CreateStartPanel(root.transform, uiFont);
        GameObject mainMenuPanel = CreateMainMenuPanel(root.transform, uiFont, out Button newGameButton, out Button continueButton, out Button loadSaveButton, out Button settingsButton, out Button creditsButton, out Button exitButton);
        GameObject creditsPanel = CreateCreditsPanel(root.transform, uiFont, out Button creditsBackButton);
        GameObject confirmPanel = CreateConfirmPanel(root.transform, uiFont, out TMP_Text confirmText, out Button confirmYesButton, out Button confirmNoButton);
        GameObject settingsPanel = CreateSettingsPanel(root.transform, uiFont);
        GameObject saveSlotPanel = CreateSaveSlotPanel(root.transform, uiFont);

        Button startButton = startPanel.GetComponentInChildren<Button>(true);

        SetActiveForPrefab(startPanel, true);
        SetActiveForPrefab(mainMenuPanel, false);
        SetActiveForPrefab(creditsPanel, false);
        SetActiveForPrefab(confirmPanel, false);
        SetActiveForPrefab(settingsPanel, false);
        SetActiveForPrefab(saveSlotPanel, false);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "startPanel", startPanel);
        SetObject(serialized, "mainMenuPanel", mainMenuPanel);
        SetObject(serialized, "creditsPanel", creditsPanel);
        SetObject(serialized, "confirmPanel", confirmPanel);
        SetObject(serialized, "confirmText", confirmText);
        SetObject(serialized, "startButton", startButton);
        SetObject(serialized, "newGameButton", newGameButton);
        SetObject(serialized, "continueButton", continueButton);
        SetObject(serialized, "loadSaveButton", loadSaveButton);
        SetObject(serialized, "settingsButton", settingsButton);
        SetObject(serialized, "creditsButton", creditsButton);
        SetObject(serialized, "exitButton", exitButton);
        SetObject(serialized, "creditsBackButton", creditsBackButton);
        SetObject(serialized, "confirmYesButton", confirmYesButton);
        SetObject(serialized, "confirmNoButton", confirmNoButton);
        SetObject(serialized, "languageDropdown", languageDropdown);
        SetObject(serialized, "settingsPanel", settingsPanel.GetComponent<SettingsPanelController>());
        SetObject(serialized, "saveSlotPanel", saveSlotPanel.GetComponent<SaveSlotPanelController>());
        serialized.ApplyModifiedPropertiesWithoutUndo();

        AssignPortugueseFontMarker(root, portugueseFont);

        PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void AssignPortugueseFontMarker(GameObject root, TMP_FontAsset portugueseFont)
    {
        TMP_Text marker = CreateText(root.transform, "PortugueseFontMarker", "PORTUGUÊS çãõáéíóú", portugueseFont, 1f, TextAlignmentOptions.Center, TextColor);
        marker.gameObject.hideFlags = HideFlags.HideInHierarchy;
        marker.gameObject.SetActive(false);
        marker.raycastTarget = false;
    }

    private static GameObject CreateStartPanel(Transform parent, TMP_FontAsset font)
    {
        GameObject panel = CreateUiObject("StartPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS9", font, 104f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 110f), new Vector2(720f, 150f));
        title.characterSpacing = 16f;

        TMP_Text subtitle = CreateLocalizedText(panel.transform, "Subtitle", "menu.intro", "THE SYSTEM RECORDS EVERY STEP.", font, 24f, TextAlignmentOptions.TopLeft, MutedTextColor);
        SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(174f, 20f), new Vector2(760f, 120f));
        subtitle.textWrappingMode = TextWrappingModes.Normal;

        Button startButton = CreateMenuButton(panel.transform, "StartButton", "menu.start", "START", font, new Vector2(170f, -130f), new Vector2(330f, 64f));
        startButton.navigation = Navigation.defaultNavigation;
        return panel;
    }

    private static GameObject CreateMainMenuPanel(
        Transform parent,
        TMP_FontAsset font,
        out Button newGameButton,
        out Button continueButton,
        out Button loadSaveButton,
        out Button settingsButton,
        out Button creditsButton,
        out Button exitButton)
    {
        GameObject panel = CreateUiObject("MainMenuPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS9", font, 76f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -92f), new Vector2(600f, 105f));
        title.characterSpacing = 12f;

        GameObject rail = CreateUiObject("ButtonRail", panel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, -40f), new Vector2(500f, 470f));
        Image railImage = rail.AddComponent<Image>();
        railImage.color = PanelColor;
        railImage.raycastTarget = false;

        const float startY = 165f;
        const float step = 70f;
        newGameButton = CreateMenuButton(rail.transform, "NewGameButton", "menu.new_game", "NEW GAME", font, new Vector2(30f, startY), new Vector2(440f, 56f));
        continueButton = CreateMenuButton(rail.transform, "ContinueButton", "menu.continue", "CONTINUE", font, new Vector2(30f, startY - step), new Vector2(440f, 56f));
        loadSaveButton = CreateMenuButton(rail.transform, "LoadSaveButton", "menu.load_save", "LOAD / SAVE", font, new Vector2(30f, startY - step * 2f), new Vector2(440f, 56f));
        settingsButton = CreateMenuButton(rail.transform, "SettingsButton", "menu.settings", "SETTINGS", font, new Vector2(30f, startY - step * 3f), new Vector2(440f, 56f));
        creditsButton = CreateMenuButton(rail.transform, "CreditsButton", "menu.credits", "CREDITS", font, new Vector2(30f, startY - step * 4f), new Vector2(440f, 56f));
        exitButton = CreateMenuButton(rail.transform, "ExitButton", "menu.exit", "EXIT", font, new Vector2(30f, startY - step * 5f), new Vector2(440f, 56f));
        return panel;
    }

    private static GameObject CreateCreditsPanel(Transform parent, TMP_FontAsset font, out Button backButton)
    {
        GameObject panel = CreateCenteredPanel(parent, "CreditsPanel", new Vector2(820f, 520f));
        TMP_Text title = CreateLocalizedText(panel.transform, "Title", "menu.credits", "CREDITS", font, 44f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 72f));

        TMP_Text body = CreateLocalizedText(panel.transform, "CreditsText", "menu.credits_text", "VIRUS9", font, 24f, TextAlignmentOptions.Center, MutedTextColor);
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 260f));
        body.textWrappingMode = TextWrappingModes.Normal;

        backButton = CreateMenuButton(panel.transform, "CreditsBackButton", "menu.back", "BACK", font, new Vector2(0f, -198f), new Vector2(300f, 56f), true);
        return panel;
    }

    private static GameObject CreateConfirmPanel(Transform parent, TMP_FontAsset font, out TMP_Text confirmText, out Button yesButton, out Button noButton)
    {
        GameObject overlay = CreateUiObject("ConfirmPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject panel = CreateCenteredPanel(overlay.transform, "ConfirmBox", new Vector2(680f, 320f));
        confirmText = CreateText(panel.transform, "ConfirmText", "EXIT THE GAME?", font, 32f, TextAlignmentOptions.Center, TextColor);
        SetRect(confirmText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(560f, 110f));
        confirmText.enableAutoSizing = true;
        confirmText.fontSizeMin = 18f;
        confirmText.fontSizeMax = 32f;
        confirmText.textWrappingMode = TextWrappingModes.Normal;

        yesButton = CreateMenuButton(panel.transform, "ConfirmYesButton", "confirm.yes", "YES", font, new Vector2(-160f, -90f), new Vector2(220f, 56f), true);
        noButton = CreateMenuButton(panel.transform, "ConfirmNoButton", "confirm.no", "NO", font, new Vector2(160f, -90f), new Vector2(220f, 56f), true);
        return overlay;
    }

    private static GameObject CreateSettingsPanel(Transform parent, TMP_FontAsset font)
    {
        GameObject panel = CreateCenteredPanel(parent, "SettingsPanel", new Vector2(1040f, 620f));
        SettingsPanelController controller = panel.AddComponent<SettingsPanelController>();

        TMP_Text title = CreateLocalizedText(panel.transform, "SettingsTitle", "settings.title", "SETTINGS", font, 40f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(880f, 58f));

        Button languageTab = CreateMenuButton(panel.transform, "LanguageTabButton", "settings.language", "LANGUAGE", font, new Vector2(24f, -126f), new Vector2(210f, 44f));
        GameObject languagePanel = CreateUiObject("LanguageContentPanel", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -126f), new Vector2(700f, 380f));
        languagePanel.AddComponent<Image>().color = PanelColor;

        TMP_Text label = CreateLocalizedText(languagePanel.transform, "language_Label", "settings.language", "LANGUAGE", font, 20f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, 120f), new Vector2(260f, 36f));

        TMP_Dropdown languageDropdown = CreateDropdown(languagePanel.transform, "language_Dropdown", font, new Vector2(180f, 120f), new Vector2(320f, 40f));

        TMP_Text controls = CreateLocalizedText(languagePanel.transform, "StaticControls", "settings.static_controls", "WASD - run", font, 18f, TextAlignmentOptions.TopLeft, MutedTextColor);
        SetRect(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(610f, 190f));
        controls.textWrappingMode = TextWrappingModes.Normal;

        Button backButton = CreateMenuButton(panel.transform, "SettingsBackButton", "menu.back", "BACK", font, new Vector2(0f, 34f), new Vector2(250f, 46f), true);

        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "tabPanels", new UnityEngine.Object[] { languagePanel });
        SetArray(serialized, "tabButtons", new UnityEngine.Object[] { languageTab });
        SetObject(serialized, "backButton", backButton);
        SetObject(serialized, "language", languageDropdown);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static GameObject CreateSaveSlotPanel(Transform parent, TMP_FontAsset font)
    {
        GameObject panel = CreateCenteredPanel(parent, "SaveSlotPanel", new Vector2(940f, 620f));
        SaveSlotPanelController controller = panel.AddComponent<SaveSlotPanelController>();

        TMP_Text title = CreateLocalizedText(panel.transform, "Title", "menu.load_save", "LOAD / SAVE", font, 40f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(780f, 64f));

        TMP_Text[] slotLabels = new TMP_Text[3];
        Button[] loadButtons = new Button[3];
        Button[] saveButtons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            float y = 160f - i * 118f;
            GameObject row = CreateUiObject($"Slot{i + 1}_Row", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(780f, 92f));
            row.AddComponent<Image>().color = PanelColor;

            slotLabels[i] = CreateText(row.transform, $"Slot{i + 1}_Label", $"SLOT {i + 1}", font, 20f, TextAlignmentOptions.MidlineLeft, TextColor);
            SetRect(slotLabels[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(390f, 74f));
            slotLabels[i].textWrappingMode = TextWrappingModes.Normal;
            slotLabels[i].enableAutoSizing = true;
            slotLabels[i].fontSizeMin = 13f;
            slotLabels[i].fontSizeMax = 20f;

            loadButtons[i] = CreateMenuButton(row.transform, $"Slot{i + 1}_LoadButton", "save.load", "LOAD", font, new Vector2(500f, 0f), new Vector2(130f, 44f), true);
            saveButtons[i] = CreateMenuButton(row.transform, $"Slot{i + 1}_SaveButton", "save.save", "SAVE", font, new Vector2(650f, 0f), new Vector2(130f, 44f), true);
        }

        Button backButton = CreateMenuButton(panel.transform, "SaveBackButton", "menu.back", "BACK", font, new Vector2(0f, -250f), new Vector2(260f, 52f), true);

        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "slotLabels", slotLabels);
        SetArray(serialized, "loadButtons", loadButtons);
        SetArray(serialized, "saveButtons", saveButtons);
        SetObject(serialized, "backButton", backButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static void CreateBackground(Transform parent, TMP_FontAsset font)
    {
        GameObject background = CreateUiObject("Background", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = BackgroundColor;
        backgroundImage.raycastTarget = false;

        GameObject strip = CreateUiObject("AccentStrip", background.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-118f, 0f), new Vector2(4f, 0f));
        Image stripImage = strip.AddComponent<Image>();
        stripImage.color = AccentMutedColor;
        stripImage.raycastTarget = false;

        TMP_Text version = CreateText(background.transform, "BuildMarker", "MENU_BOOT // REBUILT", font, 18f, TextAlignmentOptions.Right, MutedTextColor);
        SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-154f, 40f), new Vector2(420f, 30f));
    }

    private static TMP_Dropdown CreateLanguageDropdown(Transform parent, TMP_FontAsset font, Vector2 position)
    {
        TMP_Dropdown dropdown = CreateDropdown(parent, "LanguageDropdown", font, position, new Vector2(300f, 42f));
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("РУССКИЙ"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("ENGLISH"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("PORTUGUÊS"));
        return dropdown;
    }

    private static GameObject CreateCenteredPanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = CreateUiObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.color = PanelStrongColor;
        image.raycastTarget = true;
        return panel;
    }

    private static Button CreateMenuButton(Transform parent, string name, string key, string fallback, TMP_FontAsset font, Vector2 position, Vector2 size, bool centered = false)
    {
        GameObject buttonObject = CreateUiObject(name, parent, centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.045f, 0.095f, 0.105f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.075f, 0.16f, 0.16f, 1f);
        colors.pressedColor = AccentMutedColor;
        colors.selectedColor = new Color(0.07f, 0.14f, 0.14f, 1f);
        colors.disabledColor = new Color(0.025f, 0.04f, 0.045f, 0.72f);
        button.colors = colors;

        TMP_Text label = CreateLocalizedText(buttonObject.transform, "Label", key, fallback, font, 24f, TextAlignmentOptions.Center, TextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        GameObject root = CreateUiObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Image image = root.AddComponent<Image>();
        image.color = new Color(0.045f, 0.095f, 0.105f, 0.98f);

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;

        TMP_Text label = CreateText(root.transform, "Label", "PORTUGUÊS", font, 18f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(-48f, 0f));
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 18f;
        dropdown.captionText = label;

        TMP_Text arrow = CreateText(root.transform, "Arrow", "v", font, 18f, TextAlignmentOptions.Center, AccentColor);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(32f, 0f));

        GameObject template = CreateUiObject("Template", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(0f, 130f));
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.018f, 0.04f, 0.045f, 0.99f);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        template.AddComponent<CanvasGroup>();

        GameObject viewport = CreateUiObject("Viewport", template.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUiObject("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 120f));
        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject item = CreateUiObject("Item", content.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 36f));
        Toggle toggle = item.AddComponent<Toggle>();
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0f, 0f, 0f, 0f);
        toggle.targetGraphic = itemBackground;

        TMP_Text itemLabel = CreateText(item.transform, "Item Label", "Option", font, 17f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
        dropdown.itemText = itemLabel;
        dropdown.template = template.GetComponent<RectTransform>();
        template.SetActive(false);
        return dropdown;
    }

    private static TMP_Text CreateLocalizedText(Transform parent, string name, string key, string fallback, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
    {
        TMP_Text text = CreateText(parent, name, fallback, font, size, alignment, color);
        LocalizedText localized = text.gameObject.AddComponent<LocalizedText>();
        localized.Configure(key, fallback);
        return text;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUiObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 40f));
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontSizeMax = size;
        text.fontSizeMin = Mathf.Max(10f, size - 10f);
        text.enableAutoSizing = true;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null) gameObject.transform.SetParent(parent, false);
        SetRect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject serialized, string propertyName, UnityEngine.Object[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetActiveForPrefab(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
