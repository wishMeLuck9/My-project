# Virus 9: карта проекта

## Игровой маршрут

Стартовая сцена находится в `Assets/Scenes/Frontend`:

1. `MENU_BOOT` - стартовый экран, вступление и главное меню.

Игровые сцены находятся в `Assets/Scenes/Playable` и подключены после меню:

1. `LOCATION_01_EXTERIOR_DAY` - дневной город, первый фрагмент и погоня.
2. `LOCATION_02_PROTECTED_ALLEYS_NIGHT` - ночной квадрат, выбор между насилием и милосердием.
3. `LOCATION_03_GATE_FINAL` - финальные врата, бой или добровольная цена.

`Assets/Scenes/Archive` используется только для архивных сцен и не должен входить в Build Settings.

## Персонаж

- Модель: `Assets/Art/Characters/Player/Models/DEAD2.fbx`
- Статические циклы ходьбы и бега: `Assets/Art/Characters/Player/Animations/Animations_Static.fbx`
- Общий humanoid controller: `Assets/Art/Characters/Player/Controllers/PlayerHumanoid.controller`
- Runtime-управление: `Assets/Scripts/Player`

Управление в игре:

- `WASD` - бег
- `Shift` - ходьба или переключение режима, если включён `toggle_run`
- `Space` - прыжок
- `E` - взаимодействие
- `LMB` - атака после перехода в ночную фазу
- `Esc` - пауза

## Локации

- Дневной город: `Assets/Art/Locations/Location01_DayMiniCity/Models`
- Ночной квадрат: `Assets/Art/Locations/Location02_ProtectedAlleysNight/Models`
- Финальные врата: `Assets/Art/Locations/Location03_GateFinal/Models`

## Интерфейс

- Стартовое меню: `Assets/Resources/UI/FrontendMenu.prefab`
- Пауза: `Assets/Resources/UI/PauseMenu.prefab`
- Каталог RU/EN/PT локализации: `Assets/Resources/Localization/LocalizationCatalog.asset`
- UI-логика: `Assets/Scripts/UI`
- Настройки и `PlayerPrefs`: `Assets/Scripts/Settings`
- Автосейв в слоте `1`, ручные слоты `2-3`: `Assets/Scripts/Save`

## Иерархия сцен

Миграции группируют runtime-объекты в корнях сцен:

- `_ENVIRONMENT`
- `_LIGHTING`
- `_PLAYER`
- `_NPC`
- `_FRAGMENTS`
- `_PORTALS`
- `_TRIGGERS`
- `_SPAWN_POINTS`
- `_CAMERAS`
- `_SYSTEMS`
- `_NAVIGATION`

## Миграции и валидация

Повторяемая команда для игровых сцен:

`Tools/Virus 9/Apply Playable Scene Migration`

Она обновляет импорт FBX, коллайдеры, NavMesh, игровые ссылки, Animator Controller и Build Settings.

Frontend после изменения каталога или макетов пересобирается командой:

`Tools/Virus 9/Apply Frontend UI Migration`

Проверки:

- `Tools/Virus 9/Validate Frontend UI`
- `Tools/Virus 9/Validate Playable Scenes`
- `Tools/Virus 9/Clean Default Volume Profile`

Перед прогоном Tools нужно одобрить Codex MCP connection в Unity: `Project Settings > AI > Unity MCP Server`.
