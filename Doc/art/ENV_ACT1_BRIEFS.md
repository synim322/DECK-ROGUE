# ENV — Act 1 Club Backgrounds (Art Brief)

**Проект:** DECK//ROGUE · **Владелец:** Environment / Art Director  
**Источник истины:** [`19_ART_DIRECTION_JRPG.md`](../19_ART_DIRECTION_JRPG.md) (north star) · [`DECK_ROGUE_MASTER.md`](../DECK_ROGUE_MASTER.md) §05 / §12  
**Статус:** v0.2 · **Дата:** 2026-11-23 · **AD lock:** JRPG-inspired premium pixel

---

## Scope Act 1

Канон маршрута: [`01_GDD.md`](../01_GDD.md) §3 · Music: [`16_MUSIC_ACT1.md`](../16_MUSIC_ACT1.md).

```
Shuffle(club_neon_basement, club_warehouse_pulse, club_bass_factory) → N=2 → club_the_signal
```

| Роль | `club_id` | Название | Тип |
|--------|-----------|----------|-----|
| Ordinary pool | `club_neon_basement` | Neon Basement | Обычный |
| Ordinary pool | `club_warehouse_pulse` | Warehouse (Mutabor-density) | Обычный |
| Ordinary pool | `club_bass_factory` | Bass Factory | Обычный |
| Boss (fixed) | `club_the_signal` | The Signal | **Boss** — пик Act 1 |

`club_dream_lounge` / `club_rave_district` — **Act2 packs** (арт можно набросать позже, не блокирует Act1).

---

## Визуальное направление (JRPG-pixel, не «голый неон»)

**Цель:** клуб читается как **сцена из JRPG** (Octopath Traveler, Sea of Stars) — атмосфера, материалы, глубина света — при сохранении **пиксельной сетки** и читаемого пульта.

| Делать | Не делать |
|--------|-----------|
| Тёплые тени + холодные или цветные **fill lights** (луна, неон, прожектор) | Плоский «весь кадр в RGB-неоне» |
| Dither / ограниченные градиенты для тумана, объёма, дальнего плана | Blur, не-pixel glow внутри спрайтов фона |
| Архитектура с силуэтом: балки, колонны, арки, лестницы | Бесконечный repeating tile без фокальной точки |
| 2–3 **материала** на клуб (кирпич, металл, штукатурка, дерево) | Одна текстура «шума» на всё |
| Неон — **акцент** (вывески, контур, лампы), не единственный источник света | Строб / белая вспышка на весь кадр (Reduced flashing) |
| Горизонт сцены: «подиум → зал → дальний план» с явной линией пола | Камера в потолок, закрывающая crowd band |

**Референсы (настроение, не копипаст):** Octopath — ночной город, контраст света и тени; Sea of Stars — уютная глубина, узнаваемая архитектура; Mutabor — плотность зала и industrial scale (только ощущение, не бренд).

**Связь с UI:** глобальные акценты Deck A `#6EB6FF`, Deck B `#FFB07A` — на сцене **не доминируют**; клубная палитра первична. Night void `#1A1830`.

---

## Canvas, экспорт, масштаб

| Параметр | Значение |
|----------|----------|
| **Reference canvas (художественный)** | **480×270** px, 16:9, все размеры **кратны 8** (допустимо 16 на крупных блоках) |
| **PPU (Unity)** | Единый для сцены, рекомендация **32** (1 logical px = 1 texel при camera zoom 1×) |
| **Фильтр** | Point / Nearest, без compression blur |
| **Master backdrop** | Один PNG на клуб: `BG_Club_<PascalName>.png` (полный кадр сцены); опционально **parallax slices** (см. ниже) |
| **VFX света** | Отдельные аддитивные слои `VFX_Club_<Name>_Light_##.png` |

> 480×270 — пространство **клубной сцены**, не весь экран Performance. Нижняя часть viewport на desktop отдаётся под пульт ([layout в мастер-доке](../DECK_ROGUE_MASTER.md)).

### Safe zones на canvas 480×270 (обязательно)

