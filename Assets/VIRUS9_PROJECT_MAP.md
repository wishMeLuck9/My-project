# Virus 9: карта проекта

## Игровой маршрут

Игровые сцены лежат в `Assets/Scenes/Playable` и подключены в таком порядке:

1. `LOCATION_01_EXTERIOR_DAY` - дневной город, первый фрагмент и погоня.
2. `LOCATION_02_PROTECTED_ALLEYS_NIGHT` - ночной квадрат, выбор между насилием и милосердием.
3. `LOCATION_03_GATE_FINAL` - финальные ворота и итог забега.

`Assets/Scenes/Archive` предназначен только для архивных сцен и не входит в Build Settings.

## Персонаж

- Модель: `Assets/Art/Characters/Player/Models/DEAD2.fbx`
- Статические циклы ходьбы и бега: `Assets/Art/Characters/Player/Animations/Animations_Static.fbx`
- Общий humanoid-контроллер: `Assets/Art/Characters/Player/Controllers/PlayerHumanoid.controller`
- Runtime-управление: `Assets/Scripts/Player`

Управление в игре:

- `WASD` - бег
- `Shift` - ходьба
- `Space` - прыжок
- `E` - взаимодействие
- `LMB` - атака после перехода в ночную фазу
- `Esc` - пауза

## Города

- Дневной город: `Assets/Art/Locations/Location01_DayMiniCity/Models`
- Ночной квадрат: `Assets/Art/Locations/Location02_ProtectedAlleysNight/Models`
- Финальные ворота: `Assets/Art/Locations/Location03_GateFinal/Models`

## Иерархия сцен

Мигратор группирует runtime-объекты в корнях сцен:

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

## Миграция после реэкспорта

Повторяемая команда находится в меню:

`Tools/Virus 9/Apply Playable Scene Migration`

Она обновляет импорт FBX, коллайдеры, NavMesh, игровые ссылки, Animator Controller и Build Settings.
