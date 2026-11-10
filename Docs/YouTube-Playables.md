# YouTube Playables через McPlay

Кабинет: https://mcplay.mediacube.io/testing/games — игра загружается zip-архивом WebGL-сборки, точка входа `index.html`.
Путь статусов: Premoderation → In progress → On review → Build updated → Ready for YouTube → YT certification → Published.

Эталоны (наши проекты, уже в McPlay): `D:\UnityProjects\Car-Simulator-Open-City-Stunts` (прошёл предпроверку и ручную сертификацию),
Project Zombik — `D:\UnityProjects\ExtremeGames_TestTask_Unity_6000\ZombieTask_U6`.

## Предпроверка McPlay (должно быть зелёным)
- SDK YouTube (`ytgame`) загружается первым; тег SDK содержит только `src`; `window.ytgame` не переопределяется.
- Нет внешних скриптов, ресурсов и ссылок; только относительные пути.
- Не используется Page Visibility API (пауза — через SDK).
- Нет блокировки ориентации — игра должна жить и в портрете, и в альбоме.
- Каждый файл < 15 МБ (жёсткий лимит 30 MiB).
- Ассеты без сжатия — или gzip/brotli с распаковщиком в загрузчике Unity (`decompressionFallback`).
- `devicePixelRatio` ограничен; ошибки Unity видны; колбэки SDK переживают ранний вызов; `firstFrameReady` вызывается со страницы.

## Что требуется в карточке игры
- Обложки 5:7 (≥270×378, лучше 540×756), 1:1 (≥512), 16:9 (≥1280×720).
- Возрастной рейтинг, жанр, описание, changelog.
- Авторы музыки (название, автор, лицензия).
- **Описание Rewarded и Interstitial** — без них игру не принимают. Rewarded — только по кнопке игрока; Interstitial — на стыках (смена сцены, рестарт), не во время заезда.

## Как устроена интеграция (Assets/_Web)
| Файл | Что делает |
|---|---|
| `Playables.cs` + `Plugins/WebGL/Playables.jslib` | Разговор с `ytgame` напрямую (без Playgama Bridge — он слушает visibilitychange, что запрещено): gameReady, пауза/звук платформы, Interstitial/Rewarded с «билетами» |
| `CloudSave.cs` + `Plugins/WebGL/CloudSave.jslib` | Облачное сохранение: снимок известных ключей PlayerPrefs (деньги, машины, уровни, награды, цвета, настройки) → `ytgame.game.saveData`; на старте `loadData`, применяется если ревизия облака новее |
| `WebHost.cs` | Живёт на объекте `Playables` (создаётся до первой сцены): пауза платформы и реклама «держат» игру (timeScale 0 + AudioListener.pause), мут платформы главнее, gameReady при первом показе гаража, Interstitial на стыках сцен, пуш сохранения каждые 5 с / на смене сцены / на паузе, прячет кнопки Exit |
| `AdService.cs` | Единая точка рекламы. Interstitial не чаще раза в 60 с и не в первую минуту (только что был pre-roll). Rewarded — только по кнопке |
| `DoublePrizeOffer.cs` | Rewarded: кнопка «AD: x2 +N» на экране финиша (копия Restart), повторно начисляет приз только если реклама досмотрена |
| `ScreenCompat.cs` | `Screen.SetResolution` не вызывается в браузере |
| `WebGLTemplates/YouTube/index.html` | SDK первым тегом, firstFrameReady со страницы, остановка цикла движка на паузе, devicePixelRatio = 1, ошибки видны на экране загрузки |
| `Editor/WebBuild.cs` | Меню **Build → YouTube Playables (WebGL)** и batch `WebBuild.YouTube`: сначала `WebShrink.ApplyAll`, потом Brotli + распаковщик в загрузчике, DXT, без заставки Unity, отчёт по размерам файлов, zip `Builds/MicroCar-YouTube.zip` |
| `Editor/WebShrink.cs` | Вес веб-сборки: отчёт `Builds/shrink-report.txt` и WebGL-переопределения импорта (см. ниже) |
| `Tools/serve-webgl.js` | Локальная проверка: `node Tools/serve-webgl.js` → http://127.0.0.1:8099/ (файлы отдаются как есть — распаковывает сам загрузчик, как на McPlay) |

## Как собрать
Редактор должен быть закрыт (batchmode не откроет проект, который уже открыт):
```
"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe" -batchmode -nographics -projectPath D:\UnityProjects\MicroCarReady -buildTarget WebGL -executeMethod WebBuild.YouTube -logFile Builds\build-yt.log
```
Результат: `Builds/WebGL-YT/` (сайт), `Builds/MicroCar-YouTube.zip` (загружать в McPlay), отчёт `Builds/last-build-yt.txt`
(первая строка `result=…`, затем размеры файлов с пометками `over15MB` / `OVER30MiB`). Повторная сборка — около 5 минут.

## Размер сборки
Лимит McPlay — каждый файл < 30 MiB (желательно < 15 МБ). Unity кладёт все сцены в один файл `WebGL-YT.data.unityweb`.

| Сборка | `.data` (Brotli) | Что изменилось |
|---|---|---|
| Первая, 10.11.2026 | 157 MiB | — (316 МБ ассетов, 86 % — текстуры) |
| Вторая | 31.4 MiB | `WebShrink.ApplyAll` (ниже) |
| Третья | 30.0 MiB | + crunch для неба (4 → 0.8 МБ); Low-кодировка lightmaps размер не изменила — откачена |
| Четвёртая (текущая) | **28.9 MiB** | + без заставки Unity (логотип 2.7 МБ), интерфейс 512, crunch с обычным DXT вместо BC7, музыка 25 % |

