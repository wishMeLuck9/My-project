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
    private const string UiSymbolSourceFontPath = "Assets/Fonts/UI/VIRUS9-SegoeUISymbol.ttf";
    private const string UiSymbolFontAssetPath = "Assets/Fonts/UI/VIRUS9_UI_Symbols.asset";
    private const string WindowsSymbolFontPath = @"C:\Windows\Fonts\seguisym.ttf";
    private const string TmpDefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string TmpFallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
    private const string TmpLiberationSansFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string MenuMusicPath = "Assets/Audio/Music/YouMayLive2.mp3";
    private const string MenuSkinFolder = "Assets/Art/UI/Virus9Skin";
    private const string MenuBackgroundPath = "Assets/Art/UI/Virus9Skin/Virus9_Background.png";
    private const string MenuBorderPath = "Assets/Art/UI/Virus9Skin/Virus9_PanelBorder.png";
    private const string MenuBorderThinPath = "Assets/Art/UI/Virus9Skin/Virus9_PanelBorderThin.png";
    private const string MenuButtonPath = "Assets/Art/UI/Virus9Skin/Virus9_ButtonRetro.png";
    private const string MenuSelectPath = "Assets/Art/UI/Virus9Skin/Virus9_Select.png";
    private const string MenuPopupPath = "Assets/Art/UI/Virus9Skin/Virus9_Popup.png";
    private const string MenuSliderTrackPath = "Assets/Art/UI/Virus9Skin/Virus9_SliderTrack.png";
    private const string MenuSliderFillPath = "Assets/Art/UI/Virus9Skin/Virus9_SliderFill.png";
    private const string PauseFogTexturePath = "Assets/Art/VFX/JohnLemon/perlinNoise.png";
    private const string PauseDustTexturePath = "Assets/Art/VFX/JohnLemon/DustMote.png";
    private const string PauseAmbiencePath = "Assets/Audio/Ambience/JohnLemon/SFXHouseAmbience.wav";
    private const string PauseBuzzPath = "Assets/Audio/Ambience/JohnLemon/SFXBuzzingLight.wav";
    private const string RequiredUiTextCharacters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
        "ÀÁÂÃÇÉÊÍÓÔÕÚÜàáâãçéêíóôõúüÊêÍíÓóÚúÇçÃãÕõ";
    private const string RequiredUiSymbolCharacters = "\u25BC\u2713\u25A0\u25A1";
    private const string RequiredUiFontCharacters = RequiredUiTextCharacters + RequiredUiSymbolCharacters;
    private const string RequiredTmpFallbackCharacters = RequiredUiTextCharacters + "\u25BC\u25A0\u25A1";

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
        TMP_FontAsset symbolFont = EnsureUiSymbolFontAsset();
        TMP_FontAsset tmpFallbackFont = EnsureTmpFallbackFontAsset();
        ApplyUiFontFallbacks(symbolFont, tmpFallbackFont, uiFont, uiBoldFont, portugueseFont);

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
            PanelBorderThin = LoadSprite(MenuBorderThinPath, new Vector4(14f, 14f, 14f, 14f));
            Button = LoadSprite(MenuButtonPath, new Vector4(18f, 18f, 18f, 18f));
            Select = LoadSprite(MenuSelectPath, new Vector4(18f, 18f, 18f, 18f));
            Popup = LoadSprite(MenuPopupPath, new Vector4(24f, 24f, 24f, 24f));
            SliderTrack = LoadSprite(MenuSliderTrackPath, new Vector4(14f, 14f, 14f, 14f));
            SliderFill = LoadSprite(MenuSliderFillPath, new Vector4(14f, 14f, 14f, 14f));
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
        public Sprite PanelBorderThin { get; }
        public Sprite Button { get; }
        public Sprite Select { get; }
        public Sprite Popup { get; }
        public Sprite SliderTrack { get; }
        public Sprite SliderFill { get; }
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
        Directory.CreateDirectory(MenuSkinFolder);
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
            if (!fontAsset.TryAddCharacters(RequiredUiTextCharacters, out string missingCharacters) && !string.IsNullOrEmpty(missingCharacters))
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

    private static TMP_FontAsset EnsureUiSymbolFontAsset()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSymbolFontAssetPath);
        if (fontAsset != null && !HasValidSymbolFontAsset(fontAsset))
        {
            AssetDatabase.DeleteAsset(UiSymbolFontAssetPath);
            fontAsset = null;
        }

        if (fontAsset == null)
        {
            Font sourceFont = EnsureUiSymbolSourceFontImported();
            if (sourceFont == null)
            {
                Debug.LogWarning("VIRUS9 symbol fallback skipped because no symbol source font was available.");
                return null;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                512,
                512,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                Debug.LogWarning("VIRUS9 symbol fallback creation failed.");
                return null;
            }

            fontAsset.name = "VIRUS9_UI_Symbols";
            if (!fontAsset.TryAddCharacters(RequiredUiSymbolCharacters, out string missingCharacters) && !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"VIRUS9 symbol fallback could not pack UI characters: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fontAsset, UiSymbolFontAssetPath);
            AddFontSubAssets(fontAsset);
        }

        fontAsset.name = "VIRUS9_UI_Symbols";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    private static Font EnsureUiSymbolSourceFontImported()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(UiSymbolSourceFontPath);
        if (sourceFont != null) return sourceFont;

        if (!File.Exists(WindowsSymbolFontPath))
        {
            Debug.LogWarning($"VIRUS9 symbol source font missing: {WindowsSymbolFontPath}");
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(UiSymbolSourceFontPath) ?? FontsFolder);
        File.Copy(WindowsSymbolFontPath, Path.GetFullPath(UiSymbolSourceFontPath), true);
        AssetDatabase.ImportAsset(UiSymbolSourceFontPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Font>(UiSymbolSourceFontPath);
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
            if (!fallbackFont.TryAddCharacters(RequiredTmpFallbackCharacters, out string missingCharacters) && !string.IsNullOrEmpty(missingCharacters))
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

    private static void ApplyUiFontFallbacks(TMP_FontAsset symbolFont, TMP_FontAsset tmpFallbackFont, params TMP_FontAsset[] primaryFonts)
    {
        foreach (TMP_FontAsset primaryFont in primaryFonts)
        {
            SetFontFallbacks(primaryFont, symbolFont, tmpFallbackFont);
        }

        TMP_FontAsset defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpDefaultFontPath);
        SetFontFallbacks(defaultFont, symbolFont, tmpFallbackFont);
    }

    private static void SetFontFallbacks(TMP_FontAsset fontAsset, params TMP_FontAsset[] fallbacks)
    {
        if (fontAsset == null) return;

        if (fontAsset.fallbackFontAssetTable == null)
        {
            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        fontAsset.fallbackFontAssetTable.Clear();
        foreach (TMP_FontAsset fallback in fallbacks)
        {
            if (fallback == null || fallback == fontAsset || fontAsset.fallbackFontAssetTable.Contains(fallback)) continue;
            fontAsset.fallbackFontAssetTable.Add(fallback);
        }

        EditorUtility.SetDirty(fontAsset);
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

        return ContainsRequiredCharacters(fontAsset, RequiredTmpFallbackCharacters);
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

        return ContainsRequiredCharacters(fontAsset, RequiredUiTextCharacters);
    }

    private static bool HasValidSymbolFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null ||
            fontAsset.atlasTextures == null ||
            fontAsset.atlasTextures.Length == 0 ||
            fontAsset.atlasTextures[0] == null ||
            fontAsset.atlasTextures[0].width < 256 ||
            fontAsset.atlasTextures[0].height < 256)
        {
            return false;
        }

        return ContainsRequiredCharacters(fontAsset, RequiredUiSymbolCharacters);
    }

    private static bool ContainsRequiredCharacters(TMP_FontAsset fontAsset, string requiredCharacters)
    {
        for (int i = 0; i < requiredCharacters.Length; i++)
        {
            if (!ContainsCharacter(fontAsset, requiredCharacters[i])) return false;
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

        List<LocalizationCatalog.Entry> entries = DeduplicateEntriesKeepingLast(catalog.Entries);

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
        Upsert(entries, "raw.return_gate.prompt",
            "Обратные врата открыты. Вернуться к предыдущему квадрату?",
            "The return gate is open. Go back to the previous square?",
            "Os portoes de regresso estao abertos. Voltar ao quadrado anterior?");
        Upsert(entries, "raw.return_gate.shoot_prompt",
            "Обратные врата открыты. Выстрели в раму, чтобы вернуться назад.",
            "The return gate is open. Shoot the frame to return.",
            "O portal de regresso esta aberto. Dispara contra a moldura para voltar.");
        Upsert(entries, "raw.return_gate.locked",
            "Обратный маршрут еще не записан. Сначала нужен фрагмент.",
            "The return route is not recorded yet. A fragment must anchor it first.",
            "A rota de regresso ainda nao foi registada. Primeiro precisas de um fragmento.");
        Upsert(entries, "raw.return_gate.shot",
            "Рама приняла удар. Маршрут сворачивается назад.",
            "The frame takes the strike. The route folds backward.",
            "A moldura aceitou o impacto. A rota dobra para tras.");
        Upsert(entries, "raw.return_gate.enter",
            "Вернуться",
            "Return",
            "Voltar");
        Upsert(entries, "raw.return_gate.leave",
            "Остаться",
            "Stay",
            "Ficar");
        Upsert(entries, "raw.night.training.prompt",
            "Сила уже в руке. Попробуй ударить по неподвижной тени, прежде чем ночь заметит тебя.",
            "The force is already in your hand. Strike the still shadow before the night notices you.",
            "A forca ja esta na tua mao. Atinge a sombra parada antes que a noite repare em ti.");
        Upsert(entries, "raw.night.training.hit",
            "Пространство треснуло. Теперь живые тени тоже почувствуют удар.",
            "The space cracked. Now the living shadows will feel the strike too.",
            "O espaco rachou. Agora as sombras vivas tambem vao sentir o golpe.");
        Upsert(entries, "raw.night.training.release",
            "Ночь услышала. Теперь двигайся.",
            "The night heard it. Move now.",
            "A noite ouviu. Agora mexe-te.");

        ApplyReadableLocalizationOverrides(entries);
        catalog.ReplaceEntries(entries);
        EditorUtility.SetDirty(catalog);
    }

    private static List<LocalizationCatalog.Entry> DeduplicateEntriesKeepingLast(IReadOnlyList<LocalizationCatalog.Entry> source)
    {
        Dictionary<string, LocalizationCatalog.Entry> byKey = new Dictionary<string, LocalizationCatalog.Entry>(StringComparer.Ordinal);
        List<string> keyOrder = new List<string>();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                LocalizationCatalog.Entry entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                if (!byKey.ContainsKey(entry.key)) keyOrder.Add(entry.key);
                byKey[entry.key] = entry;
            }
        }

        List<LocalizationCatalog.Entry> result = new List<LocalizationCatalog.Entry>(keyOrder.Count);
        for (int i = 0; i < keyOrder.Count; i++)
        {
            result.Add(byKey[keyOrder[i]]);
        }

        return result;
    }

    private static void ApplyReadableLocalizationOverrides(List<LocalizationCatalog.Entry> entries)
    {
        Upsert(entries, "speaker.gate", "ВРАТА", "GATE", "PORTOES");
        Upsert(entries, "speaker.system", "SYSTEM", "SYSTEM", "SYSTEM");
        Upsert(entries, "speaker.price_altar", "АЛТАРЬ ЦЕНЫ", "PRICE ALTAR", "ALTAR DO PRECO");
        Upsert(entries, "dialogue.continue", "Продолжить", "Continue", "Continuar");
        Upsert(entries, "dialogue.next", "Далее ({0}/{1})", "Next ({0}/{1})", "Seguinte ({0}/{1})");

        Upsert(entries, "intro.gameplay.1",
            "ANTBORN // зона до рождения.\nЗдесь тени ждут, пока мир признает их существование.",
            "ANTBORN // pre-birth zone.\nShadows wait here until the world recognizes them.",
            "ANTBORN // zona antes do nascimento.\nAs sombras esperam aqui ate o mundo as reconhecer.");
        Upsert(entries, "intro.gameplay.2",
            "Ты появился как ошибка: маленькая доброта стала душой, которую система не умеет записать.",
            "You appeared as an error: a small kindness became a soul the system cannot record.",
            "Apareceste como um erro: uma pequena bondade tornou-se uma alma que o sistema nao consegue registar.");
        Upsert(entries, "intro.gameplay.3",
            "Система не злится. Она сверяет правила. Для нее ты запись без разрешения на жизнь.",
            "The System is not angry. It checks the rules. To it, you are a record without permission to live.",
            "O Sistema nao esta zangado. Verifica as regras. Para ele, es um registo sem permissao para viver.");
        Upsert(entries, "intro.gameplay.4",
            "Иди к свету. Собери фрагмент. Когда Врата появятся, они попросят цену.",
            "Follow the light. Take the fragment. When the Gate appears, it will ask for a price.",
            "Segue a luz. Apanha o fragmento. Quando os Portoes aparecerem, vao pedir um preco.");
        Upsert(entries, "intro.gameplay.5",
            "Управление простое: двигайся, прыгай, карабкайся рядом с краем и взаимодействуй через E.",
            "The rules are simple: move, jump, climb near edges, and interact with E.",
            "As regras sao simples: move-te, salta, trepa junto a beiras e interage com E.");

        Upsert(entries, "raw.night.intro.square",
            "Ты прошел через Врата и оказался во втором квадрате.",
            "You passed through the Gate and entered the second square.",
            "Passaste pelos Portoes e entraste no segundo quadrado.");
        Upsert(entries, "raw.night.intro.attack",
            "Ночь дает тебе силу удара. Сначала проверь ее на неподвижной тени.",
            "The night gives you a strike. Test it on the still shadow first.",
            "A noite da-te um golpe. Testa-o primeiro na sombra parada.");
        Upsert(entries, "raw.night.intro.shadows",
            "Живые тени почувствуют эту силу. Одни испугаются. Другие начнут охоту.",
            "Living shadows will feel that force. Some will fear it. Others will hunt.",
            "As sombras vivas vao sentir essa forca. Algumas terao medo. Outras vao cacar.");
        Upsert(entries, "raw.night.intro.hunted",
            "Выбор простой: пройти мимо или превратить ночь в бой. Второй фрагмент появится после твоего выбора.",
            "The choice is simple: pass through, or turn the night into a fight. The second fragment appears after your choice.",
            "A escolha e simples: passa adiante ou transforma a noite numa luta. O segundo fragmento aparece depois da tua escolha.");
        Upsert(entries, "raw.night.training.prompt",
            "Сила уже в руке. Ударь неподвижную тень, затем нажми «Продолжить».",
            "The force is already in your hand. Strike the still shadow, then press Continue.",
            "A forca ja esta na tua mao. Atinge a sombra parada e depois prime Continuar.");
        Upsert(entries, "raw.night.training.hit",
            "Пространство треснуло. Живые тени теперь тоже почувствуют удар.",
            "The space cracked. The living shadows will feel the strike now.",
            "O espaco rachou. As sombras vivas vao sentir o golpe agora.");
        Upsert(entries, "raw.night.training.release",
            "Ночь признала твою силу. Теперь двигайся: живые тени услышали удар.",
            "The night recognized your force. Move now: the living shadows heard the strike.",
            "A noite reconheceu a tua forca. Mexe-te: as sombras vivas ouviram o golpe.");
        Upsert(entries, "raw.night.observe",
            "Не подходи. Просто посмотри, что они делают друг с другом.",
            "Do not come closer. Watch what they do to each other.",
            "Nao te aproximes. Observa o que fazem umas as outras.");
        Upsert(entries, "raw.night.mercy",
            "Она встала. Фрагмент появился на дороге. Подбери его и иди к Вратам.",
            "She stood up. The fragment appeared on the road. Pick it up and go to the Gate.",
            "Ela levantou-se. O fragmento apareceu na estrada. Apanha-o e vai aos Portoes.");
        Upsert(entries, "raw.night.drop",
            "Площадь опустела. Фрагмент появился на дороге. Подбери его и иди к Вратам.",
            "The square is empty. The fragment appeared on the road. Pick it up and go to the Gate.",
            "O quadrado ficou vazio. O fragmento apareceu na estrada. Apanha-o e vai aos Portoes.");
        Upsert(entries, "raw.fragment.night.prompt",
            "Второй фрагмент лежит перед тобой. Взять его с собой?",
            "The second fragment lies before you. Take it with you?",
            "O segundo fragmento esta diante de ti. Leva-lo contigo?");
        Upsert(entries, "raw.fragment.night.collected",
            "Второй фрагмент принят. Теперь Врата смогут назвать цену.",
            "Second fragment accepted. The Gate can now name its price.",
            "Segundo fragmento aceite. Os Portoes podem agora indicar o preco.");
        Upsert(entries, "raw.transition.locked.second",
            "До Врат нельзя дойти без второго фрагмента. Вернись и подбери след ночи.",
            "You cannot reach the Gate without the second fragment. Go back and pick up the night trace.",
            "Nao podes chegar aos Portoes sem o segundo fragmento. Volta e apanha o rasto da noite.");

        Upsert(entries, "raw.exterior.fragment.awakening",
            "Фрагмент принят. Свет на тебе будет вести к Вратам. Тени тоже увидели его.",
            "Fragment accepted. The light on you will lead to the Gate. The shadows saw it too.",
            "Fragmento aceite. A luz em ti vai guiar-te aos Portoes. As sombras tambem a viram.");
        Upsert(entries, "raw.exterior.gate.reveal",
            "Врата проявились на краю квадрата.",
            "The Gate appeared at the edge of the square.",
            "Os Portoes apareceram na beira do quadrado.");
        Upsert(entries, "raw.exterior.gate.cutscene.found",
            "Ты дошел до Врат. Они не открываются от одного фрагмента.",
            "You reached the Gate. One fragment is not enough to open it.",
            "Chegaste aos Portoes. Um fragmento nao basta para os abrir.");
        Upsert(entries, "raw.exterior.gate.cutscene.vision",
            "Позади собираются тени. Они видят путь, который ты украл у системы.",
            "Shadows gather behind you. They see the route you stole from the System.",
            "As sombras juntam-se atras de ti. Veem a rota que roubaste ao Sistema.");
        Upsert(entries, "raw.exterior.gate.cutscene.prompt",
            "Выстрели во Врата, чтобы пройти дальше.",
            "Shoot the Gate to move forward.",
            "Dispara contra os Portoes para avancar.");
        Upsert(entries, "raw.exterior.gate.cutscene.shadows",
            "Ты обернулся. Тени вокруг не нападают. Они смотрят, как проход открывается.",
            "You turned back. The shadows around you do not attack. They watch the passage open.",
            "Olhaste para tras. As sombras a tua volta nao atacam. Observam a passagem abrir.");
        Upsert(entries, "raw.exterior.gate.cutscene.impossible",
            "Проход открыт. Первый квадрат отпускает тебя.",
            "The passage is open. The first square lets you go.",
            "A passagem esta aberta. O primeiro quadrado deixa-te partir.");

        Upsert(entries, "raw.final.intro.guardians",
            "Перед Вратами стоят стражи. Они защищают порядок от ошибок вроде тебя.",
            "Guardians stand before the Gate. They protect order from errors like you.",
            "Guardioes estao diante dos Portoes. Protegem a ordem de erros como tu.");
        Upsert(entries, "raw.final.intro.boundaries",
            "Здесь каждый шаг ограничен. Стражи держат границы своей волей.",
            "Every step is limited here. The guardians hold the boundaries by will.",
            "Aqui cada passo e limitado. Os guardioes seguram as fronteiras pela vontade.");
        Upsert(entries, "raw.final.intro.violent",
            "Ты принес долг ночи. Для Врат это путь силы.",
            "You brought the debt of the night. To the Gate, this is the path of force.",
            "Trouxeste a divida da noite. Para os Portoes, este e o caminho da forca.");
        Upsert(entries, "raw.final.intro.attack",
            "Стражи не предложат цену. Они попробуют остановить тебя.",
            "The guardians will not offer a price. They will try to stop you.",
            "Os guardioes nao vao oferecer um preco. Vao tentar travar-te.");
        Upsert(entries, "raw.final.intro.peaceful",
            "Ты принес второй фрагмент, но не превратил ночь в бой.",
            "You brought the second fragment without turning the night into a fight.",
            "Trouxeste o segundo fragmento sem transformar a noite numa luta.");
        Upsert(entries, "raw.final.intro.queue",
            "Врата видят ошибку, которая дошла до конца маршрута.",
            "The Gate sees an error that reached the end of the route.",
            "Os Portoes veem um erro que chegou ao fim da rota.");
        Upsert(entries, "raw.final.intro.offer",
            "Теперь они назовут цену. Ты выберешь, что сделать с фрагментами.",
            "Now they will name the price. You choose what to do with the fragments.",
            "Agora vao indicar o preco. Tu escolhes o que fazer com os fragmentos.");
        Upsert(entries, "raw.gate.incomplete",
            "Маршрут неполный. Вернись и забери недостающий фрагмент.",
            "The route is incomplete. Go back and take the missing fragment.",
            "A rota esta incompleta. Volta e apanha o fragmento em falta.");
        Upsert(entries, "raw.gate.recovery.exterior",
            "Маршрут не записан. Тело не выдерживает третий квадрат. Врата возвращают тебя в начало.",
            "The route is not recorded. Your body cannot hold the third square. The Gate returns you to the beginning.",
            "A rota nao esta registada. O teu corpo nao aguenta o terceiro quadrado. Os Portoes devolvem-te ao inicio.");
        Upsert(entries, "raw.gate.recovery.night",
            "Второй след не завершен. Врата возвращают тебя во второй квадрат.",
            "The second trace is unfinished. The Gate returns you to the second square.",
            "O segundo rasto esta incompleto. Os Portoes devolvem-te ao segundo quadrado.");

        Upsert(entries, "raw.nonstep",
            "Действие зарегистрировано. Ввод корректен. Результат отклонен. Ты сделал все правильно, но проход все равно не открылся.",
            "Action registered. Input correct. Result denied. You did everything right, but the passage still did not open.",
            "Acao registada. Entrada correta. Resultado recusado. Fizeste tudo certo, mas a passagem nao abriu.");
        Upsert(entries, "raw.price.prompt",
            "Проход требует потери. Выбери, что перестанет быть твоим.",
            "The passage demands a loss. Choose what will stop being yours.",
            "A passagem exige uma perda. Escolhe o que deixara de ser teu.");
        Upsert(entries, "raw.price.memory", "Отдать память", "Give memory", "Dar memoria");
        Upsert(entries, "raw.price.name", "Отдать имя", "Give name", "Dar nome");
        Upsert(entries, "raw.price.joy", "Отдать радость", "Give joy", "Dar alegria");
        Upsert(entries, "raw.price.refuse", "Отказаться", "Refuse", "Recusar");
        Upsert(entries, "raw.price.memory.accepted",
            "Память принята. След о цене останется неполным.",
            "Memory accepted. The trace of the price remains incomplete.",
            "Memoria aceite. O rasto do preco fica incompleto.");
        Upsert(entries, "raw.price.name.accepted",
            "Имя принято. Не каждый зов теперь будет находить тебя.",
            "Name accepted. Not every call will find you now.",
            "Nome aceite. Nem todo chamamento te encontrara agora.");
        Upsert(entries, "raw.price.joy.accepted",
            "Радость принята. Улыбка останется только движением лица.",
            "Joy accepted. A smile remains only a movement of the face.",
            "Alegria aceite. O sorriso fica apenas como movimento do rosto.");
        Upsert(entries, "raw.price.refused",
            "Отказ записан. Система считает это лишним движением.",
            "Refusal recorded. The System marks it as unnecessary movement.",
            "Recusa registada. O Sistema marca isso como movimento desnecessario.");
        Upsert(entries, "raw.shadow_npc.unknown",
            "Я не знаю, зачем ты здесь.",
            "I do not know why you are here.",
            "Nao sei porque estas aqui.");
        Upsert(entries, "raw.shadow_npc.help_prompt",
            "Если ты поможешь мне, они увидят тебя.",
            "If you help me, they will see you.",
            "Se me ajudares, eles vao ver-te.");
        Upsert(entries, "raw.shadow_npc.choice.help", "Помочь", "Help", "Ajudar");
        Upsert(entries, "raw.shadow_npc.choice.ignore", "Игнорировать", "Ignore", "Ignorar");
        Upsert(entries, "raw.shadow_npc.choice.push", "Оттолкнуть", "Push away", "Empurrar");
        Upsert(entries, "raw.shadow_npc.help",
            "Ты сделал это не для меня. Но я запомню один цикл.",
            "You did not do it for me. But I will remember one cycle.",
            "Nao fizeste isto por mim. Mas vou lembrar um ciclo.");
        Upsert(entries, "raw.shadow_npc.ignore",
            "Так проще. Так система любит.",
            "That is easier. That is what the System likes.",
            "Assim e mais facil. E disso que o Sistema gosta.");
        Upsert(entries, "raw.shadow_npc.push",
            "Ночь быстро учит тебя быть сильным.",
            "The night quickly teaches you to be strong.",
            "A noite ensina depressa a seres forte.");
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
                StylePauseSlider(slider, skin);
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

        RawImage fog = EnsureRawOverlay(dimmer, "PauseAtmosphereFog", skin.PauseFogTexture, new Color(0.12f, 0.28f, 0.19f, 0.032f));
        RawImage dust = EnsureRawOverlay(dimmer, "PauseAtmosphereDust", skin.PauseDustTexture, new Color(0.75f, 0.88f, 0.62f, 0.028f));

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
        SetFloat(serialized, "ambienceVolumeScale", 0.16f);
        SetFloat(serialized, "buzzVolumeScale", 0.04f);
        SetFloat(serialized, "flickerStrength", 0.01f);
        SetVector2(serialized, "fogTiling", new Vector2(8.5f, 5.2f));
        SetVector2(serialized, "dustTiling", new Vector2(18f, 10f));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildMenuAtmosphere(GameObject background, FrontendSkin skin)
    {
        if (background == null) return;

        RectTransform rect = background.GetComponent<RectTransform>();
        if (rect == null) return;

        CanvasGroup group = background.GetComponent<CanvasGroup>();
        if (group == null) group = background.AddComponent<CanvasGroup>();

        RawImage fog = EnsureRawOverlay(rect, "MenuAtmosphereFog", skin.PauseFogTexture, new Color(0.12f, 0.31f, 0.21f, 0.026f));
        RawImage dust = EnsureRawOverlay(rect, "MenuAtmosphereDust", skin.PauseDustTexture, new Color(0.82f, 0.92f, 0.62f, 0.018f));

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
        SetFloat(serialized, "flickerStrength", 0.006f);
        SetVector2(serialized, "fogTiling", new Vector2(9f, 5.5f));
        SetVector2(serialized, "dustTiling", new Vector2(22f, 12f));
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

        overlay.transform.SetAsFirstSibling();
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
        grungeImage.color = new Color(0.22f, 0.34f, 0.22f, 0.08f);
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
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            ResetTextOnlyButton(button);
        }
    }

    private static void ResetTextOnlyButton(Button button)
    {
        if (button == null) return;

        RemoveChild(button.transform, "HudIconBadge");
        RemoveChild(button.transform, "HudIcon");

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            RectTransform labelRect = label.GetComponent<RectTransform>();
            SetStretchOffsets(labelRect, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            label.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent != null ? parent.Find(name) : null;
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void StylePauseImage(Image image, FrontendSkin skin)
    {
        if (image == null) return;

        string objectName = image.gameObject.name;
        bool isButtonBackground = image.GetComponent<Button>() != null;
        bool isMainPanel = objectName.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           objectName.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           objectName.IndexOf("Content", StringComparison.OrdinalIgnoreCase) >= 0;

        if (objectName == "GrungeOverlay" || objectName == "TopAccent" || objectName == "BottomAccent")
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
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = Color.clear;
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
            bool secondaryPanel = objectName.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  objectName.IndexOf("Content", StringComparison.OrdinalIgnoreCase) >= 0;
            image.sprite = secondaryPanel && skin.PanelBorderThin != null ? skin.PanelBorderThin : skin.PanelBorder;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
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

    private static void StylePauseSlider(Slider slider, FrontendSkin skin)
    {
        if (slider == null) return;

        Transform backgroundTransform = slider.transform.Find("Background");
        Image background = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
        if (background != null)
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(0.03f, 0.078f, 0.055f, 0.96f);
        }

        Image target = slider.targetGraphic as Image;
        if (target != null && slider.handleRect != null && target.transform == slider.handleRect)
        {
            target.sprite = null;
            target.type = Image.Type.Simple;
            target.color = new Color(0.86f, 0.55f, 0.19f, 1f);
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            fill.color = new Color(0.62f, 0.86f, 0.64f, 0.96f);
        }

        Image handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
        if (handle != null)
        {
            handle.sprite = null;
            handle.type = Image.Type.Simple;
            handle.color = new Color(0.86f, 0.55f, 0.19f, 1f);
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
                templateImage.sprite = skin.Popup != null ? skin.Popup : skin.PanelBorder;
                templateImage.type = templateImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                templateImage.color = PanelStrongColor;
            }

            foreach (Image templateChildImage in dropdown.template.GetComponentsInChildren<Image>(true))
            {
                if (templateChildImage == null || templateChildImage == templateImage) continue;
                if (templateChildImage.gameObject.name == "Item Checkmark")
                {
                    templateChildImage.sprite = null;
                    templateChildImage.type = Image.Type.Simple;
                    templateChildImage.color = Color.clear;
                }
                else
                {
                    templateChildImage.sprite = skin.Select != null ? skin.Select : skin.Button;
                    templateChildImage.type = templateChildImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    templateChildImage.color = new Color(0.026f, 0.075f, 0.052f, 0.94f);
                }
            }

            foreach (Toggle itemToggle in dropdown.template.GetComponentsInChildren<Toggle>(true))
            {
                Transform checkmark = itemToggle.transform.Find("Item Checkmark");
                TMP_Text checkmarkText = EnsurePlainCheckText(checkmark, skin.DisplayFont, 15f);
                if (checkmarkText != null) itemToggle.graphic = checkmarkText;
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
            arrowText = CreateText(dropdown.transform, "Arrow", "▼", skin.DisplayFont, 16f, TextAlignmentOptions.Center, AccentColor);
        }
        else
        {
            arrowText = arrow.GetComponent<TMP_Text>();
            if (arrowText == null) arrowText = arrow.gameObject.AddComponent<TextMeshProUGUI>();
        }

        SetRect(arrowText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(28f, 0f));
        arrowText.font = skin.DisplayFont;
        arrowText.text = "▼";
        arrowText.color = AccentColor;
        arrowText.raycastTarget = false;
    }

    private static void StylePauseToggle(Toggle toggle, FrontendSkin skin)
    {
        if (toggle == null) return;

        Image box = toggle.targetGraphic as Image;
        if (box != null)
        {
            box.sprite = null;
            box.type = Image.Type.Simple;
            box.color = new Color(0.035f, 0.1f, 0.065f, 1f);
        }

        Transform checkTransform = toggle.graphic != null
            ? toggle.graphic.transform
            : toggle.transform.Find("Background/Checkmark");
        TMP_Text check = EnsurePlainCheckText(checkTransform, skin.DisplayFont, 16f);
        if (check != null)
        {
            check.color = new Color(0.86f, 0.55f, 0.19f, 1f);
            toggle.graphic = check;
        }
    }

    private static TMP_Text EnsurePlainCheckText(Transform target, TMP_FontAsset font, float fontSize)
    {
        if (target == null) return null;

        Image image = target.GetComponent<Image>();
        if (image != null) UnityEngine.Object.DestroyImmediate(image);

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null) text = target.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = "✓";
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(9f, fontSize - 4f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.86f, 0.55f, 0.19f, 1f);
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        return text;
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
        GameObject toggleObject = CreateUiObject("MusicToggleButton", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -68f), new Vector2(150f, 34f));
        Image toggleImage = toggleObject.AddComponent<Image>();
        toggleImage.sprite = skin.Button;
        toggleImage.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
        toggleImage.color = new Color(0.018f, 0.04f, 0.034f, 0.86f);

        Button toggle = toggleObject.AddComponent<Button>();
        toggle.targetGraphic = toggleImage;
        ColorBlock colors = toggle.colors;
        colors.normalColor = toggleImage.color;
        colors.highlightedColor = new Color(0.075f, 0.17f, 0.14f, 0.94f);
        colors.pressedColor = new Color(0.86f, 0.55f, 0.19f, 1f);
        colors.selectedColor = new Color(0.06f, 0.14f, 0.12f, 0.94f);
        colors.disabledColor = new Color(0.02f, 0.03f, 0.028f, 0.58f);
        toggle.colors = colors;

        TMP_Text toggleLabel = CreateText(toggleObject.transform, "Label", "MUSIC ON", skin.DisplayFont, 14f, TextAlignmentOptions.Center, TextColor);
        SetRect(toggleLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        toggleLabel.fontSizeMin = 10f;
        toggleLabel.fontSizeMax = 14f;

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
        SetObject(serialized, "toggleIcon", null);
        SetObject(serialized, "toggleLabel", toggleLabel);
        SetObject(serialized, "musicOnSprite", null);
        SetObject(serialized, "musicOffSprite", null);
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

        TMP_Text titleGhost = CreateText(panel.transform, "TitleGhost", "VIRUS 9", skin.DisplayFont, 78f, TextAlignmentOptions.Left, new Color(0.22f, 0.52f, 0.34f, 0.22f));
        SetRect(titleGhost.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 116f), new Vector2(560f, 104f));
        titleGhost.characterSpacing = 10f;
        titleGhost.fontStyle = FontStyles.UpperCase;

        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS 9", skin.DisplayFont, 78f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(98f, 112f), new Vector2(520f, 104f));
        title.characterSpacing = 10f;
        title.fontStyle = FontStyles.UpperCase;
        MenuTitleFlicker flicker = title.gameObject.AddComponent<MenuTitleFlicker>();
        SerializedObject flickerSerialized = new SerializedObject(flicker);
        SetObject(flickerSerialized, "primary", title);
        SetObject(flickerSerialized, "ghost", titleGhost);
        flickerSerialized.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text subtitle = CreateLocalizedText(panel.transform, "Subtitle", "menu.intro", "THE SYSTEM RECORDS EVERY STEP.", skin.BodyFont, 18f, TextAlignmentOptions.TopLeft, MutedTextColor);
        SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(102f, 22f), new Vector2(560f, 94f));
        subtitle.textWrappingMode = TextWrappingModes.Normal;

        Button startButton = CreateMenuButton(panel.transform, "StartButton", "menu.start", "START", skin, new Vector2(102f, -116f), new Vector2(260f, 48f));
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

        TMP_Text titleGhost = CreateText(panel.transform, "TitleGhost", "VIRUS 9", skin.DisplayFont, 58f, TextAlignmentOptions.Left, new Color(0.22f, 0.52f, 0.34f, 0.22f));
        SetRect(titleGhost.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(91f, -58f), new Vector2(460f, 76f));
        titleGhost.characterSpacing = 8f;
        titleGhost.fontStyle = FontStyles.UpperCase;

        TMP_Text title = CreateText(panel.transform, "Title", "VIRUS 9", skin.DisplayFont, 58f, TextAlignmentOptions.Left, TextColor);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -62f), new Vector2(430f, 74f));
        title.characterSpacing = 8f;
        title.fontStyle = FontStyles.UpperCase;
        MenuTitleFlicker flicker = title.gameObject.AddComponent<MenuTitleFlicker>();
        SerializedObject flickerSerialized = new SerializedObject(flicker);
        SetObject(flickerSerialized, "primary", title);
        SetObject(flickerSerialized, "ghost", titleGhost);
        flickerSerialized.ApplyModifiedPropertiesWithoutUndo();

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

        Button audioTab = CreateSettingsTab(panel.transform, "AudioTabButton", "settings.audio", "AUDIO", skin, 0);
        Button videoTab = CreateSettingsTab(panel.transform, "VideoTabButton", "settings.video", "VIDEO", skin, 1);
        Button controlsTab = CreateSettingsTab(panel.transform, "ControlsTabButton", "settings.controls", "CONTROLS", skin, 2);
        Button languageTab = CreateSettingsTab(panel.transform, "LanguageTabButton", "settings.language", "LANGUAGE", skin, 3);
        Button accessibilityTab = CreateSettingsTab(panel.transform, "AccessibilityTabButton", "settings.accessibility", "ACCESSIBILITY", skin, 4);

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

    private static Button CreateSettingsTab(Transform parent, string name, string key, string fallback, FrontendSkin skin, int index)
    {
        Button button = CreateMenuButton(parent, name, key, fallback, skin, new Vector2(24f, -112f - index * 56f), new Vector2(210f, 42f));
        return button;
    }

    private static GameObject CreateSettingsContentPanel(Transform parent, string name, FrontendSkin skin)
    {
        GameObject panel = CreateUiObject(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -112f), new Vector2(620f, 378f));
        Image image = panel.AddComponent<Image>();
        image.sprite = skin.PanelBorderThin != null ? skin.PanelBorderThin : skin.PanelBorder;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
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
        backgroundImage.sprite = null;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = new Color(0.03f, 0.078f, 0.055f, 0.96f);

        GameObject fillArea = CreateUiObject("Fill Area", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f));
        GameObject fill = CreateUiObject("Fill", fillArea.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 10f));
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = null;
        fillImage.type = Image.Type.Simple;
        fillImage.color = new Color(0.62f, 0.86f, 0.64f, 0.96f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f));
        GameObject handle = CreateUiObject("Handle", handleArea.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 42f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = null;
        handleImage.type = Image.Type.Simple;
        handleImage.color = new Color(0.86f, 0.55f, 0.19f, 1f);

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
        backgroundImage.sprite = null;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = new Color(0.035f, 0.1f, 0.065f, 1f);

        GameObject checkmark = CreateUiObject("Checkmark", background.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
        TMP_Text checkmarkText = checkmark.AddComponent<TextMeshProUGUI>();
        checkmarkText.font = skin.DisplayFont;
        checkmarkText.text = "✓";
        checkmarkText.fontSize = 16f;
        checkmarkText.fontSizeMin = 10f;
        checkmarkText.fontSizeMax = 16f;
        checkmarkText.enableAutoSizing = true;
        checkmarkText.alignment = TextAlignmentOptions.Center;
        checkmarkText.color = new Color(0.86f, 0.55f, 0.19f, 1f);
        checkmarkText.raycastTarget = false;

        TMP_Text label = CreateLocalizedText(root.transform, "Label", key, fallback, skin.BodyFont, 16f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(-34f, 30f));

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkText;
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
            rowImage.sprite = skin.PanelBorderThin != null ? skin.PanelBorderThin : skin.PanelBorder;
            rowImage.type = rowImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
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
            grungeImage.color = new Color(0.24f, 0.36f, 0.22f, 0.14f);
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

        GameObject line = CreateUiObject("LineAccent", buttonObject.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(3f, -14f));
        Image lineImage = line.AddComponent<Image>();
        lineImage.color = new Color(0.86f, 0.55f, 0.19f, 0.72f);
        lineImage.raycastTarget = false;

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

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, FrontendSkin skin, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject root = CreateUiObject(name, parent, anchorMin, anchorMax, pivot, position, size);
        Image image = root.AddComponent<Image>();
        image.sprite = skin.Button;
        image.type = skin.Button != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0.018f, 0.04f, 0.034f, 0.98f);

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = image;
        dropdown.alphaFadeSpeed = 0.06f;

        TMP_Text label = CreateText(root.transform, "Label", "РУССКИЙ", skin.BodyFont, 15f, TextAlignmentOptions.MidlineLeft, TextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(-42f, 0f));
        label.fontSizeMin = 10f;
        label.fontSizeMax = 15f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        dropdown.captionText = label;

        TMP_Text arrow = CreateText(root.transform, "Arrow", "▼", skin.DisplayFont, 16f, TextAlignmentOptions.Center, AccentColor);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-17f, 0f), new Vector2(28f, 0f));

        GameObject template = CreateUiObject("Template", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(0f, 112f));
        Image templateImage = template.AddComponent<Image>();
        templateImage.sprite = skin.Popup != null ? skin.Popup : skin.PanelBorder;
        templateImage.type = templateImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        templateImage.color = new Color(0.014f, 0.036f, 0.028f, 0.995f);
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
        toggleColors.highlightedColor = new Color(0.46f, 0.72f, 0.52f, 0.85f);
        toggleColors.pressedColor = new Color(0.86f, 0.55f, 0.19f, 0.95f);
        toggleColors.selectedColor = new Color(0.38f, 0.62f, 0.46f, 0.95f);
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