Нарисовать **направляющие** в Aseprite, не экспортировать в игру:

```text
y 0–28     HUD strip (название клуба, таймер, Hype) — без ярких пятен по центру
y 28–118   CROWD + STAGE band — главная композиция, DJ-силуэт, прожекторы
y 118–270  DECK SAFE — пульт (2 деки, crossfader, сетка 4/4, FEEDBACK)
```

**Правило:** в прямоугольнике **x:0–480, y:118–270** — только **низкий контраст**, фоновый пол/туман, **без** вертикальных колонн, неоновых вывесок, окон с высокой детализацией, персонажей. Боковые 48 px слева/справа — запас под safe area mobile.

---

## Система parallax (общая для Act 1)

Слои скроллятся по **phase / hype** или лёгкому idle drift (≤2 px amplitude). Скорости — доля ширины кадра за цикл, не subpixel-jitter.

| Layer ID | Имя | Scroll (×) | Содержимое | Контраст |
|--------|-----|------------|------------|----------|
| L0 | `FarSky` | 0.05 | Ночное небо, далёкие огни города, силуэты высоток **вне** клуба | Низкий |
| L1 | `BackWall` | 0.15 | Задняя стена, окна/вентиляция, крупные формы | Средний |
| L2 | `MidArchitecture` | 0.35 | Балки, трубы, бар, лестница, арка в зал | Средний–высокий |
| L3 | `StageProps` | 0.55 | Подиум (низкий), стойки света **за DJ**, декор сцены | Высокий |
| L4 | `Foreground` | 0.85 | Край рамы, ближние гирлянды, угол колонны — **только по краям**, не в центр crowd | Умеренный |
| — | `Crowd` | 1.0 | Отдельные `SPR_Crowd_*` (не в BG slice) | — |
| — | `DJ` | 1.0 | `SPR_DJ_*` на подиуме | — |
| — | `Lighting` | additive | Лучи, неон bloom, **Reduced-flash-safe** | — |

**Склейка:** при одном файле `BG_Club_*` — художник помечает границы слоёв для будущего split; для MVP допустим **единый L1–L3 bake** + отдельные L0/L4 при наличии времени.

**Именование slices:** `BG_Club_<Name>_L0_Far.png` … `L4_Fg.png`.

---

## Клуб 1 — `club_neon_basement` (Neon Basement)

**Fantasy:** первый подвал ночи — камерно, почти ламповое techno-house groove. Маленькая сцена, зал «впритык».

**Музыкальный вкус (арт):** спокойный pulse, не агрессивный industrial.

### Палитра (JRPG: холодный неон + тёплая штукатурка)

| Роль | HEX | Примечание |
|------|-----|------------|
| Night base | `#151628` | Совпадает с глобальным backdrop |
| Wall plaster (warm shadow) | `#2A2638` | Тёплый сдвиг в тенях |
| Wood / stage edge | `#3D3548` | |
| Neon primary | `#6B5CE7` | Фиолетовая вывеска |
| Neon secondary | `#62A4F4` | Синий контур (связь с Deck A, слабо) |
| Neon accent | `#B388FF` | Блик на трубах |
| Practical warm | `#E8A86B` | Лампа над баром |
| Floor bounce | `#1E2035` | |
| Haze | `#3A3560` @ 40% | Между L1–L2 |

### Ключевые props

- Низкий **подиум** (1–2 ступени), занавес/тяжёлая штора сбоку.
- **Неоновая вывеска** (абстрактное имя, не реальный бренд) на L1 — не свисает в deck safe.
- Кирпичная **полуколонна** слева, кабель-борд и **неоновые трубки** на L2.
- Мини-**бар** в глубине (бутылки силуэтами, 1–2 практических светильника).
- **Вент-труба** по потолку, ведущая взгляд к сцене.
- 2 источника света для code: `VFX` слабый violet wash + тёплый spot на DJ.

### Композиция

- Фокус: **центр–верх** crowd band; DJ чуть левее центра (место под правого зрителя на mobile).
- Глубина: тёмный зад + светлее середина (bar glow) + тень на полу подиума.

### Не рисовать

