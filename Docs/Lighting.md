# Свет и тени

Артем просил перезапечь свет (сообщение от 10.11.2026): `Assets/Off_Road_Racing_3/Scene/Race_Track_1/LightingData.asset` весит **1.37 ГБ**, кубмап отражений — около 200 МБ.
Перезапекаем сами.

## Что выяснено
- Этот `LightingData.asset` использует сцена **Village** (единственная игровая сцена в сборке).
- Сами lightmap-текстуры в папке `Race_Track_1` маленькие (0.1–1.3 МБ каждая). Огромный размер `LightingData` даёт, по всей видимости, **Realtime GI (Enlighten)** — в логе редактора идут предупреждения Enlighten.

## План перезапечки
1. Lighting Settings Village: выключить Realtime Global Illumination, оставить Baked GI (Progressive GPU), Mixed Lighting — Shadowmask или Subtractive.
2. Lightmap Resolution / Max Lightmap Size — подобрать под бюджет веба (цель — все lightmaps суммарно до ~10–15 МБ, каждый файл < 15 МБ).
3. Компрессия lightmaps — Normal/Low quality; Directional Mode — Non-Directional (минус `_dir` текстуры).
4. Reflection Probes / skybox cubemap: разрешение 128–256, Baked.
5. Мелкие объекты (трава, камни, мелкий декор) — не Contribute GI, свет от Light Probes.
6. После запекания проверить размер `LightingData.asset` и визуал в Game view.

## Результат (10.11.2026)
Настройки (`Assets/Off_Road_Racing_3/Settings/Race_Lighting_Settings_Track3.lighting`):
Realtime GI — выкл., Baked GI — Progressive GPU, 4 texel/unit, атлас 1024, padding 4, сжатие Normal Quality,
Non-Directional, сэмплы 32/128/64, 2 отскока, Mixed — Subtractive. Запекание — 3.7 мин.

| | До | После |
|---|---|---|
| `LightingData.asset` | 1373 МБ (в git не попадал — > 100 МБ) | 0.09 МБ |
| Lightmaps | 483 × 512 + 483 dir-текстуры (≈ 375 МБ) | 30 × 1024, без dir |
| Папка данных | `Scene/Race_Track_1/` | `Scene/Village/` |

Дополнительно:
- 6 невидимых коллизионных оболочек `*_ConvexHulls` (материал `Opacity`, Cutout с порогом 1) больше не рендерятся и не запекаются — коллайдеры остались. Они занимали основную площадь lightmap.
- Небо: `New Cubemap 2.cubemap` (2048, RGBA32, 192 МБ, в git не попадал) заменён на `Skybox_Desert_Cube.png` (куб 1024, DXT1 ≈ 4 МБ), сверено визуально по 6 направлениям. Старый файл больше ни на что не ссылается.
- Если веб-сборка окажется тяжёлой — следующий шаг: 3 texel/unit или меньший масштаб lightmap у дальних домов/деревьев.
