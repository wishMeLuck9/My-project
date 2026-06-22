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
    private const string PauseMenuPrefabPath = "Assets/Resources/UI/PauseMenu.prefab";
    private const string CatalogPath = "Assets/Resources/Localization/LocalizationCatalog.asset";
    private const string NotoSansRegularPath = "Assets/Fonts/UI/NotoSans-Regular.ttf";
    private const string NotoSansBoldPath = "Assets/Fonts/UI/NotoSans-Bold.ttf";
    private const string UiFontAssetPath = "Assets/Fonts/UI/VIRUS9_NotoSans_UI.asset";
    private const string UiBoldFontAssetPath = "Assets/Fonts/UI/VIRUS9_NotoSans_Bold_UI.asset";
    private const string PortugueseFontAssetPath = "Assets/Fonts/UI/VIRUS9_NotoSans_Portuguese.asset";
    private const string TmpDefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string TmpFallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
    private const string TmpLiberationSansFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string MenuMusicPath = "Assets/Audio/Music/YouMayLive2.mp3";
    private const string MenuBackgroundPath = "Assets/Art/UI/Menu/MenuBackgroundGrunge.png";
    private const string MenuBorderPath = "Assets/Art/UI/Menu/MenuPanelBorder.png";
    private const string MenuButtonPath = "Assets/Art/UI/Menu/MenuButtonRetro.png";
    private const string MenuSelectPath = "Assets/Art/UI/Menu/MenuSelectHighlight.png";
    private const string MusicOnPath = "Assets/UI button pack 2/Black/MUSIC-BLACK.png";
    private const string MusicOffPath = "Assets/UI button pack 2/Black/Music-off-black.png";
    private const string SoundIconPath = "Assets/UI button pack 2/Black/SOUND-BLACK.png";
    private const string VideoIconPath = "Assets/UI button pack 2/Black/FULL-SCREEN-BLACK.png";
    private const string ControlsIconPath = "Assets/UI button pack 2/Black/Settings-Black.png";
    private const string LanguageIconPath = "Assets/UI button pack 2/Black/CHAT-BLACK.png";
    private const string AccessibilityIconPath = "Assets/UI button pack 2/Black/Idea-black.png";
    private const string BackIconPath = "Assets/UI button pack 2/Black/Back-black.png";
    private const string CloseIconPath = "Assets/UI button pack 2/Black/Close-Black.png";
    private const string TickIconPath = "Assets/UI button pack 2/Black/Tick-black.png";
    private const string PauseIconPath = "Assets/UI button pack 2/Black/Pause-black.png";
    private const string ContinueIconPath = "Assets/UI button pack 2/Black/continue-black.png";
    private const string MenuIconPath = "Assets/UI button pack 2/Black/Menu-Black.png";
    private const string InfoIconPath = "Assets/UI button pack 2/Black/info-black.png";
    private const string PauseFogTexturePath = "Assets/Art/VFX/JohnLemon/perlinNoise.png";
    private const string PauseDustTexturePath = "Assets/Art/VFX/JohnLemon/DustMote.png";
    private const string PauseAmbiencePath = "Assets/Audio/Ambience/JohnLemon/SFXHouseAmbience.wav";
    private const string PauseBuzzPath = "Assets/Audio/Ambience/JohnLemon/SFXBuzzingLight.wav";
    private const string RequiredUiFontCharacters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        "ÀÁÂÃÇÉÊÍÓÔÕÚÜàáâãçéêíóôõúüÊêÍíÓóÚúÇçÃãÕõ";

    private static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);
    private static readonly Color BackgroundColor = new Color(0.006f, 0.008f, 0.007f, 1f);
    private static readonly Color PanelColor = new Color(0.018f, 0.032f, 0.027f, 0.88f);
    private static readonly Color PanelStrongColor = new Color(0.024f, 0.046f, 0.036f, 0.9f);
    private static readonly Color ButtonColor = new Color(0.025f, 0.054f, 0.045f, 0.96f);
    private static readonly Color AccentColor = new Color(0.68f, 0.94f, 0.78f, 1f);
    private static readonly Color AccentMutedColor = new Color(0.16f, 0.32f, 0.24f, 1f);
    private static readonly Color TextColor = new Color(0.9f, 0.96f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.78f, 0.68f, 1f);

    [MenuItem("VIRUS9/Rebuild Frontend From Scratch")]
    public static void RebuildFrontendFromScratch()
    {
        EnsureFolders();
        AssetDatabase.Refresh();

        TMP_FontAsset uiFont = EnsureFontAsset("VIRUS9_NotoSans_UI", UiFontAssetPath, NotoSansRegularPath);
        TMP_FontAsset uiBoldFont = EnsureFontAsset("VIRUS9_NotoSans_Bold_UI", UiBoldFontAssetPath, NotoSansBoldPath);
        TMP_FontAsset portugueseFont = EnsureFontAsset("VIRUS9_NotoSans_Portuguese", PortugueseFontAssetPath, NotoSansRegularPath);
        EnsureTmpFallbackFontAsset();

        FrontendSkin skin = new FrontendSkin(uiFont, uiBoldFont, portugueseFont);
        RebuildLocalizationCatalog();
        RebuildFrontendMenuPrefab(skin);
        PolishPauseMenuPrefab(skin);
        MenuBootSceneBuilder.BuildMenuBootScene();
        PlayerHumanoidControllerClimbBuilder.EnsureClimbState();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VIRUS9 frontend rebuilt from scratch: menu polish, font assets, music, localization catalog, prefab, scene, and climb animator state.");
    }

    public static void RebuildFrontendFromScratchBatch()
    {
        RebuildFrontendFromScratch();
    }

    private sealed class FrontendSkin
    {
        public FrontendSkin(TMP_FontAsset bodyFont, TMP_FontAsset displayFont, TMP_FontAsset portugueseFont)
        {
            BodyFont = bodyFont;
            DisplayFont = displayFont != null ? displayFont : bodyFont;
            PortugueseFont = portugueseFont != null ? portugueseFont : bodyFont;
            Background = LoadSprite(MenuBackgroundPath, Vector4.zero);
            PanelBorder = LoadSprite(MenuBorderPath, new Vector4(22f, 22f, 22f, 22f));
            Button = LoadSprite(MenuButtonPath, new Vector4(18f, 18f, 18f, 18f));
            Select = LoadSprite(MenuSelectPath, new Vector4(18f, 18f, 18f, 18f));
            MusicOn = LoadSprite(MusicOnPath, Vector4.zero);
            MusicOff = LoadSprite(MusicOffPath, Vector4.zero);
            SoundIcon = LoadSprite(SoundIconPath, Vector4.zero);
            VideoIcon = LoadSprite(VideoIconPath, Vector4.zero);
            ControlsIcon = LoadSprite(ControlsIconPath, Vector4.zero);
            LanguageIcon = LoadSprite(LanguageIconPath, Vector4.zero);
            AccessibilityIcon = LoadSprite(AccessibilityIconPath, Vector4.zero);
            BackIcon = LoadSprite(BackIconPath, Vector4.zero);
            CloseIcon = LoadSprite(CloseIconPath, Vector4.zero);
            TickIcon = LoadSprite(TickIconPath, Vector4.zero);
            PauseIcon = LoadSprite(PauseIconPath, Vector4.zero);
            ContinueIcon = LoadSprite(ContinueIconPath, Vector4.zero);
            MenuIcon = LoadSprite(MenuIconPath, Vector4.zero);
            InfoIcon = LoadSprite(InfoIconPath, Vector4.zero);
            PauseFogTexture = LoadRepeatingTexture(PauseFogTexturePath);
            PauseDustTexture = LoadRepeatingTexture(PauseDustTexturePath);
            MenuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicPath);
            PauseAmbience = AssetDatabase.LoadAssetAtPath<AudioClip>(PauseAmbiencePath);
            PauseBuzz = AssetDatabase.LoadAssetAtPath<AudioClip>(PauseBuzzPath);
        }

        public TMP_FontAsset BodyFont { get; }
        public TMP_FontAsset DisplayFont { get; }
        public TMP_FontAsset PortugueseFont { get; }
        public Sprite Background { get; }
        public Sprite PanelBorder { get; }
        public Sprite Button { get; }
        public Sprite Select { get; }
        public Sprite MusicOn { get; }
        public Sprite MusicOff { get; }
        public Sprite SoundIcon { get; }
        public Sprite VideoIcon { get; }
        public Sprite ControlsIcon { get; }
        public Sprite LanguageIcon { get; }
        public Sprite AccessibilityIcon { get; }
        public Sprite BackIcon { get; }
        public Sprite CloseIcon { get; }
        public Sprite TickIcon { get; }
        public Sprite PauseIcon { get; }
        public Sprite ContinueIcon { get; }
        public Sprite MenuIcon { get; }
        public Sprite InfoIcon { get; }
        public Texture2D PauseFogTexture { get; }
        public Texture2D PauseDustTexture { get; }
        public AudioClip MenuMusic { get; }
        public AudioClip PauseAmbience { get; }
        public AudioClip PauseBuzz { get; }
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(ResourcesUiFolder);
        Directory.CreateDirectory(FontsFolder);
        Directory.CreateDirectory("Assets/Audio/Music");
        Directory.CreateDirectory("Assets/Audio/Ambience/JohnLemon");
        Directory.CreateDirectory("Assets/Art/UI/Menu");
        Directory.CreateDirectory("Assets/Art/VFX/JohnLemon");
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath) ?? "Assets/Resources/Localization");
    }

    private static TMP_FontAsset EnsureFontAsset(string assetName, string assetPath, string sourceFontPath)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException($"Source font is missing at {sourceFontPath}. Download it before rebuilding the frontend.");
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset != null && !HasValidUiFontAsset(fontAsset))
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
            if (!fontAsset.TryAddCharacters(RequiredUiFontCharacters, out string missingCharacters) && !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"{assetName} could not pack UI characters: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AddFontSubAssets(fontAsset);
        }

        fontAsset.name = assetName;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        if (fontAsset.fallbackFontAssetTable != null)
        {
            fontAsset.fallbackFontAssetTable.Clear();
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    [MenuItem("VIRUS9/Repair TMP Fallback Font")]
    public static void RepairTmpFallbackFont()
    {
        EnsureFolders();
        AssetDatabase.Refresh();
        EnsureTmpFallbackFontAsset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static TMP_FontAsset EnsureTmpFallbackFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(TmpLiberationSansFontPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"TMP fallback repair skipped because source font is missing at {TmpLiberationSansFontPath}.");
            return null;
        }

        TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFallbackFontPath);
        if (fallbackFont != null && !HasValidStaticFontAsset(fallbackFont, 1024))
        {
            AssetDatabase.DeleteAsset(TmpFallbackFontPath);
            fallbackFont = null;
        }

        if (fallbackFont == null)
        {
            fallbackFont = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (fallbackFont == null)
            {
                Debug.LogWarning("TMP fallback repair skipped because LiberationSans font asset creation failed.");
                return null;
            }

            fallbackFont.name = "LiberationSans SDF - Fallback";
            if (!fallbackFont.TryAddCharacters(RequiredUiFontCharacters, out string missingCharacters) && !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"TMP fallback font could not pack UI characters: {missingCharacters}");
            }

            fallbackFont.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fallbackFont, TmpFallbackFontPath);
            AddFontSubAssets(fallbackFont);
        }

        fallbackFont.name = "LiberationSans SDF - Fallback";
        fallbackFont.atlasPopulationMode = AtlasPopulationMode.Static;
        if (fallbackFont.fallbackFontAssetTable != null)
        {
            fallbackFont.fallbackFontAssetTable.Clear();
        }

        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpDefaultFontPath);
        if (defaultFont != null)
        {
            if (defaultFont.fallbackFontAssetTable == null)
            {
                defaultFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            defaultFont.fallbackFontAssetTable.Clear();
            defaultFont.fallbackFontAssetTable.Add(fallbackFont);
            EditorUtility.SetDirty(defaultFont);
        }

        EditorUtility.SetDirty(fallbackFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(TmpFallbackFontPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(TmpDefaultFontPath, ImportAssetOptions.ForceUpdate);
        return fallbackFont;
    }

    private static bool HasValidStaticFontAsset(TMP_FontAsset fontAsset, int minimumAtlasSize)
    {
        if (fontAsset == null ||
            fontAsset.atlasPopulationMode != AtlasPopulationMode.Static ||
            fontAsset.atlasTextures == null ||
            fontAsset.atlasTextures.Length == 0 ||
            fontAsset.atlasTextures[0] == null ||
            fontAsset.atlasTextures[0].width < minimumAtlasSize ||
            fontAsset.atlasTextures[0].height < minimumAtlasSize ||
            fontAsset.glyphTable == null ||
            fontAsset.glyphTable.Count == 0 ||
            fontAsset.characterTable == null ||
            fontAsset.characterTable.Count == 0)
        {
            return false;
        }

        return ContainsRequiredCharacters(fontAsset);
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

    private static bool HasValidUiFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null ||
            fontAsset.atlasTextures == null ||
            fontAsset.atlasTextures.Length == 0 ||
            fontAsset.atlasTextures[0] == null ||
            fontAsset.atlasTextures[0].width < 1024 ||
            fontAsset.atlasTextures[0].height < 1024)
        {
            return false;
        }

        return ContainsRequiredCharacters(fontAsset);
    }

    private static bool ContainsRequiredCharacters(TMP_FontAsset fontAsset)
    {
        for (int i = 0; i < RequiredUiFontCharacters.Length; i++)
        {
            if (!ContainsCharacter(fontAsset, RequiredUiFontCharacters[i])) return false;
        }

        return true;
    }

    private static bool ContainsCharacter(TMP_FontAsset fontAsset, char character)
    {
        if (fontAsset == null || fontAsset.characterTable == null) return false;

        uint unicode = character;
        for (int i = 0; i < fontAsset.characterTable.Count; i++)
        {
            TMP_Character entry = fontAsset.characterTable[i];
            if (entry != null && entry.unicode == unicode) return true;
        }

        return false;
    }

    private static Sprite LoadSprite(string path, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Menu UI sprite not found at {path}.");
            return null;
        }

        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.spriteImportMode != SpriteImportMode.Single ||
                       importer.mipmapEnabled ||
                       importer.alphaIsTransparency == false ||
                       importer.spriteBorder != border;

        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D LoadRepeatingTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Pause atmosphere texture not found at {path}.");
            return null;
        }

        bool changed = importer.textureType != TextureImporterType.Default ||
                       importer.mipmapEnabled ||
                       importer.wrapMode != TextureWrapMode.Repeat ||
                       importer.alphaIsTransparency == false;

        if (changed)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
        Upsert(entries, "settings.master", "ОБЩАЯ ГРОМКОСТЬ", "MASTER VOLUME", "VOLUME GERAL");
        Upsert(entries, "settings.music", "МУЗЫКА", "MUSIC", "MÚSICA");
        Upsert(entries, "settings.sfx", "ЭФФЕКТЫ", "SFX", "EFEITOS");
        Upsert(entries, "settings.voice", "ДИАЛОГИ", "VOICE / DIALOGUE", "VOZ / DIÁLOGO");
        Upsert(entries, "settings.mute", "ВЫКЛЮЧИТЬ ВЕСЬ ЗВУК", "MUTE ALL", "SILENCIAR TUDO");
        Upsert(entries, "settings.resolution", "РАЗРЕШЕНИЕ", "RESOLUTION", "RESOLUÇÃO");
        Upsert(entries, "settings.fullscreen", "ПОЛНЫЙ ЭКРАН", "FULLSCREEN", "ECRÃ INTEIRO");
        Upsert(entries, "settings.brightness", "ЯРКОСТЬ", "BRIGHTNESS", "BRILHO");
        Upsert(entries, "settings.quality", "КАЧЕСТВО", "QUALITY", "QUALIDADE");
        Upsert(entries, "settings.vsync", "ВЕРТИКАЛЬНАЯ СИНХРОНИЗАЦИЯ", "VSYNC", "SINCRONIZAÇÃO VERTICAL");
        Upsert(entries, "settings.mouse_sensitivity", "ЧУВСТВИТЕЛЬНОСТЬ МЫШИ", "MOUSE SENSITIVITY", "SENSIBILIDADE DO RATO");
        Upsert(entries, "settings.invert_y", "ИНВЕРТИРОВАТЬ ОСЬ Y", "INVERT Y AXIS", "INVERTER EIXO Y");
        Upsert(entries, "settings.static_controls",
            "WASD - бег\nSHIFT - ходьба\nSPACE - прыжок / карабканье у препятствия\nE - взаимодействие\nLMB - атака ночью\nESC - пауза",
            "WASD - run\nSHIFT - walk\nSPACE - jump / climb near an obstacle\nE - interact\nLMB - night attack\nESC - pause",
            "WASD - correr\nSHIFT - andar\nSPACE - saltar / trepar junto a obstáculo\nE - interagir\nLMB - atacar à noite\nESC - pausa");
        Upsert(entries, "settings.subtitles", "СУБТИТРЫ", "SUBTITLES", "LEGENDAS");
        Upsert(entries, "settings.subtitle_size", "РАЗМЕР СУБТИТРОВ", "SUBTITLE SIZE", "TAMANHO DAS LEGENDAS");
        Upsert(entries, "settings.ui_scale", "МАСШТАБ ИНТЕРФЕЙСА", "UI SCALE", "ESCALA DA INTERFACE");
        Upsert(entries, "settings.high_contrast", "ВЫСОКИЙ КОНТРАСТ", "HIGH CONTRAST UI", "ALTO CONTRASTE");
        Upsert(entries, "settings.colorblind", "РЕЖИМ ДЛЯ ДАЛЬТОНИЗМА", "COLORBLIND-FRIENDLY MODE", "MODO PARA DALTONISMO");
        Upsert(entries, "settings.reduce_shake", "УМЕНЬШИТЬ ТРЯСКУ ЭКРАНА", "REDUCE SCREEN SHAKE", "REDUZIR TREMER DO ECRÃ");
        Upsert(entries, "settings.reduce_motion", "УМЕНЬШИТЬ ДВИЖЕНИЕ", "REDUCE MOTION", "REDUZIR MOVIMENTO");
        Upsert(entries, "settings.tutorial_hints", "ПОКАЗЫВАТЬ ПОДСКАЗКИ", "TUTORIAL HINTS", "DICAS DO TUTORIAL");
        Upsert(entries, "settings.hold_mode", "УДЕРЖАНИЕ ВМЕСТО ПОВТОРНЫХ НАЖАТИЙ", "HOLD INSTEAD OF REPEATED PRESS", "MANTER EM VEZ DE PREMIR REPETIDAMENTE");
        Upsert(entries, "settings.toggle_run", "ПЕРЕКЛЮЧАТЕЛЬ БЕГА", "TOGGLE RUN", "ALTERNAR CORRIDA");
        Upsert(entries, "settings.simple_prompts", "ПРОСТЫЕ ПОДСКАЗКИ ВЗАИМОДЕЙСТВИЯ", "SIMPLE INTERACTION PROMPTS", "INDICAÇÕES SIMPLES DE INTERAÇÃO");
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

    private static void PolishPauseMenuPrefab(FrontendSkin skin)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PauseMenuPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"Pause menu prefab not found at {PauseMenuPrefabPath}.");
            return;
        }

        try
        {
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            RectTransform settingsRect = FindRect(root, "SettingsPanel");
            if (settingsRect != null)
            {
                settingsRect.sizeDelta = new Vector2(920f, 560f);
            }

            BuildPauseAtmosphere(root, skin);
            DecoratePausePanels(root, skin);
            DecoratePauseButtons(root, skin);

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                StylePauseImage(image, skin);
            }

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                StylePauseButton(button);
            }

            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            {
                StylePauseSlider(slider);
            }

            foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                StylePauseDropdown(dropdown, skin);
            }

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                StylePauseToggle(toggle, skin);
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                StylePauseText(text, skin);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PauseMenuPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RectTransform FindRect(GameObject root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect != null && rect.gameObject.name == name) return rect;
        }

        return null;
    }

    private static Button FindButton(GameObject root, string name)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name == name) return button;
        }

        return null;
    }

    private static GameObject EnsureChildObject(Transform parent, string name)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null) return existing.gameObject;

        return CreateUiObject(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static void BuildPauseAtmosphere(GameObject root, FrontendSkin skin)
    {
        RectTransform dimmer = FindRect(root, "PauseDimmer");
        if (dimmer == null) return;

        SetRect(dimmer, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Image dimmerImage = dimmer.GetComponent<Image>();
        if (dimmerImage != null)
        {
            dimmerImage.color = new Color(0f, 0f, 0f, 0.54f);
            dimmerImage.raycastTarget = true;
        }

        CanvasGroup group = dimmer.GetComponent<CanvasGroup>();
        if (group == null) group = dimmer.gameObject.AddComponent<CanvasGroup>();

        RawImage fog = EnsureRawOverlay(dimmer, "PauseAtmosphereFog", skin.PauseFogTexture, new Color(0.12f, 0.28f, 0.19f, 0.085f));
        RawImage dust = EnsureRawOverlay(dimmer, "PauseAtmosphereDust", skin.PauseDustTexture, new Color(0.75f, 0.88f, 0.62f, 0.13f));

        AudioSource[] sources = dimmer.GetComponents<AudioSource>();
        while (sources.Length < 2)
        {
            dimmer.gameObject.AddComponent<AudioSource>();
            sources = dimmer.GetComponents<AudioSource>();
        }

        AudioSource ambienceSource = sources[0];
        AudioSource buzzSource = sources[1];
        ConfigurePauseAudioSource(ambienceSource, skin.PauseAmbience);
        ConfigurePauseAudioSource(buzzSource, skin.PauseBuzz);

        Type atmosphereType = Type.GetType("PauseMenuAtmosphereController, Assembly-CSharp");
        if (atmosphereType == null)
        {
            Debug.LogWarning("PauseMenuAtmosphereController is not compiled yet; pause atmosphere references will be wired on the next rebuild.");
            return;
        }

        Component controller = dimmer.GetComponent(atmosphereType);
        if (controller == null) controller = dimmer.gameObject.AddComponent(atmosphereType);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "group", group);
        SetObject(serialized, "fogOverlay", fog);
        SetObject(serialized, "dustOverlay", dust);
        SetObject(serialized, "ambienceSource", ambienceSource);
        SetObject(serialized, "buzzSource", buzzSource);
        SetFloat(serialized, "ambienceVolumeScale", 0.22f);
        SetFloat(serialized, "buzzVolumeScale", 0.08f);
        SetFloat(serialized, "flickerStrength", 0.04f);
        SetVector2(serialized, "fogTiling", new Vector2(3.8f, 2.4f));
        SetVector2(serialized, "dustTiling", new Vector2(6.4f, 3.2f));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildMenuAtmosphere(GameObject background, FrontendSkin skin)
    {
        if (background == null) return;

        RectTransform rect = background.GetComponent<RectTransform>();
        if (rect == null) return;

        CanvasGroup group = background.GetComponent<CanvasGroup>();
        if (group == null) group = background.AddComponent<CanvasGroup>();

        RawImage fog = EnsureRawOverlay(rect, "MenuAtmosphereFog", skin.PauseFogTexture, new Color(0.12f, 0.31f, 0.21f, 0.11f));
        RawImage dust = EnsureRawOverlay(rect, "MenuAtmosphereDust", skin.PauseDustTexture, new Color(0.82f, 0.92f, 0.62f, 0.045f));

        Type atmosphereType = Type.GetType("PauseMenuAtmosphereController, Assembly-CSharp");
        if (atmosphereType == null)
        {
            Debug.LogWarning("PauseMenuAtmosphereController is not compiled yet; main menu atmosphere references will be wired on the next rebuild.");
            return;
        }

        Component controller = background.GetComponent(atmosphereType);
        if (controller == null) controller = background.AddComponent(atmosphereType);

        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "group", group);
        SetObject(serialized, "fogOverlay", fog);
        SetObject(serialized, "dustOverlay", dust);
        SetObject(serialized, "ambienceSource", null);
        SetObject(serialized, "buzzSource", null);
        SetFloat(serialized, "ambienceVolumeScale", 0f);
        SetFloat(serialized, "buzzVolumeScale", 0f);
        SetFloat(serialized, "flickerStrength", 0.018f);
        SetVector2(serialized, "fogTiling", new Vector2(5.2f, 3.1f));
        SetVector2(serialized, "dustTiling", new Vector2(14f, 8f));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RawImage EnsureRawOverlay(RectTransform parent, string name, Texture2D texture, Color color)
    {
        GameObject overlay = EnsureChildObject(parent, name);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        SetRect(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RawImage image = overlay.GetComponent<RawImage>();
        if (image == null) image = overlay.AddComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        image.raycastTarget = false;

        overlay.transform.SetAsLastSibling();
        return image;
    }

    private static void ConfigurePauseAudioSource(AudioSource source, AudioClip clip)
    {
        if (source == null) return;

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    private static void DecoratePausePanels(GameObject root, FrontendSkin skin)
    {
        AddPanelTexture(FindRect(root, "PauseRootPanel"), skin);
        AddPanelTexture(FindRect(root, "SettingsPanel"), skin);
        AddPanelTexture(FindRect(root, "SaveSlotPanel"), skin);
        AddPanelTexture(FindRect(root, "ConfirmBox"), skin);
    }

    private static void AddPanelTexture(RectTransform panel, FrontendSkin skin)
    {
        if (panel == null) return;

        GameObject grunge = EnsureChildObject(panel, "GrungeOverlay");
        RectTransform grungeRect = grunge.GetComponent<RectTransform>();
        SetRect(grungeRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image grungeImage = grunge.GetComponent<Image>();
        if (grungeImage == null) grungeImage = grunge.AddComponent<Image>();
        grungeImage.sprite = skin.Background;
        grungeImage.color = new Color(0.22f, 0.34f, 0.22f, 0.2f);
        grungeImage.preserveAspect = false;
        grungeImage.raycastTarget = false;
        grunge.transform.SetAsFirstSibling();

        AddAccentStrip(panel, "TopAccent", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-34f, 2f), new Color(0.2f, 0.38f, 0.27f, 0.52f));
        AddAccentStrip(panel, "BottomAccent", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-34f, 2f), new Color(1f, 0.58f, 0.14f, 0.58f));
    }

    private static void AddAccentStrip(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        GameObject strip = EnsureChildObject(parent, name);
        RectTransform rect = strip.GetComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax, pivot, position, size);
        Image image = strip.GetComponent<Image>();
        if (image == null) image = strip.AddComponent<Image>();
        image.sprite = null;
        image.color = color;
        image.raycastTarget = false;
        strip.transform.SetSiblingIndex(Mathf.Min(strip.transform.parent.childCount - 1, 1));
    }

    private static void DecoratePauseButtons(GameObject root, FrontendSkin skin)
    {
        AddIconToButton(FindButton(root, "ResumeButton"), skin.ContinueIcon, skin);
        AddIconToButton(FindButton(root, "SettingsButton"), skin.ControlsIcon, skin);
        AddIconToButton(FindButton(root, "LoadSaveButton"), skin.MenuIcon, skin);
        AddIconToButton(FindButton(root, "MainMenuButton"), skin.BackIcon, skin);
        AddIconToButton(FindButton(root, "ExitButton"), skin.CloseIcon, skin);
        AddIconToButton(FindButton(root, "ConfirmYesButton"), skin.TickIcon, skin);
        AddIconToButton(FindButton(root, "ConfirmNoButton"), skin.CloseIcon, skin);

        AddIconToButton(FindButton(root, "Tab_0"), skin.SoundIcon, skin);
        AddIconToButton(FindButton(root, "Tab_1"), skin.VideoIcon, skin);
        AddIconToButton(FindButton(root, "Tab_2"), skin.ControlsIcon, skin);
        AddIconToButton(FindButton(root, "Tab_3"), skin.LanguageIcon, skin);
        AddIconToButton(FindButton(root, "Tab_4"), skin.AccessibilityIcon, skin);

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button != null && button.gameObject.name.IndexOf("BackButton", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddIconToButton(button, skin.BackIcon, skin);
            }
        }
    }

    private static void AddIconToButton(Button button, Sprite icon, FrontendSkin skin)
    {
        if (button == null || icon == null) return;

        GameObject badge = EnsureChildObject(button.transform, "HudIconBadge");
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        SetRect(badgeRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(30f, 30f));
        Image badgeImage = badge.GetComponent<Image>();
        if (badgeImage == null) badgeImage = badge.AddComponent<Image>();
        badgeImage.sprite = skin.Select != null ? skin.Select : skin.Button;
        badgeImage.type = badgeImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        badgeImage.color = new Color(0.62f, 0.92f, 0.7f, 0.82f);
        badgeImage.raycastTarget = false;

        GameObject iconObject = EnsureChildObject(button.transform, "HudIcon");
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        SetRect(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, 0f), new Vector2(22f, 22f));
        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null) iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        badge.transform.SetSiblingIndex(0);
        iconObject.transform.SetSiblingIndex(1);

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            RectTransform labelRect = label.GetComponent<RectTransform>();
            SetStretchOffsets(labelRect, new Vector2(52f, 0f), new Vector2(-12f, 0f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private static void StylePauseImage(Image image, FrontendSkin skin)
    {
        if (image == null) return;

        string objectName = image.gameObject.name;
        bool isButtonBackground = image.GetComponent<Button>() != null;
        bool isMainPanel = objectName.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           objectName.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           objectName.IndexOf("Content", StringComparison.OrdinalIgnoreCase) >= 0;

        if (objectName == "HudIcon" || objectName == "HudIconBadge" || objectName == "GrungeOverlay" ||
            objectName == "TopAccent" || objectName == "BottomAccent")
        {
            return;
        }

        if (objectName == "PauseDimmer")
        {
            image.color = new Color(0f, 0f, 0f, 0.66f);
            return;
        }

        if (objectName == "Checkmark" || objectName == "Item Checkmark")
        {
            image.sprite = skin.TickIcon;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 0.58f, 0.14f, 1f);
            image.preserveAspect = true;
            return;
        }

        if (isButtonBackground)
        {
            image.sprite = skin.Button;
            image.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = ButtonColor;
            return;
        }

        if (isMainPanel)
        {
            image.sprite = skin.PanelBorder;
            image.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = PanelStrongColor;
        }
    }

    private static void StylePauseButton(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.075f, 0.17f, 0.16f, 1f);
        colors.pressedColor = new Color(1f, 0.58f, 0.14f, 1f);
        colors.selectedColor = new Color(0.07f, 0.15f, 0.14f, 1f);
        colors.disabledColor = new Color(0.025f, 0.04f, 0.045f, 0.72f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void StylePauseSlider(Slider slider)
    {
        if (slider == null) return;

        Image target = slider.targetGraphic as Image;
        if (target != null)
        {
            target.sprite = null;
            target.type = Image.Type.Simple;
            target.color = new Color(0.035f, 0.12f, 0.14f, 1f);
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fill.color = AccentColor;
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.sprite = null;
            handle.type = Image.Type.Simple;
            handle.color = new Color(1f, 0.58f, 0.14f, 1f);
        }
    }

    private static void StylePauseDropdown(TMP_Dropdown dropdown, FrontendSkin skin)
    {
        if (dropdown == null) return;

        Image image = dropdown.targetGraphic as Image;
        if (image != null)
        {
            image.sprite = skin.Button;
            image.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = ButtonColor;
        }

        if (dropdown.template != null)
        {
            RectTransform templateRect = dropdown.template;
            SetRect(templateRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(0f, 124f));

            Image templateImage = dropdown.template.GetComponent<Image>();
            if (templateImage != null)
            {
                templateImage.sprite = skin.PanelBorder;
                templateImage.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
                templateImage.color = PanelStrongColor;
            }

            foreach (Image templateChildImage in dropdown.template.GetComponentsInChildren<Image>(true))
            {
                if (templateChildImage == null || templateChildImage == templateImage) continue;
                if (templateChildImage.gameObject.name == "Item Checkmark")
                {
                    templateChildImage.sprite = skin.TickIcon;
                    templateChildImage.type = Image.Type.Simple;
                    templateChildImage.preserveAspect = true;
                    templateChildImage.color = new Color(1f, 0.58f, 0.14f, 1f);
                }
                else
                {
                    templateChildImage.sprite = skin.Select != null ? skin.Select : skin.Button;
                    templateChildImage.type = templateChildImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    templateChildImage.color = new Color(0.018f, 0.07f, 0.075f, 0.94f);
                }
            }
        }

        EnsureDropdownArrow(dropdown, skin);
    }

    private static void EnsureDropdownArrow(TMP_Dropdown dropdown, FrontendSkin skin)
    {
        Transform arrow = dropdown.transform.Find("Arrow");
        TMP_Text arrowText;
        if (arrow == null)
        {
            arrowText = CreateText(dropdown.transform, "Arrow", "v", skin.DisplayFont, 16f, TextAlignmentOptions.Center, AccentColor);
        }
        else
        {
            arrowText = arrow.GetComponent<TMP_Text>();
            if (arrowText == null) arrowText = arrow.gameObject.AddComponent<TextMeshProUGUI>();
        }

        SetRect(arrowText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(28f, 0f));
        arrowText.font = skin.DisplayFont;
        arrowText.text = "v";
        arrowText.color = AccentColor;
        arrowText.raycastTarget = false;
    }

    private static void StylePauseToggle(Toggle toggle, FrontendSkin skin)
    {
        if (toggle == null) return;

        Image box = toggle.targetGraphic as Image;
        if (box != null)
        {
            box.sprite = skin.Select != null ? skin.Select : skin.Button;
            box.type = box.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            box.color = new Color(0.06f, 0.2f, 0.24f, 1f);
        }

        Image check = toggle.graphic as Image;
        if (check != null)
        {
            check.sprite = skin.TickIcon;
            check.type = Image.Type.Simple;
            check.preserveAspect = true;
            check.color = new Color(1f, 0.58f, 0.14f, 1f);
        }
    }

    private static void StylePauseText(TMP_Text text, FrontendSkin skin)
    {
        if (text == null) return;

        bool title = text.gameObject.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0;
        bool buttonText = text.GetComponentInParent<Button>(true) != null;
        text.font = title || buttonText ? skin.DisplayFont : skin.BodyFont;
        text.enableAutoSizing = true;
        float maxSize = text.fontSize > 0f ? text.fontSize : 16f;
        if (title) maxSize = Mathf.Clamp(maxSize, 28f, 38f);
        else if (buttonText) maxSize = Mathf.Clamp(maxSize, 14f, 18f);
        else maxSize = Mathf.Clamp(maxSize, 13f, 18f);
        text.fontSizeMax = maxSize;
        text.fontSizeMin = Mathf.Max(10f, maxSize - 6f);
        text.color = title ? AccentColor : TextColor;
    }

    private static void RebuildFrontendMenuPrefab(FrontendSkin skin)
    {
        AssetDatabase.DeleteAsset(MenuPrefabPath);

        GameObject root = CreateUiObject("FrontendMenu", null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        FrontendMenuController controller = root.AddComponent<FrontendMenuController>();

        CreateBackground(root.transform, skin);
        GameObject startPanel = CreateStartPanel(root.transform, skin);
        GameObject mainMenuPanel = CreateMainMenuPanel(root.transform, skin, out Button newGameButton, out Button continueButton, out Button loadSaveButton, out Button settingsButton, out Button creditsButton, out Button exitButton);
        GameObject creditsPanel = CreateCreditsPanel(root.transform, skin, out Button creditsBackButton);
        GameObject settingsPanel = CreateSettingsPanel(root.transform, skin);
        GameObject saveSlotPanel = CreateSaveSlotPanel(root.transform, skin);
        TMP_Dropdown languageDropdown = CreateLanguageDropdown(root.transform, skin);
        CreateMenuMusic(root.transform, skin);
        GameObject confirmPanel = CreateConfirmPanel(root.transform, skin, out TMP_Text confirmText, out Button confirmYesButton, out Button confirmNoButton);

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

        AssignPortugueseFontMarker(root, skin);

        PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void AssignPortugueseFontMarker(GameObject root, FrontendSkin skin)
    {
        TMP_Text marker = CreateText(root.transform, "PortugueseFontMarker", "PORTUGUÊS çãõáéíóú", skin.PortugueseFont, 1f, TextAlignmentOptions.Center, TextColor);
        marker.gameObject.hideFlags = HideFlags.HideInHierarchy;
        marker.gameObject.SetActive(false);
        marker.raycastTarget = false;
    }

    private static void CreateMenuMusic(Transform parent, FrontendSkin skin)
    {
        Button toggle = CreateSpriteButton(parent, "MusicToggleButton", skin.MusicOn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -20f), new Vector2(40f, 40f));
        Image icon = toggle.GetComponent<Image>();

        GameObject musicObject = new GameObject("MenuMusic", typeof(AudioSource));
        musicObject.transform.SetParent(parent, false);
        AudioSource audioSource = musicObject.GetComponent<AudioSource>();
        audioSource.clip = skin.MenuMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        MenuMusicController musicController = musicObject.AddComponent<MenuMusicController>();

        SerializedObject serialized = new SerializedObject(musicController);
        SetObject(serialized, "musicClip", skin.MenuMusic);
        SetObject(serialized, "toggleButton", toggle);
        SetObject(serialized, "toggleIcon", icon);
        SetObject(serialized, "musicOnSprite", skin.MusicOn);
        SetObject(serialized, "musicOffSprite", skin.MusicOff);
        SetFloat(serialized, "menuVolumeScale", 0.65f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateStartPanel(Transform parent, FrontendSkin skin)
    {
        GameObject panel = CreateUiObject("StartPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        GameObject titlePlate = CreateUiObject("TitlePlate", panel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 94f), new Vector2(520f, 128f));
        Image titlePlateImage = titlePlate.AddComponent<Image>();
        titlePlateImage.sprite = skin.PanelBorder;
        titlePlateImage.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        titlePlateImage.color = new Color(0.015f, 0.046f, 0.052f, 0.7f);
        titlePlateImage.raycastTarget = false;

        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS9", skin.DisplayFont, 78f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(98f, 112f), new Vector2(520f, 104f));
        title.characterSpacing = 12f;
        title.fontStyle = FontStyles.UpperCase;

        TMP_Text subtitle = CreateLocalizedText(panel.transform, "Subtitle", "menu.intro", "THE SYSTEM RECORDS EVERY STEP.", skin.BodyFont, 18f, TextAlignmentOptions.TopLeft, MutedTextColor);
        SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(102f, 22f), new Vector2(560f, 94f));
        subtitle.textWrappingMode = TextWrappingModes.Normal;

        Button startButton = CreateMenuButton(panel.transform, "StartButton", "menu.start", "START", skin, new Vector2(102f, -116f), new Vector2(260f, 48f));
        AddIconToButton(startButton, skin.ContinueIcon, skin);
        startButton.navigation = Navigation.defaultNavigation;
        return panel;
    }

    private static GameObject CreateMainMenuPanel(
        Transform parent,
        FrontendSkin skin,
        out Button newGameButton,
        out Button continueButton,
        out Button loadSaveButton,
        out Button settingsButton,
        out Button creditsButton,
        out Button exitButton)
    {
        GameObject panel = CreateUiObject("MainMenuPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS9", skin.DisplayFont, 58f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -62f), new Vector2(430f, 74f));
        title.characterSpacing = 10f;
        title.fontStyle = FontStyles.UpperCase;

        GameObject rail = CreateUiObject("ButtonRail", panel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, -44f), new Vector2(348f, 374f));
        Image railImage = rail.AddComponent<Image>();
        railImage.sprite = skin.PanelBorder;
        railImage.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        railImage.color = PanelColor;
        railImage.raycastTarget = false;

        const float startY = 135f;
        const float step = 56f;
        newGameButton = CreateMenuButton(rail.transform, "NewGameButton", "menu.new_game", "NEW GAME", skin, new Vector2(24f, startY), new Vector2(300f, 43f));
        continueButton = CreateMenuButton(rail.transform, "ContinueButton", "menu.continue", "CONTINUE", skin, new Vector2(24f, startY - step), new Vector2(300f, 43f));
        loadSaveButton = CreateMenuButton(rail.transform, "LoadSaveButton", "menu.load_save", "LOAD / SAVE", skin, new Vector2(24f, startY - step * 2f), new Vector2(300f, 43f));
        settingsButton = CreateMenuButton(rail.transform, "SettingsButton", "menu.settings", "SETTINGS", skin, new Vector2(24f, startY - step * 3f), new Vector2(300f, 43f));
        creditsButton = CreateMenuButton(rail.transform, "CreditsButton", "menu.credits", "CREDITS", skin, new Vector2(24f, startY - step * 4f), new Vector2(300f, 43f));
        exitButton = CreateMenuButton(rail.transform, "ExitButton", "menu.exit", "EXIT", skin, new Vector2(24f, startY - step * 5f), new Vector2(300f, 43f));
        AddIconToButton(newGameButton, skin.ContinueIcon, skin);
        AddIconToButton(continueButton, skin.ContinueIcon, skin);
        AddIconToButton(loadSaveButton, skin.MenuIcon, skin);
        AddIconToButton(settingsButton, skin.ControlsIcon, skin);
        AddIconToButton(creditsButton, skin.InfoIcon, skin);
        AddIconToButton(exitButton, skin.CloseIcon, skin);
        return panel;
    }

    private static GameObject CreateCreditsPanel(Transform parent, FrontendSkin skin, out Button backButton)
    {
        GameObject panel = CreateCenteredPanel(parent, "CreditsPanel", new Vector2(680f, 420f), skin);
        TMP_Text title = CreateLocalizedText(panel.transform, "Title", "menu.credits", "CREDITS", skin.DisplayFont, 34f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(560f, 54f));

        TMP_Text body = CreateLocalizedText(panel.transform, "CreditsText", "menu.credits_text", "VIRUS9", skin.BodyFont, 20f, TextAlignmentOptions.Center, MutedTextColor);
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(560f, 210f));
        body.textWrappingMode = TextWrappingModes.Normal;

        backButton = CreateMenuButton(panel.transform, "CreditsBackButton", "menu.back", "BACK", skin, new Vector2(0f, -158f), new Vector2(240f, 44f), true);
        return panel;
    }

    private static GameObject CreateConfirmPanel(Transform parent, FrontendSkin skin, out TMP_Text confirmText, out Button yesButton, out Button noButton)
    {
        GameObject overlay = CreateUiObject("ConfirmPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.68f);

        GameObject panel = CreateCenteredPanel(overlay.transform, "ConfirmBox", new Vector2(520f, 250f), skin);
        confirmText = CreateText(panel.transform, "ConfirmText", "EXIT THE GAME?", skin.DisplayFont, 26f, TextAlignmentOptions.Center, TextColor);
        SetRect(confirmText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(430f, 86f));
        confirmText.fontSizeMin = 16f;
        confirmText.fontSizeMax = 26f;
        confirmText.textWrappingMode = TextWrappingModes.Normal;

        yesButton = CreateMenuButton(panel.transform, "ConfirmYesButton", "confirm.yes", "YES", skin, new Vector2(-120f, -76f), new Vector2(168f, 42f), true);
        noButton = CreateMenuButton(panel.transform, "ConfirmNoButton", "confirm.no", "NO", skin, new Vector2(120f, -76f), new Vector2(168f, 42f), true);
        return overlay;
    }

    private static GameObject CreateSettingsPanel(Transform parent, FrontendSkin skin)
    {
        GameObject panel = CreateCenteredPanel(parent, "SettingsPanel", new Vector2(920f, 560f), skin);
        AddPanelTexture(panel.GetComponent<RectTransform>(), skin);
        SettingsPanelController controller = panel.AddComponent<SettingsPanelController>();

        TMP_Text title = CreateLocalizedText(panel.transform, "SettingsTitle", "settings.title", "SETTINGS", skin.DisplayFont, 34f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 50f));

        Button audioTab = CreateSettingsTab(panel.transform, "AudioTabButton", "settings.audio", "AUDIO", skin.SoundIcon, skin, 0);
        Button videoTab = CreateSettingsTab(panel.transform, "VideoTabButton", "settings.video", "VIDEO", skin.VideoIcon, skin, 1);
        Button controlsTab = CreateSettingsTab(panel.transform, "ControlsTabButton", "settings.controls", "CONTROLS", skin.ControlsIcon, skin, 2);
        Button languageTab = CreateSettingsTab(panel.transform, "LanguageTabButton", "settings.language", "LANGUAGE", skin.LanguageIcon, skin, 3);
        Button accessibilityTab = CreateSettingsTab(panel.transform, "AccessibilityTabButton", "settings.accessibility", "ACCESSIBILITY", skin.AccessibilityIcon, skin, 4);

        GameObject audioPanel = CreateSettingsContentPanel(panel.transform, "AudioContentPanel", skin);
        GameObject videoPanel = CreateSettingsContentPanel(panel.transform, "VideoContentPanel", skin);
        GameObject controlsPanel = CreateSettingsContentPanel(panel.transform, "ControlsContentPanel", skin);
        GameObject languagePanel = CreateSettingsContentPanel(panel.transform, "LanguageContentPanel", skin);
        GameObject accessibilityPanel = CreateSettingsContentPanel(panel.transform, "AccessibilityContentPanel", skin);

        Slider masterVolume = CreateSettingsSlider(audioPanel.transform, "masterVolume", "settings.master", "MASTER VOLUME", skin);
        Slider musicVolume = CreateSettingsSlider(audioPanel.transform, "musicVolume", "settings.music", "MUSIC", skin);
        Slider sfxVolume = CreateSettingsSlider(audioPanel.transform, "sfxVolume", "settings.sfx", "SFX", skin);
        Slider voiceVolume = CreateSettingsSlider(audioPanel.transform, "voiceVolume", "settings.voice", "VOICE / DIALOGUE", skin);
        Toggle muteAll = CreateSettingsToggle(audioPanel.transform, "muteAll", "settings.mute", "MUTE ALL", skin);

        TMP_Dropdown resolution = CreateSettingsDropdown(videoPanel.transform, "resolution", "settings.resolution", "RESOLUTION", skin);
        Toggle fullscreen = CreateSettingsToggle(videoPanel.transform, "fullscreen", "settings.fullscreen", "FULLSCREEN", skin);
        Slider brightness = CreateSettingsSlider(videoPanel.transform, "brightness", "settings.brightness", "BRIGHTNESS", skin);
        TMP_Dropdown quality = CreateSettingsDropdown(videoPanel.transform, "quality", "settings.quality", "QUALITY", skin);
        Toggle vSync = CreateSettingsToggle(videoPanel.transform, "vSync", "settings.vsync", "VSYNC", skin);

        Slider mouseSensitivity = CreateSettingsSlider(controlsPanel.transform, "mouseSensitivity", "settings.mouse_sensitivity", "MOUSE SENSITIVITY", skin);
        Toggle invertY = CreateSettingsToggle(controlsPanel.transform, "invertY", "settings.invert_y", "INVERT Y AXIS", skin);
        TMP_Text controls = CreateLocalizedText(controlsPanel.transform, "StaticControls", "settings.static_controls", "WASD - run", skin.BodyFont, 16f, TextAlignmentOptions.TopLeft, MutedTextColor);
        controls.textWrappingMode = TextWrappingModes.Normal;

        TMP_Dropdown languageDropdown = CreateSettingsDropdown(languagePanel.transform, "language", "settings.language", "LANGUAGE", skin);

        Toggle subtitles = CreateSettingsToggle(accessibilityPanel.transform, "subtitles", "settings.subtitles", "SUBTITLES", skin);
        TMP_Dropdown subtitleSize = CreateSettingsDropdown(accessibilityPanel.transform, "subtitleSize", "settings.subtitle_size", "SUBTITLE SIZE", skin);
        Slider uiScale = CreateSettingsSlider(accessibilityPanel.transform, "uiScale", "settings.ui_scale", "UI SCALE", skin);
        Toggle highContrast = CreateSettingsToggle(accessibilityPanel.transform, "highContrast", "settings.high_contrast", "HIGH CONTRAST UI", skin);
        Toggle colorblindFriendly = CreateSettingsToggle(accessibilityPanel.transform, "colorblindFriendly", "settings.colorblind", "COLORBLIND-FRIENDLY MODE", skin);
        Toggle reduceScreenShake = CreateSettingsToggle(accessibilityPanel.transform, "reduceScreenShake", "settings.reduce_shake", "REDUCE SCREEN SHAKE", skin);
        Toggle reduceMotion = CreateSettingsToggle(accessibilityPanel.transform, "reduceMotion", "settings.reduce_motion", "REDUCE MOTION", skin);
        Toggle tutorialHints = CreateSettingsToggle(accessibilityPanel.transform, "tutorialHints", "settings.tutorial_hints", "TUTORIAL HINTS", skin);
        Toggle holdInsteadOfRepeat = CreateSettingsToggle(accessibilityPanel.transform, "holdInsteadOfRepeat", "settings.hold_mode", "HOLD INSTEAD OF REPEATED PRESS", skin);
        Toggle toggleRun = CreateSettingsToggle(accessibilityPanel.transform, "toggleRun", "settings.toggle_run", "TOGGLE RUN", skin);
        Toggle simplePrompts = CreateSettingsToggle(accessibilityPanel.transform, "simplePrompts", "settings.simple_prompts", "SIMPLE INTERACTION PROMPTS", skin);

        Button backButton = CreateMenuButton(panel.transform, "SettingsBackButton", "menu.back", "BACK", skin, new Vector2(0f, 30f), new Vector2(220f, 40f), true);
        AddIconToButton(backButton, skin.BackIcon, skin);

        SetActiveForPrefab(audioPanel, true);
        SetActiveForPrefab(videoPanel, false);
        SetActiveForPrefab(controlsPanel, false);
        SetActiveForPrefab(languagePanel, false);
        SetActiveForPrefab(accessibilityPanel, false);

        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "tabPanels", new UnityEngine.Object[] { audioPanel, videoPanel, controlsPanel, languagePanel, accessibilityPanel });
        SetArray(serialized, "tabButtons", new UnityEngine.Object[] { audioTab, videoTab, controlsTab, languageTab, accessibilityTab });
        SetObject(serialized, "backButton", backButton);
        SetObject(serialized, "masterVolume", masterVolume);
        SetObject(serialized, "musicVolume", musicVolume);
        SetObject(serialized, "sfxVolume", sfxVolume);
        SetObject(serialized, "voiceVolume", voiceVolume);
        SetObject(serialized, "muteAll", muteAll);
        SetObject(serialized, "resolution", resolution);
        SetObject(serialized, "fullscreen", fullscreen);
        SetObject(serialized, "brightness", brightness);
        SetObject(serialized, "quality", quality);
        SetObject(serialized, "vSync", vSync);
        SetObject(serialized, "mouseSensitivity", mouseSensitivity);
        SetObject(serialized, "invertY", invertY);
        SetObject(serialized, "language", languageDropdown);
        SetObject(serialized, "subtitles", subtitles);
        SetObject(serialized, "subtitleSize", subtitleSize);
        SetObject(serialized, "uiScale", uiScale);
        SetObject(serialized, "highContrast", highContrast);
        SetObject(serialized, "colorblindFriendly", colorblindFriendly);
        SetObject(serialized, "reduceScreenShake", reduceScreenShake);
        SetObject(serialized, "reduceMotion", reduceMotion);
        SetObject(serialized, "tutorialHints", tutorialHints);
        SetObject(serialized, "holdInsteadOfRepeat", holdInsteadOfRepeat);
        SetObject(serialized, "toggleRun", toggleRun);
        SetObject(serialized, "simplePrompts", simplePrompts);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static Button CreateSettingsTab(Transform parent, string name, string key, string fallback, Sprite icon, FrontendSkin skin, int index)
    {
        Button button = CreateMenuButton(parent, name, key, fallback, skin, new Vector2(24f, -112f - index * 56f), new Vector2(210f, 42f));
        AddIconToButton(button, icon, skin);
        return button;
    }

    private static GameObject CreateSettingsContentPanel(Transform parent, string name, FrontendSkin skin)
    {
        GameObject panel = CreateUiObject(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -112f), new Vector2(620f, 378f));
        Image image = panel.AddComponent<Image>();
        image.sprite = skin.PanelBorder;
        image.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0.022f, 0.052f, 0.04f, 0.92f);
        AddPanelTexture(panel.GetComponent<RectTransform>(), skin);
        return panel;
    }

    private static Slider CreateSettingsSlider(Transform parent, string baseName, string key, string fallback, FrontendSkin skin)
    {
        CreateLocalizedText(parent, baseName + "_Label", key, fallback, skin.BodyFont, 16f, TextAlignmentOptions.MidlineLeft, TextColor);

        GameObject root = CreateUiObject(baseName + "_Slider", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 24f));
        Slider slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        GameObject background = CreateUiObject("Background", root.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 8f));
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.035f, 0.12f, 0.14f, 1f);

        GameObject fillArea = CreateUiObject("Fill Area", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f));
        GameObject fill = CreateUiObject("Fill", fillArea.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 10f));
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = AccentColor;

        GameObject handleArea = CreateUiObject("Handle Slide Area", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f));
        GameObject handle = CreateUiObject("Handle", handleArea.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 42f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.58f, 0.14f, 1f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        return slider;
    }

    private static TMP_Dropdown CreateSettingsDropdown(Transform parent, string baseName, string key, string fallback, FrontendSkin skin)
    {
        CreateLocalizedText(parent, baseName + "_Label", key, fallback, skin.BodyFont, 16f, TextAlignmentOptions.MidlineLeft, TextColor);
        TMP_Dropdown dropdown = CreateDropdown(parent, baseName + "_Dropdown", skin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 36f));
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData(fallback));
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static Toggle CreateSettingsToggle(Transform parent, string baseName, string key, string fallback, FrontendSkin skin)
    {
        GameObject root = CreateUiObject(baseName + "_Toggle", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580f, 30f));
        Toggle toggle = root.AddComponent<Toggle>();

        GameObject background = CreateUiObject("Background", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.06f, 0.18f, 0.12f, 1f);

        GameObject checkmark = CreateUiObject("Checkmark", background.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.sprite = skin.TickIcon;
        checkmarkImage.preserveAspect = true;
        checkmarkImage.color = new Color(1f, 0.58f, 0.14f, 1f);

        TMP_Text label = CreateLocalizedText(root.transform, "Label", key, fallback, skin.BodyFont, 16f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(-34f, 30f));

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        toggle.isOn = false;
        return toggle;
    }

    private static GameObject CreateSaveSlotPanel(Transform parent, FrontendSkin skin)
    {
        GameObject panel = CreateCenteredPanel(parent, "SaveSlotPanel", new Vector2(780f, 520f), skin);
        SaveSlotPanelController controller = panel.AddComponent<SaveSlotPanelController>();

        TMP_Text title = CreateLocalizedText(panel.transform, "Title", "menu.load_save", "LOAD / SAVE", skin.DisplayFont, 34f, TextAlignmentOptions.Center, TextColor);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 54f));

        TMP_Text[] slotLabels = new TMP_Text[3];
        Button[] loadButtons = new Button[3];
        Button[] saveButtons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            float y = 132f - i * 94f;
            GameObject row = CreateUiObject($"Slot{i + 1}_Row", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(640f, 72f));
            Image rowImage = row.AddComponent<Image>();
            rowImage.sprite = skin.PanelBorder;
            rowImage.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
            rowImage.color = PanelColor;

            slotLabels[i] = CreateText(row.transform, $"Slot{i + 1}_Label", $"SLOT {i + 1}", skin.BodyFont, 17f, TextAlignmentOptions.MidlineLeft, TextColor);
            SetRect(slotLabels[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(314f, 56f));
            slotLabels[i].textWrappingMode = TextWrappingModes.Normal;
            slotLabels[i].fontSizeMin = 12f;
            slotLabels[i].fontSizeMax = 17f;

            loadButtons[i] = CreateMenuButton(row.transform, $"Slot{i + 1}_LoadButton", "save.load", "LOAD", skin, new Vector2(410f, 0f), new Vector2(100f, 36f), true);
            saveButtons[i] = CreateMenuButton(row.transform, $"Slot{i + 1}_SaveButton", "save.save", "SAVE", skin, new Vector2(530f, 0f), new Vector2(100f, 36f), true);
        }

        Button backButton = CreateMenuButton(panel.transform, "SaveBackButton", "menu.back", "BACK", skin, new Vector2(0f, -210f), new Vector2(220f, 42f), true);

        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "slotLabels", slotLabels);
        SetArray(serialized, "loadButtons", loadButtons);
        SetArray(serialized, "saveButtons", saveButtons);
        SetObject(serialized, "backButton", backButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static void CreateBackground(Transform parent, FrontendSkin skin)
    {
        GameObject background = CreateUiObject("Background", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = BackgroundColor;
        backgroundImage.raycastTarget = false;

        if (skin.Background != null)
        {
            GameObject grunge = CreateUiObject("MenuBackgroundGrunge", background.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image grungeImage = grunge.AddComponent<Image>();
            grungeImage.sprite = skin.Background;
            grungeImage.color = new Color(0.24f, 0.36f, 0.22f, 0.38f);
            grungeImage.preserveAspect = false;
            grungeImage.raycastTarget = false;
        }

        BuildMenuAtmosphere(background, skin);

        GameObject vignette = CreateUiObject("DarkVignette", background.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image vignetteImage = vignette.AddComponent<Image>();
        vignetteImage.color = new Color(0f, 0f, 0f, 0.1f);
        vignetteImage.raycastTarget = false;

        GameObject strip = CreateUiObject("AccentStrip", background.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(3f, 0f));
        Image stripImage = strip.AddComponent<Image>();
        stripImage.color = AccentMutedColor;
        stripImage.raycastTarget = false;

        TMP_Text version = CreateText(background.transform, "BuildMarker", "MENU_BOOT // YOU MAY LIVE 2", skin.BodyFont, 13f, TextAlignmentOptions.Right, MutedTextColor);
        SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-58f, 28f), new Vector2(300f, 24f));
    }

    private static TMP_Dropdown CreateLanguageDropdown(Transform parent, FrontendSkin skin)
    {
        TMP_Dropdown dropdown = CreateDropdown(parent, "LanguageDropdown", skin, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -22f), new Vector2(214f, 36f));
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("РУССКИЙ"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("ENGLISH"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("PORTUGUÊS"));
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static GameObject CreateCenteredPanel(Transform parent, string name, Vector2 size, FrontendSkin skin)
    {
        GameObject panel = CreateUiObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.sprite = skin.PanelBorder;
        image.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = PanelStrongColor;
        image.raycastTarget = true;
        return panel;
    }

    private static Button CreateMenuButton(Transform parent, string name, string key, string fallback, FrontendSkin skin, Vector2 position, Vector2 size, bool centered = false)
    {
        GameObject buttonObject = CreateUiObject(name, parent, centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = skin.Button;
        image.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color(0.075f, 0.17f, 0.16f, 1f);
        colors.pressedColor = AccentMutedColor;
        colors.selectedColor = new Color(0.07f, 0.15f, 0.14f, 1f);
        colors.disabledColor = new Color(0.025f, 0.04f, 0.045f, 0.72f);
        button.colors = colors;

        TMP_Text label = CreateLocalizedText(buttonObject.transform, "Label", key, fallback, skin.DisplayFont, 18f, TextAlignmentOptions.Center, TextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.fontSizeMin = 11f;
        label.fontSizeMax = 18f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static Button CreateSpriteButton(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(name, parent, anchorMin, anchorMax, pivot, position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.78f, 1f, 0.94f, 1f);
        colors.pressedColor = new Color(0.45f, 0.88f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        return button;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, FrontendSkin skin, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject root = CreateUiObject(name, parent, anchorMin, anchorMax, pivot, position, size);
        Image image = root.AddComponent<Image>();
        image.sprite = skin.Button;
        image.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0.026f, 0.06f, 0.068f, 0.98f);

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.alphaFadeSpeed = 0.06f;

        TMP_Text label = CreateText(root.transform, "Label", "РУССКИЙ", skin.BodyFont, 15f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(-42f, 0f));
        label.fontSizeMin = 10f;
        label.fontSizeMax = 15f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        dropdown.captionText = label;

        TMP_Text arrow = CreateText(root.transform, "Arrow", "v", skin.DisplayFont, 16f, TextAlignmentOptions.Center, AccentColor);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-17f, 0f), new Vector2(28f, 0f));

        GameObject template = CreateUiObject("Template", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(0f, 112f));
        Image templateImage = template.AddComponent<Image>();
        templateImage.sprite = skin.PanelBorder;
        templateImage.type = skin.PanelBorder != null ? Image.Type.Sliced : Image.Type.Simple;
        templateImage.color = new Color(0.012f, 0.032f, 0.036f, 0.995f);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        template.AddComponent<CanvasGroup>();

        GameObject viewport = CreateUiObject("Viewport", template.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUiObject("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 102f));
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform contentRect = content.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject item = CreateUiObject("Item", content.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 30f));
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 30f;
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.sprite = skin.Select != null ? skin.Select : skin.Button;
        itemBackground.type = itemBackground.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        itemBackground.color = new Color(0.02f, 0.06f, 0.06f, 0.3f);
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        toggle.transition = Selectable.Transition.ColorTint;
        ColorBlock toggleColors = toggle.colors;
        toggleColors.normalColor = new Color(1f, 1f, 1f, 0.45f);
        toggleColors.highlightedColor = new Color(0.34f, 0.9f, 0.78f, 0.85f);
        toggleColors.pressedColor = new Color(0.2f, 0.55f, 0.5f, 0.95f);
        toggleColors.selectedColor = new Color(0.22f, 0.68f, 0.6f, 0.95f);
        toggle.colors = toggleColors;

        TMP_Text itemLabel = CreateText(item.transform, "Item Label", "Option", skin.BodyFont, 15f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
        itemLabel.fontSizeMin = 10f;
        itemLabel.fontSizeMax = 15f;
        itemLabel.textWrappingMode = TextWrappingModes.NoWrap;

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
        text.fontSizeMin = Mathf.Max(10f, size - 8f);
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
        if (rect == null) return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetStretchOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
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

    private static void SetVector2(SerializedObject serialized, string propertyName, Vector2 value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null) property.vector2Value = value;
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
