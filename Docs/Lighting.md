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

## Результат
- (заполнить после перезапечки: размеры до/после, настройки)
