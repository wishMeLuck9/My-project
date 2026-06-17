using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class FrontendUiMigration
{
    private const string FrontendScene = "Assets/Scenes/Frontend/MENU_BOOT.unity";
    private const string FrontendPrefab = "Assets/Resources/UI/FrontendMenu.prefab";
    private const string PausePrefab = "Assets/Resources/UI/PauseMenu.prefab";
    private const string CatalogAsset = "Assets/Resources/Localization/LocalizationCatalog.asset";
    private const string DefaultVolumeProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";
    private const string ExteriorScene = "Assets/Scenes/Playable/LOCATION_01_EXTERIOR_DAY.unity";
    private const string NightScene = "Assets/Scenes/Playable/LOCATION_02_PROTECTED_ALLEYS_NIGHT.unity";
    private const string FinalScene = "Assets/Scenes/Playable/LOCATION_03_GATE_FINAL.unity";

    private static readonly Color Background = new Color(0.008f, 0.014f, 0.025f, 1f);
    private static readonly Color Panel = new Color(0.015f, 0.035f, 0.06f, 0.97f);
    private static readonly Color PanelSoft = new Color(0.025f, 0.065f, 0.09f, 0.95f);
    private static readonly Color Cyan = new Color(0.18f, 0.82f, 1f, 1f);
    private static readonly Color CyanSoft = new Color(0.12f, 0.35f, 0.45f, 1f);
    private static readonly Color Amber = new Color(1f, 0.66f, 0.2f, 1f);
    private static readonly Color Text = new Color(0.9f, 0.96f, 1f, 1f);

    [MenuItem("Tools/Virus 9/Apply Frontend UI Migration")]
    public static void Apply()
    {
        EnsureFolders();
        EnsureCatalog();
        CleanDefaultVolumeProfile(false);
        CreateFrontendPrefab();
        CreatePausePrefab();
        CreateFrontendScene();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.Refresh();
        Debug.Log("Frontend UI migration applied: MENU_BOOT, terminal prefabs, localization catalog, settings, saves, and pause menu.");
    }

    [MenuItem("Tools/Virus 9/Validate Frontend UI")]
    public static void Validate()
    {
        List<string> issues = new List<string>();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FrontendScene) == null) issues.Add("MENU_BOOT scene is missing.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FrontendPrefab) == null) issues.Add("FrontendMenu prefab is missing.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefab) == null) issues.Add("PauseMenu prefab is missing.");
        LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogAsset);
        if (catalog == null)
        {
            issues.Add("Localization catalog is missing.");
        }
        else
        {
            ValidateLocalizationCatalog(catalog, issues);
            ValidateRawLocalizationMappings(catalog, issues);
            ValidateSerializedDialogueLocalization(catalog, issues);
            ValidateScriptDialogueLocalization(catalog, issues);
        }

        ValidateDefaultVolumeProfile(issues);

        string[] expected = { FrontendScene, ExteriorScene, NightScene, FinalScene };
        string[] route = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (!route.SequenceEqual(expected)) issues.Add("Build Settings route is not MENU_BOOT -> exterior -> night -> final.");

        if (issues.Count > 0) throw new InvalidOperationException("Frontend UI validation failed:\n- " + string.Join("\n- ", issues));
        Debug.Log("Frontend UI validation passed.");
    }

    [MenuItem("Tools/Virus 9/Clean Default Volume Profile")]
    public static void CleanDefaultVolumeProfileMenu()
    {
        int removed = CleanDefaultVolumeProfile(true);
        Debug.Log($"Default volume profile cleanup finished. Removed {removed} missing component reference(s).");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "UI");
        EnsureFolder("Assets/Resources", "Localization");
        EnsureFolder("Assets/Scenes", "Frontend");
        EnsureFolder("Assets/Prefabs", "UI");
        EnsureFolder("Assets/Prefabs/UI", "Frontend");
        EnsureFolder("Assets/Prefabs/UI", "Gameplay");
        EnsureFolder("Assets/Prefabs/UI", "Shared");
    }

    private static void EnsureCatalog()
    {
        LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogAsset);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LocalizationCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAsset);
        }

        catalog.ReplaceEntries(BuildCatalogEntries());
        EditorUtility.SetDirty(catalog);
    }

    private static List<LocalizationCatalog.Entry> BuildCatalogEntries()
    {
        return new List<LocalizationCatalog.Entry>
        {
            Entry("app.title", "VIRUS9", "VIRUS9", "VIRUS9"),
            Entry("menu.start", "НАЧАТЬ", "START", "INICIAR"),
            Entry("menu.intro", "МИР ДУШ НЕ ДАЕТ ИНСТРУКЦИЙ.\nНО СИСТЕМА ЗАПИСЫВАЕТ КАЖДЫЙ ТВОЙ ШАГ.", "THE WORLD OF SOULS GIVES NO INSTRUCTIONS.\nBUT THE SYSTEM RECORDS EVERY STEP YOU TAKE.", "O MUNDO DAS ALMAS NAO DA INSTRUCOES.\nMAS O SISTEMA REGISTA CADA PASSO TEU."),
            Entry("menu.continue_intro", "ПРОДОЛЖИТЬ", "CONTINUE", "CONTINUAR"),
            Entry("intro.gameplay.1", "ANTBORN // ЗОНА ДО РОЖДЕНИЯ.\nЗдесь тени ждут, пока мир признает их существование.", "ANTBORN // PRE-BIRTH ZONE.\nShadows wait here until the world recognizes their existence.", "ANTBORN // ZONA ANTES DO NASCIMENTO.\nAs sombras esperam aqui ate o mundo reconhecer a sua existencia."),
            Entry("intro.gameplay.2", "Ты — ФРАГМЕНТ.\nТы появился из ошибки: случайная доброта стала душой, которую система не умеет зарегистрировать.", "You are the FRAGMENT.\nYou came from an error: accidental kindness became a soul the system cannot register.", "Tu es o FRAGMENTO.\nVieste de um erro: uma bondade acidental tornou-se uma alma que o sistema nao consegue registar."),
            Entry("intro.gameplay.3", "Система не ненавидит тебя.\nОна проверяет, сверяет, отказывает и исправляет. Для нее ты не враг. Ты запись без права на существование.", "The System does not hate you.\nIt checks, validates, denies, and corrects. To it, you are not an enemy. You are a record with no right to exist.", "O Sistema nao te odeia.\nEle verifica, valida, recusa e corrige. Para ele, nao es inimigo. Es um registo sem direito a existir."),
            Entry("intro.gameplay.4", "Тени проходят через Врата, потому что они признаны.\nТы должен идти тем же путем, но правильное действие не откроет проход. Врата потребуют цену.", "Shadows pass through the Gate because they are recognized.\nYou must walk the same route, but the correct action will not open the passage. The Gate will demand a price.", "As sombras passam pelo Portao porque sao reconhecidas.\nTens de seguir o mesmo percurso, mas a acao correta nao abrira a passagem. O Portao exigira um preco."),
            Entry("intro.gameplay.5", "ПРОТОКОЛ: ПРОБУДИТЬ ФРАГМЕНТ.\nИди. Собери свет. Дойди до Врат. Узнай, что у тебя можно отнять.", "PROTOCOL: WAKE THE FRAGMENT.\nMove. Gather the light. Reach the Gate. Learn what can be taken from you.", "PROTOCOLO: DESPERTAR O FRAGMENTO.\nAvanca. Recolhe a luz. Chega ao Portao. Descobre o que te podem tirar."),
            Entry("menu.new_game", "НОВАЯ ИГРА", "NEW GAME", "NOVO JOGO"),
            Entry("menu.continue", "ПРОДОЛЖИТЬ", "CONTINUE", "CONTINUAR"),
            Entry("menu.load_save", "ЗАГРУЗКА / СОХРАНЕНИЕ", "LOAD / SAVE", "CARREGAR / GUARDAR"),
            Entry("menu.settings", "НАСТРОЙКИ", "SETTINGS", "DEFINICOES"),
            Entry("menu.credits", "ТИТРЫ", "CREDITS", "CREDITOS"),
            Entry("menu.exit", "ВЫЙТИ", "EXIT", "SAIR"),
            Entry("menu.back", "НАЗАД", "BACK", "VOLTAR"),
            Entry("menu.resume", "ПРОДОЛЖИТЬ", "RESUME", "CONTINUAR"),
            Entry("menu.main_menu", "В ГЛАВНОЕ МЕНЮ", "RETURN TO MAIN MENU", "VOLTAR AO MENU PRINCIPAL"),
            Entry("menu.pause", "ПАУЗА", "PAUSE", "PAUSA"),
            Entry("menu.credits_text", "VIRUS9", "VIRUS9", "VIRUS9"),
            Entry("confirm.yes", "ДА", "YES", "SIM"),
            Entry("confirm.no", "НЕТ", "NO", "NAO"),
            Entry("confirm.exit", "ВЫЙТИ ИЗ ИГРЫ?", "EXIT THE GAME?", "SAIR DO JOGO?"),
            Entry("confirm.main_menu", "ВЕРНУТЬСЯ В ГЛАВНОЕ МЕНЮ?\nПРОГРЕСС БУДЕТ АВТОМАТИЧЕСКИ СОХРАНЕН.", "RETURN TO MAIN MENU?\nPROGRESS WILL BE AUTOSAVED.", "VOLTAR AO MENU PRINCIPAL?\nO PROGRESSO SERA GUARDADO AUTOMATICAMENTE."),
            Entry("settings.title", "НАСТРОЙКИ", "SETTINGS", "DEFINICOES"),
            Entry("settings.audio", "АУДИО", "AUDIO", "AUDIO"),
            Entry("settings.video", "ВИДЕО", "VIDEO", "VIDEO"),
            Entry("settings.controls", "УПРАВЛЕНИЕ", "CONTROLS", "CONTROLOS"),
            Entry("settings.language", "ЯЗЫК", "LANGUAGE", "LINGUA"),
            Entry("settings.accessibility", "ДОСТУПНОСТЬ", "ACCESSIBILITY", "ACESSIBILIDADE"),
            Entry("settings.master", "ОБЩАЯ ГРОМКОСТЬ", "MASTER VOLUME", "VOLUME GERAL"),
            Entry("settings.music", "МУЗЫКА", "MUSIC", "MUSICA"),
            Entry("settings.sfx", "ЭФФЕКТЫ", "SFX", "EFEITOS"),
            Entry("settings.voice", "ДИАЛОГИ", "VOICE / DIALOGUE", "VOZ / DIALOGO"),
            Entry("settings.mute", "ВЫКЛЮЧИТЬ ВЕСЬ ЗВУК", "MUTE ALL", "SILENCIAR TUDO"),
            Entry("settings.resolution", "РАЗРЕШЕНИЕ", "RESOLUTION", "RESOLUCAO"),
            Entry("settings.fullscreen", "ПОЛНЫЙ ЭКРАН", "FULLSCREEN", "ECRA INTEIRO"),
            Entry("settings.brightness", "ЯРКОСТЬ", "BRIGHTNESS", "BRILHO"),
            Entry("settings.quality", "КАЧЕСТВО", "QUALITY", "QUALIDADE"),
            Entry("settings.vsync", "ВЕРТИКАЛЬНАЯ СИНХРОНИЗАЦИЯ", "VSYNC", "SINCRONIZACAO VERTICAL"),
            Entry("settings.mouse_sensitivity", "ЧУВСТВИТЕЛЬНОСТЬ МЫШИ", "MOUSE SENSITIVITY", "SENSIBILIDADE DO RATO"),
            Entry("settings.invert_y", "ИНВЕРТИРОВАТЬ ОСЬ Y", "INVERT Y AXIS", "INVERTER EIXO Y"),
            Entry("settings.static_controls", "WASD — БЕГ\nSHIFT — ХОДЬБА\nSPACE — ПРЫЖОК\nE — ВЗАИМОДЕЙСТВИЕ\nLMB — АТАКА НОЧЬЮ\nESC — ПАУЗА", "WASD — RUN\nSHIFT — WALK\nSPACE — JUMP\nE — INTERACT\nLMB — ATTACK AT NIGHT\nESC — PAUSE", "WASD — CORRER\nSHIFT — ANDAR\nSPACE — SALTAR\nE — INTERAGIR\nLMB — ATACAR A NOITE\nESC — PAUSA"),
            Entry("settings.subtitles", "СУБТИТРЫ", "SUBTITLES", "LEGENDAS"),
            Entry("settings.subtitle_size", "РАЗМЕР СУБТИТРОВ", "SUBTITLE SIZE", "TAMANHO DAS LEGENDAS"),
            Entry("settings.ui_scale", "МАСШТАБ ИНТЕРФЕЙСА", "UI SCALE", "ESCALA DA INTERFACE"),
            Entry("settings.high_contrast", "ВЫСОКИЙ КОНТРАСТ", "HIGH CONTRAST UI", "ALTO CONTRASTE"),
            Entry("settings.colorblind", "ДРУЖЕЛЮБНЫЙ РЕЖИМ ДЛЯ ДАЛЬТОНИЗМА", "COLORBLIND-FRIENDLY MODE", "MODO PARA DALTONISMO"),
            Entry("settings.reduce_shake", "УМЕНЬШИТЬ ТРЯСКУ ЭКРАНА", "REDUCE SCREEN SHAKE", "REDUZIR TREMER DO ECRA"),
            Entry("settings.reduce_motion", "УМЕНЬШИТЬ ДВИЖЕНИЕ", "REDUCE MOTION", "REDUZIR MOVIMENTO"),
            Entry("settings.tutorial_hints", "ПОКАЗЫВАТЬ ПОДСКАЗКИ", "TUTORIAL HINTS", "DICAS DO TUTORIAL"),
            Entry("settings.hold_mode", "УДЕРЖАНИЕ ВМЕСТО ПОВТОРНЫХ НАЖАТИЙ", "HOLD INSTEAD OF REPEATED PRESS", "MANTER EM VEZ DE PREMIR REPETIDAMENTE"),
            Entry("settings.toggle_run", "ПЕРЕКЛЮЧАТЕЛЬ БЕГА", "TOGGLE RUN", "ALTERNAR CORRIDA"),
            Entry("settings.simple_prompts", "ПРОСТЫЕ ПОДСКАЗКИ ВЗАИМОДЕЙСТВИЯ", "SIMPLE INTERACTION PROMPTS", "INDICACOES SIMPLES DE INTERACAO"),
            Entry("settings.small", "МАЛЕНЬКИЙ", "SMALL", "PEQUENO"),
            Entry("settings.medium", "СРЕДНИЙ", "MEDIUM", "MEDIO"),
            Entry("settings.large", "БОЛЬШОЙ", "LARGE", "GRANDE"),
            Entry("language.russian", "РУССКИЙ", "RUSSIAN", "RUSSO"),
            Entry("language.english", "АНГЛИЙСКИЙ", "ENGLISH", "INGLES"),
            Entry("language.portuguese", "ПОРТУГАЛЬСКИЙ", "PORTUGUESE", "PORTUGUES"),
            Entry("save.title", "ЗАГРУЗКА / СОХРАНЕНИЕ", "LOAD / SAVE", "CARREGAR / GUARDAR"),
            Entry("save.load", "ЗАГРУЗИТЬ", "LOAD", "CARREGAR"),
            Entry("save.save", "СОХРАНИТЬ", "SAVE", "GUARDAR"),
            Entry("save.autosave", "СЛОТ 1 — АВТОСОХРАНЕНИЕ", "SLOT 1 — AUTOSAVE", "SLOT 1 — AUTOSAVE"),
            Entry("save.manual_slot", "СЛОТ {0} — РУЧНОЕ СОХРАНЕНИЕ", "SLOT {0} — MANUAL SAVE", "SLOT {0} — GUARDAR MANUAL"),
            Entry("save.empty", "ПУСТО", "EMPTY", "VAZIO"),
            Entry("hud.interact", "[E] ВЗАИМОДЕЙСТВОВАТЬ", "[E] INTERACT", "[E] INTERAGIR"),
            Entry("hud.controls.day", "[WASD] БЕГ\n[SHIFT] ХОДЬБА\n[SPACE] ПРЫЖОК\n[E] ВЗАИМОДЕЙСТВИЕ\n[ESC] ПАУЗА", "[WASD] RUN\n[SHIFT] WALK\n[SPACE] JUMP\n[E] INTERACT\n[ESC] PAUSE", "[WASD] CORRER\n[SHIFT] ANDAR\n[SPACE] SALTAR\n[E] INTERAGIR\n[ESC] PAUSA"),
            Entry("hud.controls.night", "[WASD] БЕГ\n[SHIFT] ХОДЬБА\n[SPACE] ПРЫЖОК\n[E] ВЗАИМОДЕЙСТВИЕ\n[LMB] АТАКА\n[ESC] ПАУЗА", "[WASD] RUN\n[SHIFT] WALK\n[SPACE] JUMP\n[E] INTERACT\n[LMB] ATTACK\n[ESC] PAUSE", "[WASD] CORRER\n[SHIFT] ANDAR\n[SPACE] SALTAR\n[E] INTERAGIR\n[LMB] ATACAR\n[ESC] PAUSA"),
            Entry("hud.objective.exterior.fragment", "SYSTEM // ЦЕЛЬ: НАЙДИ ФРАГМЕНТ СВЕТА", "SYSTEM // OBJECTIVE: FIND THE LIGHT FRAGMENT", "SYSTEM // OBJETIVO: ENCONTRA O FRAGMENTO DE LUZ"),
            Entry("hud.objective.exterior.gate", "SYSTEM // ЦЕЛЬ: ДОБЕРИСЬ ДО ДВЕРИ КВАДРАТА", "SYSTEM // OBJECTIVE: REACH THE SQUARE GATE", "SYSTEM // OBJETIVO: CHEGA A PORTA DO QUADRADO"),
            Entry("hud.objective.night.choice", "SYSTEM // ЦЕЛЬ: НАБЛЮДАЙ ИЛИ ДЕЙСТВУЙ", "SYSTEM // OBJECTIVE: OBSERVE OR ACT", "SYSTEM // OBJETIVO: OBSERVA OU AGE"),
            Entry("hud.objective.night.exit", "SYSTEM // ЦЕЛЬ: ДОБЕРИСЬ ДО ВЫХОДА", "SYSTEM // OBJECTIVE: REACH THE EXIT", "SYSTEM // OBJETIVO: CHEGA A SAIDA"),
            Entry("hud.objective.final", "SYSTEM // ЦЕЛЬ: ДОЙДИ ДО ВРАТ", "SYSTEM // OBJECTIVE: REACH THE GATE", "SYSTEM // OBJETIVO: CHEGA AOS PORTOES"),
            Entry("hud.stability", "УСТОЙЧИВОСТЬ  {0}", "STABILITY  {0}", "ESTABILIDADE  {0}"),
            Entry("hud.night_unlocked", "НОЧЬ ПРИЗНАЛА В ТЕБЕ СИЛУ. УДАР ДОСТУПЕН.", "THE NIGHT RECOGNIZED YOUR STRENGTH. ATTACK IS AVAILABLE.", "A NOITE RECONHECEU A TUA FORCA. O ATAQUE ESTA DISPONIVEL."),
            Entry("hud.map_active", "SYSTEM // КАРТА КВАДРАТА АКТИВНА. ВЫХОД ОТМЕЧЕН ГОЛУБЫМ.", "SYSTEM // SQUARE MAP ACTIVE. EXIT MARKED IN BLUE.", "SYSTEM // MAPA DO QUADRADO ATIVO. SAIDA MARCADA A AZUL."),
            Entry("hud.idle_hint", "ЕСЛИ ТЫ НЕ ВЗАИМОДЕЙСТВУЕШЬ, ТЫ ИСЧЕЗАЕШЬ.", "IF YOU DO NOT INTERACT, YOU DISAPPEAR.", "SE NAO INTERAGIRES, DESAPARECES."),
            Entry("hud.start.1", "ПРОТОКОЛ КВАДРАТА АКТИВЕН.", "SQUARE PROTOCOL ACTIVE.", "PROTOCOLO DO QUADRADO ATIVO."),
            Entry("hud.start.2", "ФРАГМЕНТ ОБНАРУЖЕН.", "FRAGMENT DETECTED.", "FRAGMENTO DETETADO."),
            Entry("hud.start.3", "ТЫ НАУЧИЛСЯ ИДТИ. ЭТОГО ДОСТАТОЧНО, ЧТОБЫ НАЧАТЬ.\nВЫЖИВЕШЬ ЛИ ТЫ — ЗАВИСИТ ОТ ТЕБЯ.", "YOU HAVE LEARNED TO WALK. THAT IS ENOUGH TO BEGIN.\nWHETHER YOU SURVIVE IS UP TO YOU.", "APRENDESTE A ANDAR. ISSO BASTA PARA COMECAR.\nSOBREVIVER DEPENDE DE TI."),
            Entry("hud.health", "ЖИЗНЬ {0}/{1}", "HP {0}/{1}", "VIDA {0}/{1}"),
            Entry("hud.boss", "СТРАЖИ {0}%", "GUARDIANS {0}%", "GUARDIOES {0}%"),
            Entry("hud.boss.phase2", "СТРАЖИ {0}%  ФАЗА 2", "GUARDIANS {0}%  PHASE 2", "GUARDIOES {0}%  FASE 2"),
            Entry("hud.damage", "Страж попал. Жизнь {0}/{1}", "Guardian hit. HP {0}/{1}", "O guardiao acertou. Vida {0}/{1}"),
            Entry("boss.intro", "Стражи проснулись. Выживи в суде.", "The guardians wake. Survive the judgement.", "Os guardioes acordaram. Sobrevive ao julgamento."),
            Entry("boss.phase2", "Врата отвечают силой.", "The gate answers with force.", "Os portoes respondem com forca."),
            Entry("speaker.gate", "ВРАТА", "GATE", "PORTOES"),
            Entry("speaker.system", "SYSTEM", "SYSTEM", "SYSTEM"),
            Entry("save.saved_at", "{0}  {1}", "{0}  {1}", "{0}  {1}"),
            Entry("save.scene.exterior", "Квадрат: день", "Square: day", "Quadrado: dia"),
            Entry("save.scene.night", "Защищённые аллеи: ночь", "Protected alleys: night", "Becos protegidos: noite"),
            Entry("save.scene.final", "Финальные врата", "Final gate", "Portoes finais"),
            Entry("ending.restart", "Начать заново", "Start again", "Recomecar"),
            Entry("dialogue.continue", "Продолжить", "Continue", "Continuar"),
            Entry("dialogue.next", "Далее ({0}/{1})", "Next ({0}/{1})", "Seguinte ({0}/{1})"),
            Entry("raw.hunt.restart", "Тебя вернули в начало. Следующий побег будет тяжелее.", "You were returned to the beginning. The next escape will be harder.", "Foste devolvido ao inicio. A proxima fuga sera mais dificil."),
            Entry("raw.hunt.purgatory", "Тени добрались до тебя. Теперь тебя ждет чистилище.\n\nТебя не хоронят. Тебя форматируют.", "The shadows reached you. Purgatory awaits.\n\nYou are not buried. You are formatted.", "As sombras alcancaram-te. O purgatorio espera-te.\n\nNao te enterram. Formatam-te."),
            Entry("raw.fragment.day.prompt", "Фрагмент лежит там, где на тебя еще смотрели без погони. Поднять его?", "The fragment lies where they still watched you without pursuit. Pick it up?", "O fragmento esta onde ainda te observavam sem perseguir. Apanha-lo?"),
            Entry("raw.fragment.night.prompt", "Фрагмент остался на месте чужого выбора. Взять его с собой?", "The fragment remained at the site of someone else's choice. Take it with you?", "O fragmento ficou no lugar da escolha de outra pessoa. Leva-lo contigo?"),
            Entry("raw.fragment.take", "Поднять", "Pick up", "Apanhar"),
            Entry("raw.fragment.leave", "Оставить", "Leave", "Deixar"),
            Entry("raw.fragment.day.collected", "Фрагмент принят. Квадрат узнал тебя. Беги к зданию на окраине.", "Fragment accepted. The square recognized you. Run to the building at the edge.", "Fragmento aceite. O quadrado reconheceu-te. Corre para o edificio na periferia."),
            Entry("raw.fragment.night.collected", "Второй фрагмент принят. Теперь врата смогут назвать цену.", "Second fragment accepted. The gate can now name its price.", "Segundo fragmento aceite. Os portoes podem agora indicar o preco."),
            Entry("raw.transition.locked.square", "Дверь квадрата закрыта. Пространство еще не признало твой маршрут.", "The square door is closed. The space has not recognized your route yet.", "A porta do quadrado esta fechada. O espaco ainda nao reconheceu o teu percurso."),
            Entry("raw.transition.locked.first", "Выход не признает тебя без первого фрагмента.", "The exit will not recognize you without the first fragment.", "A saida nao te reconhecera sem o primeiro fragmento."),
            Entry("raw.transition.locked.second", "До врат нельзя дойти без фрагмента, оставшегося в ночном квадрате.", "You cannot reach the gate without the fragment left in the night square.", "Nao podes chegar aos portoes sem o fragmento deixado no quadrado noturno."),
            Entry("raw.transition.prompt", "Порог найден. Продолжить путь?", "Threshold found. Continue?", "Limiar encontrado. Continuar?"),
            Entry("raw.transition.enter", "Войти", "Enter", "Entrar"),
            Entry("raw.transition.leave", "Отойти", "Step away", "Recuar"),
            Entry("raw.night.drop", "Площадь опустела. Фрагмент выпал из последней тени.", "The square is empty. A fragment fell from the last shadow.", "O quadrado ficou vazio. Um fragmento caiu da ultima sombra."),
            Entry("raw.night.observe", "Не подходи. Просто смотри.", "Do not come closer. Just watch.", "Nao te aproximes. Observa apenas."),
            Entry("raw.night.mercy", "Она встала. Забери то, что осталось на земле, и не делай из этого охоту.", "She stood up. Take what remains on the ground, and do not turn this into a hunt.", "Ela levantou-se. Leva o que ficou no chao e nao transformes isto numa caca."),
            Entry("raw.shadow.hit.first", "Ночь записала это как право. Утро назовет это долгом.", "The night recorded this as a right. Morning will call it a debt.", "A noite registou isto como um direito. A manha chamar-lhe-a uma divida."),
            Entry("raw.shadow.hit", "Мы знали, что ночь выберет тебя.", "We knew the night would choose you.", "Sabiamos que a noite te escolheria."),
            Entry("raw.npc.unknown", "Я не знаю, зачем ты здесь.", "I do not know why you are here.", "Nao sei porque estas aqui."),
            Entry("raw.npc.help.prompt", "Если ты поможешь мне, они увидят тебя.", "If you help me, they will see you.", "Se me ajudares, eles vao ver-te."),
            Entry("raw.npc.help", "Помочь", "Help", "Ajudar"),
            Entry("raw.npc.ignore", "Игнорировать", "Ignore", "Ignorar"),
            Entry("raw.npc.push", "Оттолкнуть", "Push away", "Afastar"),
            Entry("raw.npc.help.result", "Ты сделал это не для меня. Но я запомню один цикл.", "You did not do it for me. But I will remember one cycle.", "Nao fizeste isto por mim. Mas vou lembrar-me de um ciclo."),
            Entry("raw.npc.ignore.result", "Так проще. Так система любит.", "It is easier this way. The system likes it this way.", "Assim e mais simples. O sistema gosta assim."),
            Entry("raw.npc.push.result", "Ночь быстро учит тебя быть сильным.", "The night quickly teaches you to be strong.", "A noite ensina-te depressa a ser forte."),
            Entry("raw.gate.incomplete", "Врата не признают неполный след. Вернись с двумя фрагментами.", "The gate rejects an incomplete trace. Return with two fragments.", "Os portoes rejeitam um rasto incompleto. Volta com dois fragmentos."),
            Entry("raw.gate.restart", "Страж коснулся тебя. Суд начинается заново.", "The guardian touched you. The trial starts again.", "O guardiao tocou-te. O julgamento recomeca."),
            Entry("raw.gate.violent", "Ты принес убийство. Защитники не примут цену. Выживи и открой врата силой.", "You brought murder. The defenders will not accept a price. Survive and force the gate open.", "Trouxeste morte. Os defensores nao aceitarao um preco. Sobrevive e abre os portoes pela forca."),
            Entry("raw.gate.peaceful.prompt", "Ты дошел без убийства. Врата требуют оба фрагмента, память и жизнь. Отдать все ради прохода?", "You arrived without killing. The gate demands both fragments, memory, and life. Give everything to pass?", "Chegaste sem matar. Os portoes exigem ambos os fragmentos, memoria e vida. Dar tudo para passar?"),
            Entry("raw.gate.pay", "Отдать все и войти", "Give everything and enter", "Dar tudo e entrar"),
            Entry("raw.gate.restart.choice", "Начать заново", "Start again", "Recomecar"),
            Entry("raw.guardian.hit", "Удар принят. Врата все еще закрыты.", "Hit accepted. The gate remains closed.", "Golpe aceite. Os portoes continuam fechados."),
            Entry("raw.guardian.missing", "Состояние не найдено. Проверка отложена.", "State not found. Evaluation postponed.", "Estado nao encontrado. Avaliacao adiada."),
            Entry("raw.guardian.violent", "Ты принес убийство. Теперь проход придется отнять.", "You brought murder. Now you will have to take passage by force.", "Trouxeste morte. Agora teras de tomar a passagem pela forca."),
            Entry("raw.guardian.mercy", "Ты удержал руку. Сила отступит, если ты отдашь все добровольно.", "You held your hand. Force will recede if you give everything willingly.", "Contiveste a tua mao. A forca recuara se entregares tudo voluntariamente."),
            Entry("raw.guardian.price", "Два фрагмента, память и жизнь. Иначе врата не откроются.", "Two fragments, memory, and life. Otherwise the gate will not open.", "Dois fragmentos, memoria e vida. Caso contrario, os portoes nao se abrem."),
            Entry("raw.final.gate", "Перед тобой ворота. Или суд. Или ловушка.", "The gate is before you. Or a trial. Or a trap.", "Os portoes estao diante de ti. Ou um julgamento. Ou uma armadilha."),
            Entry("raw.final.night", "Ночная сила подтверждена. Милосердие не подтверждено.", "Night strength confirmed. Mercy not confirmed.", "Forca noturna confirmada. Misericordia nao confirmada."),
            Entry("raw.final.error", "Ошибка 9. Объект не должен быть здесь. Объект всё ещё здесь.", "Error 9. The object should not be here. The object is still here.", "Erro 9. O objeto nao devia estar aqui. O objeto ainda esta aqui."),
            Entry("raw.nonstep", "Действие зарегистрировано. Ввод корректен. Результат отклонен. Ты сделал всё правильно. Но не получилось.", "Action registered. Input correct. Result rejected. You did everything right. It still failed.", "Acao registada. Entrada correta. Resultado rejeitado. Fizeste tudo bem. Mesmo assim falhou."),
            Entry("raw.altar.prompt", "Проход требует потери. Выбери, что перестанет быть твоим.", "Passage requires loss. Choose what will stop being yours.", "A passagem exige perda. Escolhe o que deixara de ser teu."),
            Entry("raw.altar.memory", "Отдать память", "Give memory", "Dar memoria"),
            Entry("raw.altar.name", "Отдать имя", "Give name", "Dar nome"),
            Entry("raw.altar.joy", "Отдать радость", "Give joy", "Dar alegria"),
            Entry("raw.altar.refuse", "Отказаться", "Refuse", "Recusar"),
            Entry("raw.altar.memory.result", "Память принята. Воспоминание о цене удалено не полностью.", "Memory accepted. The memory of the price was not fully deleted.", "Memoria aceite. A memoria do preco nao foi totalmente eliminada."),
            Entry("raw.altar.name.result", "Имя принято. Обращение к тебе больше не гарантирует ответ.", "Name accepted. Calling you no longer guarantees an answer.", "Nome aceite. Chamar-te ja nao garante uma resposta."),
            Entry("raw.altar.joy.result", "Радость принята. Улыбка сохранена как внешний жест.", "Joy accepted. The smile was preserved as an external gesture.", "Alegria aceite. O sorriso foi preservado como gesto exterior."),
            Entry("raw.altar.refuse.result", "Отказ записан как лишнее движение.", "Refusal recorded as unnecessary movement.", "Recusa registada como movimento desnecessario."),
            Entry("raw.ending.force.title", "ФИНАЛ: ПУТЬ ЯРОСТИ", "ENDING: PATH OF RAGE", "FINAL: CAMINHO DA RAIVA"),
            Entry("raw.ending.force.text", "Ты родился.\n\nТы не помнишь всего, что произошло до Врат. Но внутри тебя всё ещё кипит то, что не смогло умереть.\n\nТвой фрагмент остался с тобой — не как свет, а как ярость.\n\nТеперь ты заперт в новом теле вместе с собственным бессилием. Каким бы злым ты ни стал, ты никогда не превзойдёшь ярость того, что осталось внутри.\n\nИногда тебе будут сниться тени. Но они больше не будут просить о помощи.\n\nОни будут помнить.", "You are born.\n\nYou do not remember everything that happened before the Gate. But inside you, something that could not die is still boiling.\n\nYour fragment stayed with you, not as light, but as rage.\n\nNow you are locked inside a new body with your own helplessness. No matter how angry you become, you will never surpass the rage of what remains inside.\n\nSometimes you will dream of shadows. But they will no longer ask for help.\n\nThey will remember.", "Nasceste.\n\nNao te lembras de tudo o que aconteceu antes dos Portoes. Mas dentro de ti ainda ferve algo que nao conseguiu morrer.\n\nO teu fragmento ficou contigo, nao como luz, mas como raiva.\n\nAgora estas preso num novo corpo com a tua propria impotencia. Por mais raiva que sintas, nunca vais ultrapassar a raiva daquilo que ficou dentro de ti.\n\nAs vezes vais sonhar com sombras. Mas elas ja nao vao pedir ajuda.\n\nElas vao lembrar-se."),
            Entry("raw.ending.paid.title", "ФИНАЛ: ПУТЬ ХРАНИТЕЛЯ", "ENDING: PATH OF THE KEEPER", "FINAL: CAMINHO DO GUARDIAO"),
            Entry("raw.ending.paid.text", "Ты родился.\n\nТы не помнишь всего, что произошло до Врат. Но иногда во сне ты видишь одинокую тень.\n\nОна смотрит на тебя из темноты — не с ненавистью, а с тихим пониманием. Будто всё ещё помогает тебе найти путь.\n\nТвой фрагмент остался внутри. В нём всё ещё живёт ярость, которую ты мог бы выпустить на мир.\n\nНо теперь ты не её пленник.\n\nТы её хранитель.\n\nИ однажды, когда придёт время, ты вспомнишь, зачем она была оставлена в тебе.", "You are born.\n\nYou do not remember everything that happened before the Gate. But sometimes, in sleep, you see a lone shadow.\n\nIt watches you from the dark, not with hatred, but with quiet understanding. As if it is still helping you find the way.\n\nYour fragment stayed inside. In it, the rage you could have released into the world is still alive.\n\nBut now you are not its prisoner.\n\nYou are its keeper.\n\nAnd one day, when the time comes, you will remember why it was left inside you.", "Nasceste.\n\nNao te lembras de tudo o que aconteceu antes dos Portoes. Mas as vezes, no sono, ves uma sombra solitaria.\n\nEla olha para ti da escuridao, nao com odio, mas com uma compreensao silenciosa. Como se ainda te ajudasse a encontrar o caminho.\n\nO teu fragmento ficou dentro de ti. Nele ainda vive a raiva que poderias ter libertado sobre o mundo.\n\nMas agora nao es prisioneiro dela.\n\nEs o seu guardiao.\n\nE um dia, quando chegar a hora, vais lembrar-te porque ela foi deixada dentro de ti."),
            Entry("raw.ending.restart.prompt", "Конец записан. Цикл может начаться заново.", "The ending is recorded. The cycle can begin again.", "O final foi registado. O ciclo pode recomecar."),
            Entry("raw.npc.queue.1", "Ты не из очереди.", "You are not in the queue.", "Tu nao estas na fila."),
            Entry("raw.npc.queue.2", "Не толкайся. Здесь ждут те, кому хотя бы обещали место.", "Do not push. Those who were at least promised a place wait here.", "Nao empurres. Aqui esperam aqueles a quem pelo menos prometeram um lugar."),
            Entry("raw.npc.queue.3", "У тебя даже имени нет.", "You do not even have a name.", "Tu nem sequer tens nome."),
            Entry("raw.npc.witness.1", "Смотри.", "Watch.", "Observa."),
            Entry("raw.npc.witness.2", "Он думает, что выбирает.", "He thinks he is choosing.", "Ele pensa que esta a escolher."),
            Entry("raw.npc.witness.3", "Он ещё не понял, что вход уже записан.", "He still has not realized that the entrance has already been recorded.", "Ele ainda nao percebeu que a entrada ja foi registada."),
            Entry("raw.npc.pleading", "Не гонись за мной ради удара. Смотри, что случится дальше.", "Do not chase me just to strike. Watch what happens next.", "Nao me persigas apenas para atacar. Observa o que acontece a seguir."),
            Entry("raw.npc.observer.1", "Квадрат помнит твои шаги, даже если ты нет.", "The square remembers your steps, even if you do not.", "O quadrado lembra-se dos teus passos, mesmo que tu nao."),
            Entry("raw.npc.observer.2", "Цена всегда выше ночью. Ты готов платить?", "The price is always higher at night. Are you ready to pay?", "O preco e sempre mais alto a noite. Estas pronto para pagar?"),
            Entry("raw.npc.whisperer.1", "Не смотри в окна. Они пусты не просто так.", "Do not look into the windows. They are empty for a reason.", "Nao olhes para as janelas. Elas estao vazias por uma razao."),
            Entry("raw.npc.whisperer.2", "Ты первый Фрагмент, добравшийся до этого цикла.", "You are the first Fragment to reach this cycle.", "Es o primeiro Fragmento a chegar a este ciclo."),
            Entry("raw.npc.afraid.neutral", "Не подходи резко. Я все еще пытаюсь встать.", "Do not approach suddenly. I am still trying to stand up.", "Nao te aproximes de repente. Ainda estou a tentar levantar-me."),
            Entry("raw.npc.afraid.mercy", "Ты не ударил. Значит, здесь еще можно подняться.", "You did not strike. Then it is still possible to stand up here.", "Nao atacaste. Entao ainda e possivel levantar-se aqui."),
            Entry("raw.npc.afraid.violent", "Я видел, что ты сделал. Не называй это выходом.", "I saw what you did. Do not call it an exit.", "Eu vi o que fizeste. Nao lhe chames uma saida."),
            Entry("raw.npc.ally.neutral", "К воротам ведет улица, но выбор идет рядом с тобой.", "A street leads to the gate, but the choice walks beside you.", "Uma rua leva aos portoes, mas a escolha caminha ao teu lado."),
            Entry("raw.npc.ally.mercy", "Фрагмент появился не из смерти. Сохрани это до ворот.", "The fragment did not come from death. Keep that with you until the gate.", "O fragmento nao surgiu da morte. Guarda isso contigo ate aos portoes."),
            Entry("raw.npc.ally.violent", "Врата узнают, сколько теней ты оставил лежать.", "The gate will know how many shadows you left lying down.", "Os portoes saberao quantas sombras deixaste caidas.")
            , Entry("raw.exterior.fragment.awakening", "Ты чувствуешь: ты можешь. Ты не создан, чтобы вечно страдать. Ты уникален, и ты признаешь это. Среди всего квадрата есть место, которое зовет тебя. Иди к нему.", "You feel it: you can act. You were not made to suffer forever. You are unique, and you admit it. Somewhere in this square, a place is calling you. Go to it.", "Sentes isto: podes agir. Nao foste feito para sofrer para sempre. Es unico, e admites isso. Algures neste quadrado, um lugar chama por ti. Vai ate ele.")
            , Entry("raw.exterior.gate.reveal", "Огонек в твоей груди начинает сиять. Воздух складывается в дверь, которую ты не видел до этого шага.", "The light in your chest begins to shine. The air folds into a door you could not see before this step.", "A luz no teu peito comeca a brilhar. O ar dobra-se numa porta que nao conseguias ver antes deste passo.")
            , Entry("raw.exterior.gate.claim", "Ты еще не был избран. Иди же и возьми.", "You have not been chosen yet. Go, then, and take.", "Ainda nao foste escolhido. Vai, entao, e toma.")
            , Entry("raw.exterior.gate.collapse", "Огонек вспыхивает. Удар ломает контур двери, и квадрат на мгновение сворачивается внутрь себя.", "The light flares. The strike breaks the door's outline, and for a moment the square collapses into itself.", "A luz irrompe. O golpe quebra o contorno da porta, e por um instante o quadrado colapsa para dentro de si.")
            , Entry("raw.exterior.gate.cutscene.found", "Фрагмент найден. Тебе не место здесь. Все тут закрыто.", "Fragment found. You do not belong here. Everything here is closed.", "Fragmento encontrado. Este nao e o teu lugar. Tudo aqui esta fechado.")
            , Entry("raw.exterior.gate.cutscene.vision", "Мне было видение: я чувствовал силу, способную разрывать, выводить и уничтожать миры. Я протянул руку свою и рассек пространство.", "I had a vision: I felt a power able to tear, extract, and destroy worlds. I reached out my hand and split space.", "Tive uma visao: senti uma forca capaz de rasgar, retirar e destruir mundos. Estendi a minha mao e cortei o espaco.")
            , Entry("raw.exterior.gate.cutscene.prompt", "Нажмите левую клавишу мыши, чтобы сломать пространство.", "Press the left mouse button to break space.", "Prime o botao esquerdo do rato para quebrar o espaco.")
            , Entry("raw.exterior.gate.cutscene.shadows", "Ты оборачиваешься. Тени стоят вокруг тебя и не нападают. Они смотрят в ужасе на то, что ты сделал.", "You turn around. The shadows stand around you and do not attack. They stare in horror at what you have done.", "Voltas-te. As sombras estao a tua volta e nao atacam. Olham horrorizadas para o que fizeste.")
            , Entry("raw.exterior.gate.cutscene.impossible", "Невозможно. Вы оказались за пределами. Добро сделало свое дело.", "Impossible. You are beyond the limits. Kindness has done its work.", "Impossivel. Ficaste para la dos limites. A bondade fez o seu trabalho.")
            , Entry("raw.night.intro.square", "Ты внутри квадрата, оказавшегося еще одним квадратом. И теперь ты понимаешь: таких квадратов бесконечное множество.", "You are inside a square that turned out to be another square. Now you understand: there are infinitely many of them.", "Estas dentro de um quadrado que afinal era outro quadrado. Agora entendes: ha infinitos quadrados destes.")
            , Entry("raw.night.intro.attack", "Чтобы пробить стены, нужен второй фрагмент. Атака доступна: каждый удар вызывает сдвиг материи.", "To break the walls, you need a second fragment. Attack is available: every strike shifts matter.", "Para romper as paredes, precisas de um segundo fragmento. O ataque esta disponivel: cada golpe desloca a materia.")
            , Entry("raw.night.intro.shadows", "Ночь признает твою силу. Теперь у тебя власть над живыми тенями. Ты чувствуешь их страх и их надежду к тому, что возрождается.", "The night recognizes your strength. Now you have power over living shadows. You feel their fear and their hope toward what is being reborn.", "A noite reconhece a tua forca. Agora tens poder sobre sombras vivas. Sentes o medo delas e a esperanca delas perante aquilo que renasce.")
            , Entry("raw.night.intro.hunted", "Они загнаны в собственное желание существовать. Какой путь ты предпочтешь? Они чуют тебя, ненавидят тебя, охотятся за тобой.", "They are trapped inside their own desire to exist. Which path will you choose? They sense you, hate you, and hunt you.", "Elas estao encurraladas no proprio desejo de existir. Que caminho vais escolher? Elas sentem-te, odeiam-te e cacam-te.")
            , Entry("raw.night.drop", "Площадь опустела. Второй фрагмент падает на дорогу, видимый всем: не из тела, а как доказательство выбранного пути.", "The square is empty. The second fragment falls onto the road, visible to all: not from a body, but as proof of the path chosen.", "O quadrado ficou vazio. O segundo fragmento cai na estrada, visivel para todos: nao de um corpo, mas como prova do caminho escolhido.")
            , Entry("raw.night.mercy", "Она встала. Фрагмент лежит на дороге, отдельно от тел и страха. Забери его и сохрани этот след до Врат.", "She stood up. The fragment lies on the road, apart from bodies and fear. Take it and carry that trace to the Gate.", "Ela levantou-se. O fragmento esta na estrada, longe de corpos e medo. Leva-o e guarda esse rasto ate aos Portoes.")
            , Entry("raw.night.exit.cast", "Герой бросает снаряд в дверь. Ночь не отступает, но створки снова раскрываются.", "The hero throws a projectile into the door. The night does not retreat, but the doors open again.", "O heroi lanca um projetil contra a porta. A noite nao recua, mas as portas abrem-se outra vez.")
            , Entry("raw.night.exit.fragment", "Второй фрагмент говорит внутри тебя: мы не просто осколки. Мы части великих, что создали этот мир.", "The second fragment speaks inside you: we are not mere shards. We are parts of the great ones who created this world.", "O segundo fragmento fala dentro de ti: nao somos meros estilhacos. Somos partes dos grandes que criaram este mundo.")
            , Entry("raw.night.exit.mercy", "Они проявили снисхождение к каждому, и потому эти тени скитаются в мирах, созданных для них. Теперь ты несешь это знание к Вратам.", "They showed mercy to each one, and so these shadows wander through worlds made for them. Now you carry that knowledge to the Gate.", "Eles mostraram misericordia a cada um, e por isso estas sombras vagueiam por mundos feitos para elas. Agora levas esse conhecimento aos Portoes.")
            , Entry("raw.final.intro.guardians", "Перед тобой сильнейшие воины. Они не отдельные души, а души, ставшие целым, часть системы, которая защищает порядок от таких, как Fragmento.", "Before you stand the strongest warriors. They are not separate souls, but souls made whole, part of the system that protects order from beings like Fragmento.", "Diante de ti estao os guerreiros mais fortes. Nao sao almas separadas, mas almas tornadas inteiras, parte do sistema que protege a ordem de seres como Fragmento.")
            , Entry("raw.final.intro.boundaries", "Каждый твой шаг наблюдается. Даже твоя сила не позволит сделать больше шести шагов в сторону: границы держатся волей стражей.", "Every step you take is watched. Even your strength will not let you move more than six steps aside: the boundaries are held by the guardians' will.", "Cada passo teu e observado. Nem a tua forca te deixa dar mais de seis passos para o lado: as fronteiras sao mantidas pela vontade dos guardioes.")
            , Entry("raw.final.intro.violent", "Стражи против самого факта твоего существования. Ты пришел с долгом теней, и их смерть говорит громче твоих фрагментов.", "The guardians oppose the fact of your existence itself. You arrived carrying the debt of shadows, and their deaths speak louder than your fragments.", "Os guardioes opoe-se ao proprio facto da tua existencia. Chegaste com a divida das sombras, e as mortes delas falam mais alto do que os teus fragmentos.")
            , Entry("raw.final.intro.attack", "Раз ты здесь, тебя нельзя игнорировать. Но тебе не предложат цену. Тебя будут останавливать.", "Since you are here, you cannot be ignored. But no price will be offered to you. You will be stopped.", "Como estas aqui, nao podes ser ignorado. Mas nao te sera oferecido preco. Vais ser travado.")
            , Entry("raw.final.intro.peaceful", "Стражи против самой ошибки, которой ты являешься, но не могут игнорировать твое существование: ты дошел, удержав руку.", "The guardians oppose the very error that you are, but they cannot ignore your existence: you arrived while holding back your hand.", "Os guardioes opoe-se ao proprio erro que tu es, mas nao podem ignorar a tua existencia: chegaste contendo a mao.")
            , Entry("raw.final.intro.queue", "Ты душа или тень, которая должна была родиться позже. Ты пошел против очереди и дерзнул взять то, что должно было прийти не скоро.", "You are a soul, or a shadow, that was meant to be born later. You broke the queue and dared to take what was not meant to come soon.", "Es uma alma, ou uma sombra, que devia nascer mais tarde. Quebraste a fila e ousaste tomar aquilo que nao devia chegar tao cedo.")
            , Entry("raw.final.intro.offer", "Они нашли в мире существо, которому предстоит сложная судьба. Они не дадут тебе создать свою судьбу, но позволят принять эту.", "They found a being in the world that is due for a difficult fate. They will not let you create your own destiny, but they will let you accept this one.", "Encontraram no mundo um ser destinado a uma vida dificil. Nao te deixarao criar o teu destino, mas deixam-te aceitar este.")
            , Entry("raw.final.bound.1", "Остатки душ сковывают тебя. Ты не можешь шелохнуться, хотя чувствуешь, как цепи уже трещат.", "The remnants of souls bind you. You cannot move, though you feel the chains already cracking.", "Os restos das almas prendem-te. Nao consegues mover-te, embora sintas as correntes ja a rachar.")
            , Entry("raw.final.bound.2", "Мы не удержим тебя долго. Но этого хватит, чтобы Врата записали твой путь как ярость.", "We will not hold you for long. But it will be enough for the Gate to record your path as rage.", "Nao te vamos segurar por muito tempo. Mas bastara para os Portoes registarem o teu caminho como raiva.")
            , Entry("raw.gate.violent", "Ты принес смерть. Защитники не примут цену. Выживи, убей стражей и открой Врата силой.", "You brought death. The defenders will not accept a price. Survive, kill the guardians, and force the Gate open.", "Trouxeste morte. Os defensores nao aceitarao um preco. Sobrevive, mata os guardioes e abre os Portoes pela forca.")
            , Entry("raw.gate.peaceful.prompt", "Стражи заковывают тебя в цепи и указывают на Врата. Все, что ты создашь, будет разрушено; все, кто дорог, умрут; ты продолжишь путь. Но если вернешься, неся второго внутри, они обещают лучшую жизнь. Что ты выберешь?", "The guardians chain you and point to the Gate. Everything you create will be destroyed; everyone dear to you will die; you will continue. But if you return carrying the second one within, they promise a better life. What do you choose?", "Os guardioes acorrentam-te e apontam para os Portoes. Tudo o que criares sera destruido; todos os que amares morrerao; tu continuas. Mas se voltares trazendo o segundo dentro de ti, prometem uma vida melhor. O que escolhes?")
            , Entry("raw.gate.accept.chains", "Принять цепи", "Accept the chains", "Aceitar as correntes")
            , Entry("raw.gate.fight", "Драться", "Fight", "Lutar")
            , Entry("raw.gate.destroy", "Уничтожить фрагменты", "Destroy the fragments", "Destruir os fragmentos")
            , Entry("raw.gate.accept.1", "Ты идешь к Вратам в цепях. Внутри кипит второй, тот, кого ты не выпустил и собственноручно заковал.", "You walk to the Gate in chains. Inside you boils the second one, the one you did not release and chained with your own hands.", "Caminhas para os Portoes acorrentado. Dentro de ti ferve o segundo, aquele que nao libertaste e que prendeste com as tuas proprias maos.")
            , Entry("raw.gate.accept.2", "Единственный путь пройти - использовать силу фрагмента в момент перехода: сохранить часть памяти и вместе со вторым пробиться внутрь, чтобы родиться чем-то новым.", "The only way through is to use the fragment's power at the moment of crossing: keep part of memory and break through with the second one to be born as something new.", "A unica forma de passar e usar a forca do fragmento no momento da travessia: guardar parte da memoria e romper com o segundo para nascer como algo novo.")
            , Entry("raw.ending.destroy.title", "ФИНАЛ: ФРАГМЕНТ ПОГАШЕН", "ENDING: FRAGMENT EXTINGUISHED", "FINAL: FRAGMENTO APAGADO")
            , Entry("raw.ending.destroy.text", "Усилием воли ты гасишь фрагменты.\n\nВсе вокруг становится тише. Маленький кусочек перестает существовать в этом мире.\n\nИ вместе с ним гаснет часть возможного счастья.\n\nСистема не наказывает тебя. Ей больше нечего записывать.", "By force of will, you extinguish the fragments.\n\nEverything around you becomes quieter. A small piece stops existing in this world.\n\nWith it, part of possible happiness also goes dark.\n\nThe system does not punish you. It has nothing left to record.", "Por vontade propria, apagas os fragmentos.\n\nTudo a tua volta fica mais silencioso. Um pequeno pedaco deixa de existir neste mundo.\n\nCom ele, uma parte da felicidade possivel tambem se apaga.\n\nO sistema nao te pune. Ja nao tem nada para registar.")
            , Entry("raw.ending.force.title", "ФИНАЛ: ПУТЬ ЯРОСТИ", "ENDING: PATH OF RAGE", "FINAL: CAMINHO DA RAIVA")
            , Entry("raw.ending.force.text", "Ты родился.\n\nТы не помнишь всего, что произошло до Врат. Но внутри тебя все еще кипит то, что не смогло умереть.\n\nТвой фрагмент остался с тобой - не как свет, а как ярость.\n\nТеперь ты заперт в новом теле вместе с собственным бессилием. Каким бы злым ты ни стал, ты никогда не превзойдешь ярость того, что осталось внутри.\n\nИногда тебе будут сниться тени. Но они больше не будут просить о помощи.\n\nОни будут помнить.", "You are born.\n\nYou do not remember everything that happened before the Gate. But inside you, something that could not die is still boiling.\n\nYour fragment stayed with you, not as light, but as rage.\n\nNow you are locked inside a new body with your own helplessness. No matter how angry you become, you will never surpass the rage of what remains inside.\n\nSometimes you will dream of shadows. But they will no longer ask for help.\n\nThey will remember.", "Nasceste.\n\nNao te lembras de tudo o que aconteceu antes dos Portoes. Mas dentro de ti ainda ferve algo que nao conseguiu morrer.\n\nO teu fragmento ficou contigo, nao como luz, mas como raiva.\n\nAgora estas preso num novo corpo com a tua propria impotencia. Por mais raiva que sintas, nunca vais ultrapassar a raiva daquilo que ficou dentro de ti.\n\nAs vezes vais sonhar com sombras. Mas elas ja nao vao pedir ajuda.\n\nElas vao lembrar-se.")
            , Entry("raw.ending.paid.title", "ФИНАЛ: ПУТЬ ХРАНИТЕЛЯ", "ENDING: PATH OF THE KEEPER", "FINAL: CAMINHO DO GUARDIAO")
            , Entry("raw.ending.paid.text", "Ты родился.\n\nТы не помнишь всего, что произошло до Врат. Но иногда во сне ты видишь одинокую тень.\n\nОна смотрит на тебя из темноты - не с ненавистью, а с тихим пониманием. Будто все еще помогает тебе найти путь.\n\nТвой фрагмент остался внутри. В нем все еще живет ярость, которую ты мог выпустить на мир.\n\nНо теперь ты не ее пленник.\n\nТы ее хранитель.\n\nИ однажды, когда придет время, ты вспомнишь, зачем она была оставлена в тебе.", "You are born.\n\nYou do not remember everything that happened before the Gate. But sometimes, in sleep, you see a lone shadow.\n\nIt watches you from the dark, not with hatred, but with quiet understanding. As if it is still helping you find the way.\n\nYour fragment stayed inside. In it, the rage you could have released into the world is still alive.\n\nBut now you are not its prisoner.\n\nYou are its keeper.\n\nAnd one day, when the time comes, you will remember why it was left inside you.", "Nasceste.\n\nNao te lembras de tudo o que aconteceu antes dos Portoes. Mas as vezes, no sono, ves uma sombra solitaria.\n\nEla olha para ti da escuridao, nao com odio, mas com uma compreensao silenciosa. Como se ainda te ajudasse a encontrar o caminho.\n\nO teu fragmento ficou dentro de ti. Nele ainda vive a raiva que poderias ter libertado sobre o mundo.\n\nMas agora nao es prisioneiro dela.\n\nEs o seu guardiao.\n\nE um dia, quando chegar a hora, vais lembrar-te porque ela foi deixada dentro de ti.")
        };
    }

    private static LocalizationCatalog.Entry Entry(string key, string russian, string english, string portuguese)
    {
        return new LocalizationCatalog.Entry { key = key, russian = russian, english = english, portuguese = portuguese };
    }

    private static void CreateFrontendPrefab()
    {
        GameObject root = CreateCanvasRoot("FrontendMenu", 700);
        CreateStretchImage("Background", root.transform, Background);
        CreateTerminalDecoration(root.transform);
        TMP_Dropdown language = CreateDropdown("QuickLanguage", root.transform, new Vector2(-170f, -28f), new Vector2(260f, 42f), new Vector2(1f, 1f));

        GameObject startPanel = CreateCenteredPanel("StartPanel", root.transform, new Vector2(560f, 300f));
        CreateLocalizedText("Title", startPanel.transform, "app.title", 54f, new Vector2(0f, 62f), new Vector2(500f, 70f), TextAlignmentOptions.Center, Cyan);
        Button start = CreateButton("StartButton", startPanel.transform, "menu.start", new Vector2(0f, -56f), new Vector2(330f, 56f));

        GameObject introPanel = CreateCenteredPanel("IntroductionPanel", root.transform, new Vector2(760f, 420f));
        CreateLocalizedText("IntroTitle", introPanel.transform, "app.title", 40f, new Vector2(0f, 130f), new Vector2(700f, 54f), TextAlignmentOptions.Center, Cyan);
        CreateLocalizedText("IntroText", introPanel.transform, "menu.intro", 24f, new Vector2(0f, 20f), new Vector2(680f, 150f), TextAlignmentOptions.Center, Text);
        Button introContinue = CreateButton("ContinueButton", introPanel.transform, "menu.continue_intro", new Vector2(0f, -138f), new Vector2(330f, 52f));

        GameObject mainPanel = CreateCenteredPanel("MainMenuPanel", root.transform, new Vector2(640f, 560f));
        CreateLocalizedText("MainTitle", mainPanel.transform, "app.title", 46f, new Vector2(0f, 206f), new Vector2(580f, 58f), TextAlignmentOptions.Center, Cyan);
        Button newGame = CreateButton("NewGameButton", mainPanel.transform, "menu.new_game", new Vector2(0f, 118f), new Vector2(390f, 48f));
        Button continueGame = CreateButton("ContinueButton", mainPanel.transform, "menu.continue", new Vector2(0f, 62f), new Vector2(390f, 48f));
        Button loadSave = CreateButton("LoadSaveButton", mainPanel.transform, "menu.load_save", new Vector2(0f, 6f), new Vector2(390f, 48f));
        Button settings = CreateButton("SettingsButton", mainPanel.transform, "menu.settings", new Vector2(0f, -50f), new Vector2(390f, 48f));
        Button credits = CreateButton("CreditsButton", mainPanel.transform, "menu.credits", new Vector2(0f, -106f), new Vector2(390f, 48f));
        Button exit = CreateButton("ExitButton", mainPanel.transform, "menu.exit", new Vector2(0f, -162f), new Vector2(390f, 48f), Amber);

        GameObject creditsPanel = CreateCenteredPanel("CreditsPanel", root.transform, new Vector2(560f, 300f));
        CreateLocalizedText("CreditsTitle", creditsPanel.transform, "menu.credits", 32f, new Vector2(0f, 82f), new Vector2(500f, 48f), TextAlignmentOptions.Center, Cyan);
        CreateLocalizedText("CreditsText", creditsPanel.transform, "menu.credits_text", 40f, Vector2.zero, new Vector2(500f, 70f), TextAlignmentOptions.Center, Text);
        Button creditsBack = CreateButton("BackButton", creditsPanel.transform, "menu.back", new Vector2(0f, -96f), new Vector2(280f, 46f));

        SettingsPanelController settingsPanel = CreateSettingsPanel(root.transform);
        SaveSlotPanelController savePanel = CreateSavePanel(root.transform);
        ConfirmPanel confirm = CreateConfirmPanel(root.transform);

        FrontendMenuController controller = root.AddComponent<FrontendMenuController>();
        SetReference(controller, "startPanel", startPanel);
        SetReference(controller, "introductionPanel", introPanel);
        SetReference(controller, "mainMenuPanel", mainPanel);
        SetReference(controller, "creditsPanel", creditsPanel);
        SetReference(controller, "confirmPanel", confirm.Panel);
        SetReference(controller, "confirmText", confirm.Text);
        SetReference(controller, "startButton", start);
        SetReference(controller, "introContinueButton", introContinue);
        SetReference(controller, "newGameButton", newGame);
        SetReference(controller, "continueButton", continueGame);
        SetReference(controller, "loadSaveButton", loadSave);
        SetReference(controller, "settingsButton", settings);
        SetReference(controller, "creditsButton", credits);
        SetReference(controller, "exitButton", exit);
        SetReference(controller, "creditsBackButton", creditsBack);
        SetReference(controller, "confirmYesButton", confirm.Yes);
        SetReference(controller, "confirmNoButton", confirm.No);
        SetReference(controller, "languageDropdown", language);
        SetReference(controller, "settingsPanel", settingsPanel);
        SetReference(controller, "saveSlotPanel", savePanel);

        PrefabUtility.SaveAsPrefabAsset(root, FrontendPrefab);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreatePausePrefab()
    {
        GameObject root = CreateCanvasRoot("PauseMenu", 650);
        GameObject dimmer = CreateStretchImage("PauseDimmer", root.transform, new Color(0f, 0f, 0f, 0.6f)).gameObject;
        GameObject menuPanel = CreateCenteredPanel("PauseRootPanel", root.transform, new Vector2(580f, 500f));
        CreateLocalizedText("PauseTitle", menuPanel.transform, "menu.pause", 38f, new Vector2(0f, 188f), new Vector2(520f, 54f), TextAlignmentOptions.Center, Cyan);
        Button resume = CreateButton("ResumeButton", menuPanel.transform, "menu.resume", new Vector2(0f, 108f), new Vector2(390f, 48f));
        Button settings = CreateButton("SettingsButton", menuPanel.transform, "menu.settings", new Vector2(0f, 52f), new Vector2(390f, 48f));
        Button loadSave = CreateButton("LoadSaveButton", menuPanel.transform, "menu.load_save", new Vector2(0f, -4f), new Vector2(390f, 48f));
        Button mainMenu = CreateButton("MainMenuButton", menuPanel.transform, "menu.main_menu", new Vector2(0f, -60f), new Vector2(390f, 48f));
        Button exit = CreateButton("ExitButton", menuPanel.transform, "menu.exit", new Vector2(0f, -116f), new Vector2(390f, 48f), Amber);
        SettingsPanelController settingsPanel = CreateSettingsPanel(root.transform);
        SaveSlotPanelController savePanel = CreateSavePanel(root.transform);
        ConfirmPanel confirm = CreateConfirmPanel(root.transform);

        PauseMenuController controller = root.AddComponent<PauseMenuController>();
        SetReference(controller, "pauseDimmer", dimmer);
        SetReference(controller, "rootPanel", menuPanel);
        SetReference(controller, "confirmPanel", confirm.Panel);
        SetReference(controller, "confirmText", confirm.Text);
        SetReference(controller, "resumeButton", resume);
        SetReference(controller, "settingsButton", settings);
        SetReference(controller, "loadSaveButton", loadSave);
        SetReference(controller, "mainMenuButton", mainMenu);
        SetReference(controller, "exitButton", exit);
        SetReference(controller, "confirmYesButton", confirm.Yes);
        SetReference(controller, "confirmNoButton", confirm.No);
        SetReference(controller, "settingsPanel", settingsPanel);
        SetReference(controller, "saveSlotPanel", savePanel);

        PrefabUtility.SaveAsPrefabAsset(root, PausePrefab);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static SettingsPanelController CreateSettingsPanel(Transform parent)
    {
        GameObject root = CreateCenteredPanel("SettingsPanel", parent, new Vector2(1080f, 650f));
        SettingsPanelController controller = root.AddComponent<SettingsPanelController>();
        CreateLocalizedText("Title", root.transform, "settings.title", 34f, new Vector2(0f, 278f), new Vector2(960f, 48f), TextAlignmentOptions.Center, Cyan);
        Button back = CreateButton("BackButton", root.transform, "menu.back", new Vector2(0f, -278f), new Vector2(260f, 44f));

        string[] tabKeys = { "settings.audio", "settings.video", "settings.controls", "settings.language", "settings.accessibility" };
        Button[] tabs = new Button[tabKeys.Length];
        GameObject[] panels = new GameObject[tabKeys.Length];
        for (int i = 0; i < tabKeys.Length; i++)
        {
            tabs[i] = CreateButton($"Tab_{i}", root.transform, tabKeys[i], new Vector2(-424f, 190f - i * 60f), new Vector2(190f, 44f));
            panels[i] = CreatePanel($"TabPanel_{i}", root.transform, new Vector2(170f, 0f), new Vector2(680f, 460f), PanelSoft);
        }

        int audioY = 172;
        Slider master = AddSliderRow(panels[0].transform, "settings.master", ref audioY, 0f, 1f);
        Slider music = AddSliderRow(panels[0].transform, "settings.music", ref audioY, 0f, 1f);
        Slider sfx = AddSliderRow(panels[0].transform, "settings.sfx", ref audioY, 0f, 1f);
        Slider voice = AddSliderRow(panels[0].transform, "settings.voice", ref audioY, 0f, 1f);
        Toggle mute = AddToggleRow(panels[0].transform, "settings.mute", ref audioY);

        int videoY = 172;
        TMP_Dropdown resolution = AddDropdownRow(panels[1].transform, "settings.resolution", ref videoY);
        Toggle fullscreen = AddToggleRow(panels[1].transform, "settings.fullscreen", ref videoY);
        Slider brightness = AddSliderRow(panels[1].transform, "settings.brightness", ref videoY, 0.4f, 1.2f);
        TMP_Dropdown quality = AddDropdownRow(panels[1].transform, "settings.quality", ref videoY);
        Toggle vSync = AddToggleRow(panels[1].transform, "settings.vsync", ref videoY);

        int controlsY = 172;
        Slider sensitivity = AddSliderRow(panels[2].transform, "settings.mouse_sensitivity", ref controlsY, 0.2f, 2f);
        Toggle invertY = AddToggleRow(panels[2].transform, "settings.invert_y", ref controlsY);
        CreateLocalizedText("StaticControls", panels[2].transform, "settings.static_controls", 20f, new Vector2(-280f, -44f), new Vector2(580f, 220f), TextAlignmentOptions.TopLeft, Text);

        int languageY = 172;
        TMP_Dropdown language = AddDropdownRow(panels[3].transform, "settings.language", ref languageY);

        int accessibilityY = 190;
        Toggle subtitles = AddToggleRow(panels[4].transform, "settings.subtitles", ref accessibilityY, 34);
        TMP_Dropdown subtitleSize = AddDropdownRow(panels[4].transform, "settings.subtitle_size", ref accessibilityY, 34);
        Slider uiScale = AddSliderRow(panels[4].transform, "settings.ui_scale", ref accessibilityY, 0.8f, 1.4f, 34);
        Toggle highContrast = AddToggleRow(panels[4].transform, "settings.high_contrast", ref accessibilityY, 34);
        Toggle colorblind = AddToggleRow(panels[4].transform, "settings.colorblind", ref accessibilityY, 34);
        Toggle reduceShake = AddToggleRow(panels[4].transform, "settings.reduce_shake", ref accessibilityY, 34);
        Toggle reduceMotion = AddToggleRow(panels[4].transform, "settings.reduce_motion", ref accessibilityY, 34);
        Toggle tutorialHints = AddToggleRow(panels[4].transform, "settings.tutorial_hints", ref accessibilityY, 34);
        Toggle holdMode = AddToggleRow(panels[4].transform, "settings.hold_mode", ref accessibilityY, 34);
        Toggle toggleRun = AddToggleRow(panels[4].transform, "settings.toggle_run", ref accessibilityY, 34);
        Toggle simplePrompts = AddToggleRow(panels[4].transform, "settings.simple_prompts", ref accessibilityY, 34);

        SetReferenceArray(controller, "tabPanels", panels);
        SetReferenceArray(controller, "tabButtons", tabs);
        SetReference(controller, "backButton", back);
        SetReference(controller, "masterVolume", master);
        SetReference(controller, "musicVolume", music);
        SetReference(controller, "sfxVolume", sfx);
        SetReference(controller, "voiceVolume", voice);
        SetReference(controller, "muteAll", mute);
        SetReference(controller, "resolution", resolution);
        SetReference(controller, "fullscreen", fullscreen);
        SetReference(controller, "brightness", brightness);
        SetReference(controller, "quality", quality);
        SetReference(controller, "vSync", vSync);
        SetReference(controller, "mouseSensitivity", sensitivity);
        SetReference(controller, "invertY", invertY);
        SetReference(controller, "language", language);
        SetReference(controller, "subtitles", subtitles);
        SetReference(controller, "subtitleSize", subtitleSize);
        SetReference(controller, "uiScale", uiScale);
        SetReference(controller, "highContrast", highContrast);
        SetReference(controller, "colorblindFriendly", colorblind);
        SetReference(controller, "reduceScreenShake", reduceShake);
        SetReference(controller, "reduceMotion", reduceMotion);
        SetReference(controller, "tutorialHints", tutorialHints);
        SetReference(controller, "holdInsteadOfRepeat", holdMode);
        SetReference(controller, "toggleRun", toggleRun);
        SetReference(controller, "simplePrompts", simplePrompts);
        root.SetActive(false);
        return controller;
    }

    private static SaveSlotPanelController CreateSavePanel(Transform parent)
    {
        GameObject root = CreateCenteredPanel("SaveSlotPanel", parent, new Vector2(900f, 560f));
        CreateLocalizedText("Title", root.transform, "save.title", 32f, new Vector2(0f, 224f), new Vector2(820f, 48f), TextAlignmentOptions.Center, Cyan);
        TMP_Text[] labels = new TMP_Text[3];
        Button[] loadButtons = new Button[3];
        Button[] saveButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 132f - i * 118f;
            GameObject row = CreatePanel($"Slot_{i + 1}", root.transform, new Vector2(0f, y), new Vector2(800f, 96f), new Color(0.02f, 0.08f, 0.11f, 0.96f));
            labels[i] = CreateText("SlotLabel", row.transform, string.Empty, 17f, new Vector2(-370f, 0f), new Vector2(440f, 78f), TextAlignmentOptions.Left, Text);
            loadButtons[i] = CreateButton("LoadButton", row.transform, "save.load", new Vector2(194f, 0f), new Vector2(150f, 42f));
            saveButtons[i] = CreateButton("SaveButton", row.transform, "save.save", new Vector2(326f, 0f), new Vector2(104f, 42f));
        }

        Button back = CreateButton("BackButton", root.transform, "menu.back", new Vector2(0f, -224f), new Vector2(260f, 44f));
        SaveSlotPanelController controller = root.AddComponent<SaveSlotPanelController>();
        SetReferenceArray(controller, "slotLabels", labels);
        SetReferenceArray(controller, "loadButtons", loadButtons);
        SetReferenceArray(controller, "saveButtons", saveButtons);
        SetReference(controller, "backButton", back);
        root.SetActive(false);
        return controller;
    }

    private static ConfirmPanel CreateConfirmPanel(Transform parent)
    {
        GameObject root = CreatePanel("ConfirmPanel", parent, Vector2.zero, new Vector2(560f, 240f), Panel);
        AddBorder(root.transform);
        TMP_Text text = CreateText("ConfirmText", root.transform, string.Empty, 24f, new Vector2(0f, 44f), new Vector2(500f, 86f), TextAlignmentOptions.Center, Text);
        Button yes = CreateButton("YesButton", root.transform, "confirm.yes", new Vector2(-112f, -68f), new Vector2(180f, 44f), Amber);
        Button no = CreateButton("NoButton", root.transform, "confirm.no", new Vector2(112f, -68f), new Vector2(180f, 44f));
        root.SetActive(false);
        return new ConfirmPanel(root, text, yes, no);
    }

    private static Slider AddSliderRow(Transform parent, string key, ref int y, float min, float max, int step = 54)
    {
        CreateLocalizedText(key + "_Label", parent, key, 17f, new Vector2(-300f, y), new Vector2(310f, 30f), TextAlignmentOptions.Left, Text);
        Slider slider = CreateSlider(key + "_Slider", parent, new Vector2(180f, y), new Vector2(280f, 24f), min, max);
        y -= step;
        return slider;
    }

    private static Toggle AddToggleRow(Transform parent, string key, ref int y, int step = 54)
    {
        Toggle toggle = CreateToggle(key + "_Toggle", parent, new Vector2(-300f, y), new Vector2(580f, 28f), key);
        y -= step;
        return toggle;
    }

    private static TMP_Dropdown AddDropdownRow(Transform parent, string key, ref int y, int step = 54)
    {
        CreateLocalizedText(key + "_Label", parent, key, 17f, new Vector2(-300f, y), new Vector2(310f, 30f), TextAlignmentOptions.Left, Text);
        TMP_Dropdown dropdown = CreateDropdown(key + "_Dropdown", parent, new Vector2(180f, y), new Vector2(280f, 34f), new Vector2(0.5f, 0.5f));
        y -= step;
        return dropdown;
    }

    private static GameObject CreateCanvasRoot(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreatePanel(name, parent, Vector2.zero, size, Panel);
        AddBorder(panel.transform);
        CreateImage("TopAccent", panel.transform, new Vector2(0f, size.y * 0.5f - 3f), new Vector2(size.x, 6f), Cyan);
        return panel;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = CreateRect(name, parent, position, size, new Vector2(0.5f, 0.5f));
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    private static void CreateTerminalDecoration(Transform parent)
    {
        CreateImage("TopLine", parent, new Vector2(0f, -14f), new Vector2(1240f, 2f), CyanSoft, new Vector2(0.5f, 1f));
        CreateText("TopStatus", parent, "SYSTEM // VIRUS9 // ONLINE", 16f, new Vector2(24f, -34f), new Vector2(500f, 28f), TextAlignmentOptions.Left, Cyan, new Vector2(0f, 1f));
        CreateText("BottomStatus", parent, "SOUL WORLD PROTOCOL // BUILD 09", 14f, new Vector2(24f, 22f), new Vector2(500f, 24f), TextAlignmentOptions.Left, CyanSoft, new Vector2(0f, 0f));
    }

    private static void AddBorder(Transform parent)
    {
        RectTransform rect = parent.GetComponent<RectTransform>();
        Vector2 size = rect.sizeDelta;
        CreateImage("BorderTop", parent, new Vector2(0f, size.y * 0.5f), new Vector2(size.x, 2f), CyanSoft);
        CreateImage("BorderBottom", parent, new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, 2f), CyanSoft);
        CreateImage("BorderLeft", parent, new Vector2(-size.x * 0.5f, 0f), new Vector2(2f, size.y), CyanSoft);
        CreateImage("BorderRight", parent, new Vector2(size.x * 0.5f, 0f), new Vector2(2f, size.y), CyanSoft);
    }

    private static Button CreateButton(string name, Transform parent, string key, Vector2 position, Vector2 size, Color? accent = null)
    {
        GameObject obj = CreateRect(name, parent, position, size, new Vector2(0.5f, 0.5f));
        Image image = obj.AddComponent<Image>();
        Color baseColor = accent ?? CyanSoft;
        image.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.82f);
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(Amber.r, Amber.g, Amber.b, 0.95f);
        colors.disabledColor = new Color(0.08f, 0.12f, 0.14f, 0.7f);
        button.colors = colors;
        CreateLocalizedText("Label", obj.transform, key, 18f, Vector2.zero, size - new Vector2(16f, 8f), TextAlignmentOptions.Center, Text);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size, float min, float max)
    {
        GameObject root = CreateRect(name, parent, position, size, new Vector2(0.5f, 0.5f));
        Slider slider = root.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        GameObject background = CreateImage("Background", root.transform, Vector2.zero, new Vector2(size.x, 8f), new Color(0.06f, 0.14f, 0.18f, 1f)).gameObject;
        GameObject fillArea = CreateRect("FillArea", root.transform, Vector2.zero, new Vector2(size.x - 12f, 8f), new Vector2(0.5f, 0.5f));
        Image fill = CreateStretchImage("Fill", fillArea.transform, Cyan);
        GameObject handleArea = CreateRect("HandleSlideArea", root.transform, Vector2.zero, new Vector2(size.x - 16f, size.y), new Vector2(0.5f, 0.5f));
        Image handle = CreateImage("Handle", handleArea.transform, Vector2.zero, new Vector2(18f, 18f), Amber);
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 position, Vector2 size, string key)
    {
        GameObject root = CreateRect(name, parent, position, size, new Vector2(0.5f, 0.5f));
        Image box = CreateImage("Background", root.transform, new Vector2(12f, 0f), new Vector2(22f, 22f), CyanSoft, new Vector2(0f, 0.5f));
        Image check = CreateImage("Checkmark", box.transform, Vector2.zero, new Vector2(14f, 14f), Amber);
        Toggle toggle = root.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        CreateLocalizedText("Label", root.transform, key, 16f, new Vector2(42f, 0f), new Vector2(size.x - 44f, size.y), TextAlignmentOptions.Left, Text, new Vector2(0f, 0.5f));
        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject root = CreateRect(name, parent, position, size, anchor);
        Image image = root.AddComponent<Image>();
        image.color = new Color(CyanSoft.r, CyanSoft.g, CyanSoft.b, 0.9f);
        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        TMP_Text caption = CreateText("Label", root.transform, string.Empty, 16f, new Vector2(-size.x * 0.5f + 10f, 0f), new Vector2(size.x - 44f, size.y), TextAlignmentOptions.Left, Text, new Vector2(0f, 0.5f));
        CreateText("Arrow", root.transform, "v", 18f, new Vector2(-14f, 0f), new Vector2(24f, size.y), TextAlignmentOptions.Center, Text, new Vector2(1f, 0.5f));

        GameObject template = CreateRect("Template", root.transform, new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, 150f), new Vector2(0.5f, 0f));
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = Panel;
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        GameObject viewport = CreateRect("Viewport", template.transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.05f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        GameObject content = CreateRect("Content", viewport.transform, Vector2.zero, new Vector2(0f, 34f), new Vector2(0.5f, 1f));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 34f);
        GameObject item = CreateRect("Item", content.transform, Vector2.zero, new Vector2(0f, 34f), new Vector2(0.5f, 1f));
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.04f, 0.12f, 0.16f, 0.95f);
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        Image checkmark = CreateImage("Item Checkmark", item.transform, new Vector2(12f, 0f), new Vector2(12f, 12f), Amber, new Vector2(0f, 0.5f));
        toggle.graphic = checkmark;
        TMP_Text itemText = CreateText("Item Label", item.transform, string.Empty, 15f, new Vector2(30f, 0f), new Vector2(size.x - 38f, 30f), TextAlignmentOptions.Left, Text, new Vector2(0f, 0.5f));
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        dropdown.targetGraphic = image;
        dropdown.captionText = caption;
        dropdown.template = template.GetComponent<RectTransform>();
        dropdown.itemText = itemText;
        template.SetActive(false);
        return dropdown;
    }

    private static TMP_Text CreateLocalizedText(string name, Transform parent, string key, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color, Vector2? anchor = null)
    {
        TMP_Text text = CreateText(name, parent, string.Empty, fontSize, position, size, alignment, color, anchor);
        text.gameObject.AddComponent<LocalizedText>().Configure(key, key);
        return text;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color, Vector2? anchor = null)
    {
        GameObject obj = CreateRect(name, parent, position, size, anchor ?? new Vector2(0.5f, 0.5f));
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
    {
        GameObject obj = CreateRect(name, parent, position, size, anchor ?? new Vector2(0.5f, 0.5f));
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Image CreateStretchImage(string name, Transform parent, Color color)
    {
        GameObject obj = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        RectTransform rect = obj.GetComponent<RectTransform>();
        Stretch(rect);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateFrontendScene()
    {
        Scene originalScene = SceneManager.GetActiveScene();
        string originalPath = originalScene.path;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneIds.Menu;
        new GameObject("_UI_ROOT");
        new GameObject("_SYSTEMS");
        GameObject cameras = new GameObject("_CAMERAS");
        GameObject cameraObject = new GameObject("Menu Camera", typeof(Camera));
        cameraObject.transform.SetParent(cameras.transform);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;

        GameObject uiRoot = GameObject.Find("_UI_ROOT");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrontendPrefab);
        GameObject frontend = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        frontend.transform.SetParent(uiRoot.transform, false);
        EnsureEventSystem();
        EditorSceneManager.SaveScene(scene, FrontendScene);

        if (!string.IsNullOrWhiteSpace(originalPath) && System.IO.File.Exists(originalPath))
        {
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(FrontendScene, true),
            new EditorBuildSettingsScene(ExteriorScene, true),
            new EditorBuildSettingsScene(NightScene, true),
            new EditorBuildSettingsScene(FinalScene, true)
        };
    }

    private static void ValidateLocalizationCatalog(LocalizationCatalog catalog, List<string> issues)
    {
        string[] requiredKeys =
        {
            "hud.health",
            "hud.boss",
            "hud.boss.phase2",
            "hud.damage",
            "intro.gameplay.1",
            "intro.gameplay.2",
            "intro.gameplay.3",
            "intro.gameplay.4",
            "intro.gameplay.5",
            "boss.intro",
            "boss.phase2",
            "speaker.gate",
            "save.saved_at",
            "save.scene.exterior",
            "save.scene.night",
            "save.scene.final",
            "ending.restart"
        };
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (LocalizationCatalog.Entry entry in catalog.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                issues.Add("Localization catalog contains an entry without a key.");
                continue;
            }

            if (!keys.Add(entry.key)) issues.Add($"Localization catalog contains duplicate key: {entry.key}");
            if (string.IsNullOrWhiteSpace(entry.russian)) issues.Add($"Russian localization is missing: {entry.key}");
            if (string.IsNullOrWhiteSpace(entry.english)) issues.Add($"English localization is missing: {entry.key}");
            if (string.IsNullOrWhiteSpace(entry.portuguese)) issues.Add($"Portuguese localization is missing: {entry.key}");
            if (ContainsMojibake(entry.russian)) issues.Add($"Russian localization looks mojibaked: {entry.key}");
            if (ContainsMojibake(entry.english)) issues.Add($"English localization looks mojibaked: {entry.key}");
            if (ContainsMojibake(entry.portuguese)) issues.Add($"Portuguese localization looks mojibaked: {entry.key}");
        }

        foreach (string key in requiredKeys)
        {
            if (!keys.Contains(key)) issues.Add($"Localization catalog is missing required key: {key}");
        }
    }

    private static void ValidateSerializedDialogueLocalization(LocalizationCatalog catalog, List<string> issues)
    {
        HashSet<string> translatedRussian = new HashSet<string>(
            catalog.Entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.russian))
                .Select(entry => entry.russian),
            StringComparer.Ordinal);
        Scene activeScene = SceneManager.GetActiveScene();
        string[] paths = { ExteriorScene, NightScene, FinalScene };
        foreach (string path in paths)
        {
            Scene scene = activeScene.path == path
                ? activeScene
                : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    SerializedObject serialized = new SerializedObject(behaviour);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.String) continue;
                        string value = property.stringValue;
                        if (string.IsNullOrWhiteSpace(value) || !ContainsCyrillic(value) || translatedRussian.Contains(value)) continue;
                        issues.Add($"Serialized dialogue localization is missing: {scene.name}/{behaviour.name}/{behaviour.GetType().Name}.{property.propertyPath} => {value}");
                    }
                }
            }

            if (scene != activeScene) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateRawLocalizationMappings(LocalizationCatalog catalog, List<string> issues)
    {
        Dictionary<string, LocalizationCatalog.Entry> mappings =
            new Dictionary<string, LocalizationCatalog.Entry>(StringComparer.Ordinal);
        foreach (LocalizationCatalog.Entry entry in catalog.Entries.Where(entry => entry != null && entry.key.StartsWith("raw.", StringComparison.Ordinal)))
        {
            ValidateRawMappingValue(entry.russian, entry, mappings, issues);
            ValidateRawMappingValue(entry.english, entry, mappings, issues);
            ValidateRawMappingValue(entry.portuguese, entry, mappings, issues);
        }
    }

    private static void ValidateRawMappingValue(
        string value,
        LocalizationCatalog.Entry entry,
        Dictionary<string, LocalizationCatalog.Entry> mappings,
        List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!mappings.TryGetValue(value, out LocalizationCatalog.Entry previous))
        {
            mappings[value] = entry;
            return;
        }

        bool sameMapping =
            previous.russian == entry.russian &&
            previous.english == entry.english &&
            previous.portuguese == entry.portuguese;
        if (!sameMapping)
        {
            issues.Add($"Ambiguous raw localization value: '{value}' is used by {previous.key} and {entry.key}.");
        }
    }

    private static void ValidateScriptDialogueLocalization(LocalizationCatalog catalog, List<string> issues)
    {
        HashSet<string> translatedRussian = new HashSet<string>(
            catalog.Entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.russian))
                .Select(entry => entry.russian),
            StringComparer.Ordinal);
        string[] paths = Directory.GetFiles("Assets/Scripts/Environment", "*.cs", SearchOption.AllDirectories);
        foreach (string path in paths)
        {
            string source = File.ReadAllText(path);
            foreach (Match match in Regex.Matches(source, "\"((?:\\\\.|[^\"\\\\])*)\""))
            {
                string value = Regex.Unescape(match.Groups[1].Value);
                if (!ContainsCyrillic(value) || translatedRussian.Contains(value)) continue;
                issues.Add($"Script dialogue localization is missing: {path} => {value}");
            }
        }
    }

    private static bool ContainsCyrillic(string value)
    {
        return value.Any(character => character >= '\u0400' && character <= '\u04FF');
    }

    private static bool ContainsMojibake(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Contains("Đ", StringComparison.Ordinal)
               || value.Contains("đ", StringComparison.Ordinal)
               || value.Contains("Ń", StringComparison.Ordinal)
               || value.Contains("Ă", StringComparison.Ordinal)
               || value.Contains("Â", StringComparison.Ordinal)
               || value.Contains("�", StringComparison.Ordinal);
    }

    private static int CleanDefaultVolumeProfile(bool saveAssets)
    {
        UnityEngine.Object profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultVolumeProfilePath);
        if (profile == null) return 0;

        SerializedObject serialized = new SerializedObject(profile);
        SerializedProperty components = serialized.FindProperty("components");
        if (components == null || !components.isArray) return 0;

        int removed = 0;
        for (int i = components.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = components.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue != null) continue;

            components.DeleteArrayElementAtIndex(i);
            removed++;
        }

        if (removed <= 0) return 0;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        if (saveAssets) AssetDatabase.SaveAssets();
        return removed;
    }

    private static void ValidateDefaultVolumeProfile(List<string> issues)
    {
        UnityEngine.Object profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultVolumeProfilePath);
        if (profile == null) return;

        SerializedObject serialized = new SerializedObject(profile);
        SerializedProperty components = serialized.FindProperty("components");
        if (components == null || !components.isArray) return;

        for (int i = 0; i < components.arraySize; i++)
        {
            SerializedProperty element = components.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == null)
            {
                issues.Add($"DefaultVolumeProfile has a missing Volume component at index {i}. Run Tools/Virus 9/Clean Default Volume Profile.");
            }
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object reference)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null) throw new InvalidOperationException($"Serialized field is missing: {target.GetType().Name}.{fieldName}");
        property.objectReferenceValue = reference;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetReferenceArray(UnityEngine.Object target, string fieldName, UnityEngine.Object[] references)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || !property.isArray) throw new InvalidOperationException($"Serialized array is missing: {target.GetType().Name}.{fieldName}");
        property.arraySize = references.Length;
        for (int i = 0; i < references.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct ConfirmPanel
    {
        public ConfirmPanel(GameObject panel, TMP_Text text, Button yes, Button no)
        {
            Panel = panel;
            Text = text;
            Yes = yes;
            No = no;
        }

        public GameObject Panel { get; }
        public TMP_Text Text { get; }
        public Button Yes { get; }
        public Button No { get; }
    }
}