- Полноэкранный strobe, мигающие RGB-полосы.
- Крупные лица NPC в crowd band (только crowd sprites).
- Любые детали с **читаемым текстом** в HUD/deck safe.
- Закрытие **силуэта сцены** вывеской на уровне y &lt; 118 в центре 60% ширины.

**Asset:** `BG_Club_NeonBasement.png`, `VFX_Club_NeonBasement_Wash_01.png`

---

## Клуб 2 — `club_warehouse_pulse` (Warehouse)

**Fantasy:** большой склад под techno — длинные линии, дым, низкий **sub** в воздухе. Референс — **плотность и масштаб** Mutabor, не логотип.

### Палитра

| Роль | HEX | Примечание |
|------|-----|------------|
| Industrial night | `#12151A` | Почти чёрный с синим |
| Concrete | `#2C3238` | |
| Rust / patina | `#4A3F35` | JRPG warmth |
| Steel highlight | `#5A6570` | |
| Fog / haze | `#3D4A52` @ 55% | Между балками |
| Practical sodium | `#C9A227` | Редкие лампы |
| Strobe accent (off by default) | `#E8DCC8` | Только в VFX, не в base bake |
| Teal kicker | `#3D7A78` | Тонкий accent на PAR |

### Ключевые props

- **Фермы потолка**, diagonal на L1–L2.
- **Грузовые ворота** в глубине (свет из щели).
- **Дым** (dithered layers L1, анимация 2–3 кадра optional).
- Высокие **стеллажи** по бокам кадра (L4), не в центр.
- Подвесные **PAR / moving head** силуэтами на L3.
- Широкий **танцпол** — пятно света на полу (ellipse, y ≈ 105–115).

### Композиция

- Сильная **vanishing point** к центру сцены; масштаб больше, чем Basement — игрок чувствует «вторую площадку акта».
- Боковой свет, rim на балках — Octopath-style depth.

### Не рисовать

- Реалистичные бренды, флаги, постеры артистов.
- Яркая **жёлто-синяя rave-полоса** через весь кадр (это Rave District).
- Опоры/колонны, пересекающие deck safe по центру.
- Мелкий repeating brick **без вариации** (anti-tiling: 1–2 уникальных пятна плесени/ржавчины).

**Asset:** `BG_Club_WarehousePulse.png`, `VFX_Club_WarehousePulse_Haze_##.png`

---

## Клуб 3 — `club_bass_factory` (Bass Factory)

**Fantasy:** tech-house forge — мощный low-end, искра/жар, промышленный «пресс». Контраст после Warehouse: меньше тумана, больше **огня и металла**, читаемый риск баса.

### Палитра

| Роль | HEX | Примечание |
|------|-----|------------|
| Forge night | `#1A1210` | |
| Ember core | `#FF6B3D` | Identity клуба |
| Hot metal | `#C44A2A` | |
| Coal shadow | `#2A2220` | |
| Steel plate | `#5A5552` | |
| Sparks (VFX only) | `#F2CF6E` | Reduced flashing: статичные |
| Cool fill | `#3D5A78` | Чтобы не было «весь кадр красный» |

### Ключевые props

- **Пресс / котёл** силуэтом на L1–L2 (не детальный механизм).
- **Трубы и клапаны** по краям, не в deck safe.
- **Жар** снизу сцены (ellipse на полу y ≈ 105–115) — «sub в ногах».
- Стеллажи с **катушками / барабанами** по бокам L4.
- Подвесные **промышленные лампы** L3 (тёплый practical).

### Композиция

- Сильный **нижний акцент** жара vs холодный fill сверху — Octopath contrast.
- Шире «кузница», уже чем Warehouse vanishing; DJ на приподнятом подиуме.

### Не рисовать

- Лава / реальное пламя fullscreen.
- Кислотный rave-жёлтый через весь кадр.
- Busy warning signs с читаемым текстом.
- Колонны по центру deck safe.

**Asset:** `BG_Club_BassFactory.png`, `VFX_Club_BassFactory_Ember_##.png`

---

## Босс — `club_the_signal` (The Signal)

