# Google Play и iPhone

Выпускаем после YouTube. Что сейчас в Player Settings и что нужно поменять:

| Параметр | Сейчас | Нужно |
|---|---|---|
| Company / Product name | SmartMove / OffroadRacing | Наши |
| Android bundle id | `com.ALIyerEdon.MudRacer` (от шаблона) | Свой, например `com.<студия>.microcar` |
| iOS bundle id | не задан | Свой |
| Версия | bundleVersion 3, versionCode 3 | По нашей схеме |
| Android min / target SDK | 25 / 35 | Target — актуальный требуемый Google Play |
| Архитектуры Android | ARMv7 + ARM64, IL2CPP | ARM64 обязателен |
| Keystore | не задан | Создать и хранить вне репозитория |
| iOS target | 15.0, Team ID пуст | Указать Team ID, IL2CPP |
| AdMob App ID | Android есть, iOS пуст | Завести iOS App ID, реальные ad unit ID (сейчас тестовые) |

Также:
- Модули Android Build Support и iOS Build Support для 6000.3.9f1 сейчас **не установлены** (есть только Web).
- Согласие на рекламу (UMP / GDPR) и ATT на iOS.
- Политика конфиденциальности — ссылка в сторе.
- Интеграцию рекламы держать за общим `AdService`, чтобы веб и мобилки использовали один интерфейс.