Что делает `WebShrink` — **только WebGL-переопределения**, Android/iOS не трогаются (кроме сжатия мешей):
- Camera Filter Pack: из `Resources` (всё оттуда попадает в сборку — 294 шейдера и 43 текстуры, 75 МБ) убрано всё,
  кроме `CameraFilterPack_TV_ARCADE*` (единственный фильтр в сценах). Остальное лежит в `Assets/Camera Filter Pack/NotInBuild`;
  чтобы включить другой фильтр — вернуть его шейдер и текстуры в `Resources`.
- Текстуры (WebGL): максимум 512 (интерфейс тоже), карты нормалей 256, небо 1024, lightmaps 512; crunch (качество 50)
  везде, кроме нормалей и lightmaps. Crunch работает только с обычным DXT — у текстур с HighQuality (BC7) формат для веба
  переводится в обычное сжатие, иначе флаг crunch молча игнорируется.
- Спрайты со сторонами не кратными 4 на WebGL не сжимаются в DXT и идут как RGBA32 — поэтому интерфейс урезан до 512.
- Звук (WebGL): музыка AAC 25 %, эффекты 60 %.
- В `WebBuild` на время сборки выключается заставка Unity (`show` и `showUnityLogo`), после — возвращается.
- Модели: сжатие мешей Medium (глобально; Read/Write не трогается — повреждения машин правят вершины).
- Если `.data` снова подберётся к 30 MiB — следующий шаг, как в Car Simulator: Addressables (гараж и трасса отдельными бандлами).

## Как проверять в браузере
- `node Tools/serve-webgl.js` → http://127.0.0.1:8099/. Окно браузера должно быть **на переднем плане**: Chromium
  притормаживает фоновые/перекрытые окна до ~1 кадра в секунду, и игра выглядит «зависшей» (картинка не обновляется,
  клики будто не работают). Встроенная панель браузера Claude тоже под это попадает.
- `?debug=input` в адресе включает `Assets/_Web/InputProbe.cs`: каждое нажатие пишет в консоль, во что попал луч UI и почему
  кнопка его не приняла. Без параметра ничего не делает.
- В режиме эмуляции экрана инструменты браузера могут пересчитывать координаты клика иначе, чем рисуется страница —
  сверяйтесь с логом InputProbe.

## Описание рекламы для карточки McPlay (черновик)
- **Rewarded:** only from a button the player presses. On the race finish screen, after a prize is won, the player can watch an ad to receive the same prize again ("AD: x2"). The prize is added only when the SDK reports the ad was watched.
- **Interstitial:** only at scene changes, never during a race: when the player starts a race from the garage, leaves a race back to the garage, or restarts a race. Not more often than once per 60 seconds and not in the first minute after the pre-roll.

## Статус
- 10.11.2026 — проект получен, разобран, документация заведена.
- 10.11.2026 — свет перезапечён, небо сжато, веб-слой YouTube перенесён из Car Simulator, браузерные правки шаблона. Платформа переключается на WebGL; первая сборка — следующий шаг.
- 10.11.2026 — WebGL-сборка проходит лимит McPlay: `.data` 28.9 MiB (< 30 MiB), всего 34.3 MiB (было 157 MiB одним файлом).
  Локально (`Tools/serve-webgl.js`): загрузчик распаковывает Brotli сам, `ytgame` подгружается, гараж открывается, ошибок в консоли нет.
  Исправлено: `WebHost.OnHostPause/OnHostAudio` совпадали по имени с сообщениями страницы → `MissingMethodException` в браузере.
- 10.11.2026 — проход гараж → выбор уровня → машина → заезд → финиш → «AD: x2» → выход в гараж (Play Mode в редакторе, кнопки нажимались
  через их onClick, финиш — `Race_Manager.Finish_Race()`): работает, ошибок нет. Исправлено по ходу:
  - кнопка «AD: x2» закрывала кнопку Exit на экране финиша (выйти в гараж было нельзя) — теперь стоит над Restart;
  - `LevelSelect.Start` падал с NullReferenceException на пустых слотах уровней (в шаблоне слотов больше, чем уровней);
    на WebGL такие исключения не ловятся (ExplicitlyThrownExceptionsOnly) — добавлены проверки.
  В браузере (сборка + `Tools/serve-webgl.js`): загрузка, гараж, настройки, открытие выбора уровня — работают; консоль чистая.
  До исправления `LevelSelect` браузерная сборка **падала** при открытии выбора уровня (`RuntimeError: null function`,
  `Halting program`) — так на WebGL выглядит NullReferenceException при ExplicitlyThrownExceptionsOnly.
- 10.11.2026 — стартовый счёт нового игрока: 123 000 → **1 400** (`Garage.unity`, `MainUtility.startingScore`; было тестовое значение). Уже сыгравшие сохраняют свой счёт.
  Не проверено: заезд в браузере и на телефоне (тач), реклама и облачные сохранения внутри YouTube (только в McPlay/YouTube).

## Что ещё не сделано
- `.data` 28.9 MiB — запас до 30 MiB около 1 МБ, а желательно < 15 МБ: новый контент сначала через `WebShrink`, при росте — Addressables.
- Портретный режим UI (сейчас интерфейс только альбомный; McPlay проверяет отсутствие блокировки ориентации, YouTube смотрит, как игра выглядит в портрете).
- Обложки 5:7 / 1:1 / 16:9 и нормальная картинка загрузки (`WebGLTemplates/YouTube/loading.jpg` сейчас — кадр из сцены).
- Авторы музыки для карточки.
- Проверка тач-управления на телефоне.
