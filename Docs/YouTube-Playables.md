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

## План интеграции MicroCar (перенос из Car Simulator)
1. Шаблон `Assets/WebGLTemplates/YouTube` и `Assets/Plugins/WebGL/Playables.jslib`.
2. Слой `Assets/_Web`: `Playables`, `PlatformService`, `AdService`, `Prefs` (облачные сохранения SDK вместо PlayerPrefs), `SoundSettings`, `NoExitButton`, `PortraitFit`, `BrowserVisibility`.
3. Заменить `UnityAdsScript` (AdMob) на `AdService`: Interstitial — при выходе из гонки в гараж и при рестарте; Rewarded — удвоение награды на финише (+ разблокировка машин).
4. Убрать `Application.Quit`, `Screen.SetResolution`, чит-клавиши; Esc только пауза.
5. Тач-управление в мобильном браузере (сейчас на WebGL принудительно клавиатура).
6. Портретный режим UI.
7. Размер: перезапечь свет ([Lighting.md](Lighting.md)), сжать текстуры (DXT для веба), при необходимости Addressables, чтобы каждый файл был < 15–30 МБ.
8. Сборка — `Editor/WebBuild.cs` → `WebBuild.YouTube` (batchmode), результат в `Builds/WebGL-YT`.

## Статус
- 10.11.2026 — проект получен, разобран, документация заведена. Интеграция не начата.
