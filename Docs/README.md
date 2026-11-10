# MicroCar Racing (проект MicroCarReady) — документация

Офроуд-гонка на радиоуправляемых машинках. Unity **6000.3.9f1**, Built-in Render Pipeline + Post Processing v2.
Основа — ассет-шаблон «Off Road Racing 3» (namespace `ALIyerEdon`) + RealRacingCarsDetach.

| Документ | О чём |
|---|---|
| [YouTube-Playables.md](YouTube-Playables.md) | Выпуск на YouTube через McPlay: требования, сборка, статус |
| [Mobile.md](Mobile.md) | Google Play и iPhone: что настроить перед выпуском |
| [Lighting.md](Lighting.md) | Запекание света и теней, бюджет размера |

## Репозиторий
- GitHub: https://github.com/rileex/ts.
- Ветка `main` — Unity-проект. Ветка `site-backup` — сайт-каталог, который лежал в репозитории раньше; `gh-pages` — его публикация.
- `Library/`, `Logs/`, `obj/`, `Builds/`, `CLAUDE.md` в git не попадают.

## Сцены (Build Settings)
| # | Сцена | Что делает |
|---|---|---|
| 0 | `Off_Road_Racing_3/Scene/1_firstScene` | Заставка 4 с, асинхронная загрузка гаража; здесь живёт объект рекламы (`UnityAdsScript`, DontDestroyOnLoad) |
| 1 | `Off_Road_Racing_3/Scene/Garage` | Меню: выбор/покупка машины (`CarSelect`), уровни (`LevelSelect`), настройки (`SettingsMenu`), `MainUtility` |
| 2 | `Off_Road_Racing_3/Scene/Village` | Гонка: `Race_Manager` спавнит машину игрока и AI. Свет — `Scene/Village/LightingData.asset` (перезапечён, см. [Lighting.md](Lighting.md)) |
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

## Реклама
- **WebGL (YouTube):** YouTube Playables SDK через `Assets/_Web/AdService.cs` — Interstitial на стыках сцен, Rewarded «x2 приз» на финише. AdMob на WebGL выключен. Подробно — [YouTube-Playables.md](YouTube-Playables.md).
- **Android/iOS:** AdMob (GoogleMobileAds 7.4.0), только тестовые ID. Interstitial на каждой загрузке сцены (`OnLevelWasLoaded`), Rewarded не подключён, баннеры выключены — см. [Mobile.md](Mobile.md).

## Известные проблемы (из разбора 10.11.2026)
- ~~Читы в гараже: H — `PlayerPrefs.DeleteAll`, E — +14000 к ключу `Coins`~~ — теперь только в редакторе (E добавляет к `TotalScores`).
- ~~`Application.Quit` и меню выхода по Esc~~ — выключены на WebGL, кнопки Exit в браузере скрыты.
- ~~`Screen.SetResolution` при смене качества~~ — в браузере не вызывается (`ScreenCompat`).
- ~~На WebGL всегда клавиатура~~ — в мобильном браузере включается тач-управление.
- ~~UI только альбомный~~ — портрет и экраны уже 16:9 подстраивает `Assets/_Web/PortraitFit.cs` (см. YouTube-Playables.md). Меню настроек в портрете мелкое.
- ~~`LevelSelect.Start`: NullReferenceException на пустых слотах уровней~~ — исправлено (в WebGL это роняло игру).
- ~~`Garage.unity`: `MainUtility.startingScore = 123000`~~ — теперь 1 400, как в скрипте.
- Постеры других игр на полке в гараже (из шаблона) — решено оставить (10.11.2026).
- Play в редакторе меняет общие материалы фар (`BackLight.mat`, `Light.mat`, `Light_2.mat` — `_EmissionColor` пишут скрипты шаблона: EasyCarController и др.). Эти изменения не коммитить: `git checkout -- <файл>`.
- В шаблоне перепутаны медали: 2-е место получает иконку/приз «bronze» (2000), 3-е — «silver» (1000) (`Start_Finish_UI.cs`).
- Остатки шаблона: бренд «ALIyerEdon», `companyName: SmartMove`, bundle id `com.ALIyerEdon.MudRacer`, `Assets/Scenes/SampleScene.unity`. (`productName` теперь `MicroCar Racing`.)
- Camera Filter Pack: в `Resources` оставлен только фильтр TV_ARCADE, остальные — в `Camera Filter Pack/NotInBuild` (см. [YouTube-Playables.md](YouTube-Playables.md#размер-сборки)).
