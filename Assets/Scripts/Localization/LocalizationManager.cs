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
        AddBuiltInEntry("hud.damage", "Страж попал. Устойчивость {0}/{1}", "Guardian hit. Stability {0}/{1}", "O guardiao acertou. Estabilidade {0}/{1}");
        AddBuiltInEntry("hud.exterior.stability_lost", "Фрагмент выпал из тебя. Первый квадрат возвращает тебя к старту.", "The fragment falls out of you. The first square returns you to the start.", "O fragmento cai de ti. O primeiro quadrado devolve-te ao inicio.");
        AddBuiltInEntry("hud.exterior.retry_keep_fragment", "Фрагмент остается с тобой. Первый квадрат отбрасывает тебя назад.", "The fragment stays with you. The first square throws you back.", "O fragmento fica contigo. O primeiro quadrado atira-te para tras.");
        AddBuiltInEntry("boss.intro", "Стражи проснулись. Выживи в суде.", "The guardians wake. Survive the judgement.", "Os guardioes acordaram. Sobrevive ao julgamento.");
        AddBuiltInEntry("boss.phase2", "Врата отвечают силой.", "The gate answers with force.", "Os portoes respondem com forca.");
        AddBuiltInEntry("hud.shadow_guardian_promoted", "Тень стала стражем. Ей нужно больше одного удара.", "A shadow hardens into a guardian. It takes more than one strike.", "Uma sombra endurece num guardiao. Precisa de mais de um golpe.");
        AddBuiltInEntry("hud.shadow_guardian_hit", "Страж-тень выдержал удар. Осталось {0}/{1}.", "Guardian-shadow resisted. HP {0}/{1}.", "Guardiao-sombra resistiu. Vida {0}/{1}.");
        AddBuiltInEntry("hud.boundary.exterior.blocked", "Сила высших не дает тебе двинуться дальше, хотя ты прекрасно видишь эту возможность.", "A higher force will not let you move farther, even though the way is clearly there.", "Uma forca superior nao te deixa avancar, mesmo vendo claramente essa possibilidade.");
        AddBuiltInEntry("hud.boundary.exterior.escape", "Невероятно, куда могут привести пути. Но сейчас тебя вернет к началу.", "It is incredible where paths can lead. But not now. You are returned to the start.", "E incrivel onde os caminhos podem levar. Mas agora voltas ao inicio.");
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
        AddBuiltInEntry("raw.return_gate.prompt",
            "Обратные врата открыты. Вернуться к предыдущему квадрату?",
            "The return gate is open. Go back to the previous square?",
            "Os portoes de regresso estao abertos. Voltar ao quadrado anterior?");
        AddBuiltInEntry("raw.return_gate.shoot_prompt",
            "Обратные врата открыты. Выстрели в раму, чтобы вернуться назад.",
            "The return gate is open. Shoot the frame to return.",
            "O portal de regresso esta aberto. Dispara contra a moldura para voltar.");
        AddBuiltInEntry("raw.return_gate.locked",
            "Обратный маршрут еще не записан. Сначала нужен фрагмент.",
            "The return route is not recorded yet. A fragment must anchor it first.",
            "A rota de regresso ainda nao foi registada. Primeiro precisas de um fragmento.");
        AddBuiltInEntry("raw.return_gate.shot",
            "Рама приняла удар. Маршрут сворачивается назад.",
            "The frame takes the strike. The route folds backward.",
            "A moldura aceitou o impacto. A rota dobra para tras.");
        AddBuiltInEntry("raw.return_gate.enter",
            "Вернуться",
            "Return",
            "Voltar");
        AddBuiltInEntry("raw.return_gate.leave",
            "Остаться",
            "Stay",
            "Ficar");
        AddBuiltInEntry("raw.night.training.prompt",
            "Сила уже в руке. Попробуй ударить по неподвижной тени, прежде чем ночь заметит тебя.",
            "The force is already in your hand. Strike the still shadow before the night notices you.",
            "A forca ja esta na tua mao. Atinge a sombra parada antes que a noite repare em ti.");
        AddBuiltInEntry("raw.night.training.hit",
            "Пространство треснуло. Теперь живые тени тоже почувствуют удар.",
            "The space cracked. Now the living shadows will feel the strike too.",
            "O espaco rachou. Agora as sombras vivas tambem vao sentir o golpe.");
        AddBuiltInEntry("raw.night.training.release",
            "Ночь услышала. Теперь двигайся.",
            "The night heard it. Move now.",
            "A noite ouviu. Agora mexe-te.");
        AddBuiltInEntry("raw.gate.recovery.exterior",
            "Маршрут не записан. Тело не выдерживает третий квадрат. Врата возвращают тебя к началу.",
            "The route is not recorded. Your body cannot hold the third square, so the gate returns you to the beginning.",
            "A rota nao esta registada. O teu corpo nao aguenta o terceiro quadrado, por isso os portoes devolvem-te ao inicio.");
        AddBuiltInEntry("raw.gate.recovery.night",
            "След не завершён. Врата складывают путь назад, во второй квадрат.",
            "The trace is unfinished. The gate folds the route back to the second square.",
            "O rasto esta incompleto. Os portoes dobram o caminho de volta ao segundo quadrado.");
        AddReadableBuiltInOverrides();
    }

    private void AddReadableBuiltInOverrides()
    {
        AddBuiltInEntry("speaker.gate", "ВРАТА", "GATE", "PORTOES");
        AddBuiltInEntry("speaker.system", "SYSTEM", "SYSTEM", "SYSTEM");
        AddBuiltInEntry("speaker.price_altar", "АЛТАРЬ ЦЕНЫ", "PRICE ALTAR", "ALTAR DO PRECO");
        AddBuiltInEntry("hud.health", "УСТОЙЧИВОСТЬ {0}/{1}", "STABILITY {0}/{1}", "ESTABILIDADE {0}/{1}");
        AddBuiltInEntry("hud.damage", "Страж попал. Устойчивость {0}/{1}", "Guardian hit. Stability {0}/{1}", "O guardiao acertou. Estabilidade {0}/{1}");
        AddBuiltInEntry("hud.exterior.retry_keep_fragment", "Фрагмент остается с тобой. Первый квадрат отбрасывает тебя назад.", "The fragment stays with you. The first square throws you back.", "O fragmento fica contigo. O primeiro quadrado atira-te para tras.");
        AddBuiltInEntry("dialogue.continue", "Продолжить", "Continue", "Continuar");
        AddBuiltInEntry("dialogue.next", "Далее ({0}/{1})", "Next ({0}/{1})", "Seguinte ({0}/{1})");
        AddBuiltInEntry("raw.nonstep", "Действие зарегистрировано. Ввод корректен. Результат отклонён. Ты сделал всё правильно, но проход всё равно не открылся.", "Action registered. Input correct. Result denied. You did everything right, but the passage still did not open.", "Acao registada. Entrada correta. Resultado recusado. Fizeste tudo certo, mas a passagem nao abriu.");
        AddBuiltInEntry("raw.night.training.release", "Ночь признала твою силу. Теперь двигайся: живые тени услышали удар.", "The night recognized your force. Move now: the living shadows heard the strike.", "A noite reconheceu a tua forca. Mexe-te: as sombras vivas ouviram o golpe.");
        AddBuiltInEntry("raw.night.fragment.ready", "Фрагмент появился на дороге. Подбери его, прежде чем идти к Вратам.", "The fragment appeared on the road. Pick it up before going to the Gate.", "O fragmento apareceu na estrada. Apanha-o antes de ires aos Portoes.");
        AddBuiltInEntry("raw.gate.violent", "Ночь записала путь силы. Защитники не примут цену словами. Выживи и открой Врата силой.", "The night recorded a path of force. The defenders will not accept words as a price. Survive and open the Gate by force.", "A noite registou um caminho de forca. Os defensores nao aceitarao palavras como preco. Sobrevive e abre os Portoes pela forca.");
        AddBuiltInEntry("raw.guardian.violent", "Ночь записала силу. Теперь проход придется отстоять.", "The night recorded force. Now passage must be claimed by force.", "A noite registou forca. Agora a passagem tera de ser reclamada pela forca.");
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