**Fantasy:** **сюжетный пик ночи Act 1** — не «ещё один клуб», а место, где город **слишком слышит** твой сет. Фазы босса (свет/настроение) меняются; фон — **одна база** + смена VFX/palette swap, не три полных перерисовки.

**Музыкальный вкус (арт):** фирменный саунд акта; визуал — собранный, знаковый.

### Палитра (база + фазы)

| Фаза / состояние | Доминанта | HEX якоря | Поведение арта |
|------------------|-----------|-----------|----------------|
| Base | Signal violet | `#5B4FD1` | Стабильный неон-«антенный» мотив |
| Phase A — Build | Electric cyan | `#4ECDC4` | Усиление L3 lights, тонкий scan line в VFX |
| Phase B — Peak | Hot coral | `#E76F80` | + rim на архитектуре; **без** full-screen flash |
| Resolve / win | Gold lift | `#F2CF6E` | Короткий warm wash 1 такт (toggle reduced) |

Глобальные `#151628` + `#F3F3EC` для ночи и читаемых лунных бликов.

### Ключевые props

- Центральный **broadcast motif**: радио-мачта / dish / «антенный» кольцевой знак на L1 (**абстрактный**, не корп. лого).
- **Платформа сцены** выше, чем в Basement — иерархия босса.
- Симметричные **light towers** L2 (сдержанная симметрия JRPG boss arena).
- Кабели, уходящие в **городской силуэт** L0 — связь с Act 1.
- Пол с **тонким геометрическим паттерном** (круг / ромб), dithered — только в crowd/stage band.
- Опционально **2–3 кадра** VFX «signal pulse» на beat (аддитив, не в bake).

### Композиция

- Сильная **ось симметрии**; DJ на подиуме — focal point между towers.
- Дальний план: намёк на **неоновый город** (общий с Basement, но крупнее).
- Ощущение **Finale Festival** урезано: нет панорамы на весь экран — всё ещё readable pixel scale.

### Не рисовать

- UI-полоски HP босса в фоне (это gameplay overlay).
- Отдельный «режим аркады» без пульта — сцена **не** занимает нижнюю половину.
- Текст сюжета на стенах (briefing / promoter).
- Copyright signage, реальные радио-станции.
- Full-screen white strobe на phase change.

**Asset:** `BG_Club_TheSignal.png`, `VFX_Club_TheSignal_PhaseA/B/Resolve_##.png`, palette swap JSON optional (P1).

---

## Shared checklist (приёмка фона)

- [ ] Canvas 480×270, кратность 8, Nearest filter.
- [ ] Deck safe y ≥ 118 соблюдён; grayscale screenshot читается.
- [ ] Parallax slices именованы или слои помечены в `.ase`.
- [ ] Неон ≠ единственный свет; есть тёплые/холодные тени (JRPG depth).
- [ ] Reduced flashing: база без мигающих элементов; строб только в отключаемом VFX.
- [ ] Имя файла по конвенции `BG_Club_*` / `VFX_Club_*`.
- [ ] Клуб отличим от других Act 1 в **thumbnail** и в grayscale.

---

## Deliverables Act 1 (ENV)

| Клуб | Backdrop | Parallax (min) | VFX |
|------|----------|----------------|-----|
| Neon Basement | 1 | L0+L2 или baked | 1 wash |
| Warehouse | 1 | L0+L1 haze | haze 2–3 frames |
| Bass Factory | 1 | L2 metal + ember | ember 2–3 frames |
| The Signal | 1 | L0 city + L3 towers | phase set 3 |

**Out of scope:** crowd sprites, DJ, instrument UI, pre-club static illustrations.

---

## Связь с engineering

- Слой **Instrument UI** и beat grid — отдельный canvas; арт сцены не перекрывает интерактив ([§12 checklist](../DECK_ROGUE_MASTER.md)).
- Android: сцена может сжаться в **узкую полосу**; композиция держится при crop **по центру crowd band**, не по полу в safe zone.

*Вопросы по палитре — синхрон с Art Director / `Night backdrop` в глобальной GPL.*