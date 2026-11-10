# MicroCarReady — документация проекта

Офроуд-гонка на радиоуправляемых машинках. Unity **6000.3.9f1**, Built-in Render Pipeline + Post Processing v2.
Основа — ассет-шаблон «Off Road Racing 3» (namespace `ALIyerEdon`) + RealRacingCarsDetach.

| Документ | О чём |
|---|---|
| [YouTube-Playables.md](YouTube-Playables.md) | Выпуск на YouTube через McPlay: требования, сборка, статус |
| [Mobile.md](Mobile.md) | Google Play и iPhone: что настроить перед выпуском |
| [Lighting.md](Lighting.md) | Запекание света и теней, бюджет размера |

## Репозиторий
- GitHub: https://github.com/rileex/ts (планируется переименование в `microcarready`).
- Ветка `main` — Unity-проект. Ветка `site-backup` — сайт-каталог, который лежал в репозитории раньше; `gh-pages` — его публикация.
- `Library/`, `Logs/`, `obj/`, `Builds/`, `CLAUDE.md` в git не попадают.

## Сцены (Build Settings)
| # | Сцена | Что делает |
|---|---|---|
| 0 | `Off_Road_Racing_3/Scene/1_firstScene` | Заставка 4 с, асинхронная загрузка гаража; здесь живёт объект рекламы (`UnityAdsScript`, DontDestroyOnLoad) |
| 1 | `Off_Road_Racing_3/Scene/Garage` | Меню: выбор/покупка машины (`CarSelect`), уровни (`LevelSelect`), настройки (`SettingsMenu`), `MainUtility` |
| 2 | `Off_Road_Racing_3/Scene/Village` | Гонка: `Race_Manager` спавнит машину игрока и AI. Свет берёт из `Scene/Race_Track_1/LightingData.asset` |
| — | `Race_Track_2`, `Race_Track_3` | Выключены в сборке |

Важно: в `CarSelect.levelNames` только `"Village"`, `Race_Manager.levelID` = 0 — фактически в игре одна трасса.
Префабы `Assets/Game/Prefabs/Levels/level1..9, freeride, levelMain` ни на что не ссылаются (только геометрия и чекпоинты) — логики выбора 9 уровней и фриройда нет.

## Сохранения (PlayerPrefs)
- Первый запуск (`MainUtility.cs`, флаг `FirstRun`=3) выставляет: `TotalScores` (деньги, 1400), `Car0`=3, `Level0`=3, `ControlType`, `Music`, `QualityLevel`, графические флаги и т. д.
- `Car{ID}`=3 — машина открыта, `Level{n}`=3 — уровень открыт, `CarID`/`LevelID` — выбор, `Award_Level_{n}` — медали, `CarColor{n}` и ключи кастомизации.
- `PlayerPrefs.Save()` нигде не вызывается.

## Управление
Старый Input Manager. Клавиатура: стрелки/WASD, Space — ручник, LeftShift — нитро, C — камера, Esc — пауза.
Тач-кнопки работают только при `controlType == Mobile`; на WebGL `InputSystem` принудительно включает клавиатуру.
Режимы на мобилках: стрелки, джойстик, акселерометр (`ControlType` 0/1/2).

## Реклама (сейчас)
AdMob (GoogleMobileAds 7.4.0), только тестовые ID. Interstitial показывается на каждой загрузке сцены (`OnLevelWasLoaded`).
Rewarded нигде не вызывается, награда закомментирована. Баннеры выключены.

## Известные проблемы (из разбора 10.11.2026)
- Читы в гараже: **H — `PlayerPrefs.DeleteAll`**, E — +14000 к неиспользуемому ключу `Coins` (`MainUtility.cs:114-120`).
- `Application.Quit` в гараже и `MainMenu`; Esc открывает меню выхода.
- `Screen.SetResolution` при смене качества (`Load_Settings.cs`, `SettingsMenu.cs`) — ломает канвас в браузере.
- UI только альбомный (CanvasScaler 1280×720, match 0.5).
- Остатки шаблона: бренд «ALIyerEdon», `companyName: SmartMove`, `productName: OffroadRacing`, bundle id `com.ALIyerEdon.MudRacer`, `Assets/Scenes/SampleScene.unity`.
