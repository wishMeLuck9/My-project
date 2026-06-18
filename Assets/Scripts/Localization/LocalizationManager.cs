using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    Russian,
    English,
    Portuguese
}

public class LocalizationManager : MonoBehaviour
{
    private const string LanguagePref = "virus9.settings.language";
    private const string CatalogResource = "Localization/LocalizationCatalog";

    public static LocalizationManager Instance { get; private set; }

    public static event Action LanguageChanged;

    private readonly Dictionary<string, LocalizationCatalog.Entry> entries =
        new Dictionary<string, LocalizationCatalog.Entry>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalizationCatalog.Entry> rawEntries =
        new Dictionary<string, LocalizationCatalog.Entry>(StringComparer.Ordinal);

    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Russian;

    public static LocalizationManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        LocalizationManager existing = FindFirstObjectByType<LocalizationManager>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        return new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCatalog();
        CurrentLanguage = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguagePref, 0), 0, 2);
    }

    public void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language) return;

        CurrentLanguage = language;
        PlayerPrefs.SetInt(LanguagePref, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public string Get(string key, string fallback = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
        if (!entries.TryGetValue(key, out LocalizationCatalog.Entry entry))
        {
            return fallback ?? key;
        }

        string value = Resolve(entry);
        return string.IsNullOrWhiteSpace(value) ? fallback ?? key : value;
    }

    public string Format(string key, params object[] args)
    {
        try
        {
            return string.Format(Get(key), args);
        }
        catch (FormatException exception)
        {
            Debug.LogWarning($"Localization format failed for '{key}': {exception.Message}", this);
            return Get(key);
        }
    }

    public string TranslateRaw(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (!rawEntries.TryGetValue(value, out LocalizationCatalog.Entry entry)) return value;

        string translated = Resolve(entry);
        return string.IsNullOrWhiteSpace(translated) ? value : translated;
    }

    private void LoadCatalog()
    {
        entries.Clear();
        rawEntries.Clear();
        AddBuiltInEntries();

        LocalizationCatalog catalog = Resources.Load<LocalizationCatalog>(CatalogResource);
        if (catalog == null) return;

        foreach (LocalizationCatalog.Entry entry in catalog.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
            entries[entry.key] = entry;
            if (!entry.key.StartsWith("raw.", StringComparison.Ordinal)) continue;
            AddRawEntry(entry.russian, entry);
            AddRawEntry(entry.english, entry);
            AddRawEntry(entry.portuguese, entry);
        }
    }

    private string Resolve(LocalizationCatalog.Entry entry)
    {
        return CurrentLanguage switch
        {
            GameLanguage.English => entry.english,
            GameLanguage.Portuguese => entry.portuguese,
            _ => entry.russian
        };
    }

    private void AddRawEntry(string raw, LocalizationCatalog.Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(raw)) rawEntries[raw] = entry;
    }

    private void AddBuiltInEntries()
    {
        AddBuiltInEntry("speaker.gate", "ВРАТА", "GATE", "PORTOES");
        AddBuiltInEntry("speaker.system", "SYSTEM", "SYSTEM", "SYSTEM");
        AddBuiltInEntry("hud.health", "ЖИЗНЬ {0}/{1}", "HP {0}/{1}", "VIDA {0}/{1}");
        AddBuiltInEntry("hud.boss", "СТРАЖИ {0}%", "GUARDIANS {0}%", "GUARDIOES {0}%");
        AddBuiltInEntry("hud.boss.phase2", "СТРАЖИ {0}%  ФАЗА 2", "GUARDIANS {0}%  PHASE 2", "GUARDIOES {0}%  FASE 2");
        AddBuiltInEntry("hud.damage", "Страж попал. Жизнь {0}/{1}", "Guardian hit. HP {0}/{1}", "O guardiao acertou. Vida {0}/{1}");
        AddBuiltInEntry("boss.intro", "Стражи проснулись. Выживи в суде.", "The guardians wake. Survive the judgement.", "Os guardioes acordaram. Sobrevive ao julgamento.");
        AddBuiltInEntry("boss.phase2", "Врата отвечают силой.", "The gate answers with force.", "Os portoes respondem com forca.");
        AddBuiltInEntry("hud.shadow_guardian_promoted", "Тень стала стражем. Ей нужно больше одного удара.", "A shadow hardens into a guardian. It takes more than one strike.", "Uma sombra endurece num guardiao. Precisa de mais de um golpe.");
        AddBuiltInEntry("hud.shadow_guardian_hit", "Страж-тень выдержал удар. Осталось {0}/{1}.", "Guardian-shadow resisted. HP {0}/{1}.", "Guardiao-sombra resistiu. Vida {0}/{1}.");
        AddBuiltInEntry("raw.hunt.reaction.seen", "Они видят тебя.", "They see you.", "Eles veem-te.");
        AddBuiltInEntry("raw.hunt.reaction.close", "Они идут за твоим светом.", "They follow your light.", "Eles seguem a tua luz.");
        AddBuiltInEntry("raw.hunt.reaction.above", "Они ждут, когда ты спустишься.", "They wait for you to come down.", "Eles esperam que desças.");
        AddBuiltInEntry("raw.hunt.reaction.shove", "Он пытается сбить тебя.", "He tries to knock you down.", "Ele tenta derrubar-te.");
        AddBuiltInEntry("raw.hunt.interact.run", "Не говори. Беги.", "Do not speak. Run.", "Nao fales. Corre.");
        AddBuiltInEntry("raw.hunt.interact.close", "Мы уже рядом.", "We are already close.", "Ja estamos perto.");
        AddBuiltInEntry("raw.shadow.hunt.reaction.seen", "Тьма повернулась к тебе.", "The dark turns toward you.", "A escuridao vira-se para ti.");
        AddBuiltInEntry("raw.shadow.hunt.reaction.close", "Она помнит твой удар.", "It remembers your strike.", "Ela lembra-se do teu golpe.");
        AddBuiltInEntry("raw.shadow.hunt.reaction.above", "Снизу тоже видно страх.", "Fear is visible from below too.", "O medo tambem se ve de baixo.");
        AddBuiltInEntry("raw.shadow.hunt.interact.close", "Поздно просить тишины.", "Too late to ask for silence.", "Tarde demais para pedir silencio.");
        AddBuiltInEntry("raw.shadow.hunt.interact.angry", "Ты сделал нас такими.", "You made us like this.", "Foste tu que nos fizeste assim.");
        AddBuiltInEntry("raw.guardian.battle.interact.judgement", "Суд уже начался.", "The judgement has already begun.", "O julgamento ja começou.");
        AddBuiltInEntry("raw.guardian.battle.interact.silence", "Ответь движением, не словами.", "Answer with movement, not words.", "Responde com movimento, nao com palavras.");
        AddBuiltInEntry("dialogue.continue", "Продолжить", "Continue", "Continuar");
        AddBuiltInEntry("dialogue.next", "Далее ({0}/{1})", "Next ({0}/{1})", "Seguinte ({0}/{1})");
        AddBuiltInEntry("save.saved_at", "{0}  {1}", "{0}  {1}", "{0}  {1}");
        AddBuiltInEntry("save.scene.exterior", "Квадрат: день", "Square: day", "Quadrado: dia");
        AddBuiltInEntry("save.scene.night", "Защищённые аллеи: ночь", "Protected alleys: night", "Becos protegidos: noite");
        AddBuiltInEntry("save.scene.final", "Финальные врата", "Final gate", "Portoes finais");
        AddBuiltInEntry("ending.restart", "Начать заново", "Start again", "Recomecar");
        AddBuiltInEntry("ui.missing", "Не назначено", "Not assigned", "Nao atribuido");
    }

    private void AddBuiltInEntry(string key, string russian, string english, string portuguese)
    {
        LocalizationCatalog.Entry entry = new LocalizationCatalog.Entry
        {
            key = key,
            russian = russian,
            english = english,
            portuguese = portuguese
        };
        entries[key] = entry;
    }
}
