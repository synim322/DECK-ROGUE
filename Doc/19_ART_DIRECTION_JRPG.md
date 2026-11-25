# 19. Art Direction — JRPG-inspired Premium Pixel

**Владелец:** Art Director · **Статус:** канон направления · **Дата:** 2026-11-23  
**Связано:** [мастер-док §12](DECK_ROGUE_MASTER.md#section-13), [Content Bible §05](DECK_ROGUE_MASTER.md#section-06), [UI/UX §11](DECK_ROGUE_MASTER.md#section-12)

> Этот файл — **art north star**. При конфликте с устаревшими формулировками «neon pixel club only» побеждает этот документ. Код Unity и аудиограф не переписываем ради стиля; меняем арт-язык, палитры, композицию кадра и briefs.

---

## North Star

**JRPG-inspired premium pixel** — ночной клубный рогалик, который выглядит как storytelling-кадр из Octopath Traveler / Sea of Stars / классического JRPG: тёплый объём через свет и глубину слоёв, читаемые силуэты, атмосферный задник — но **пульт DJ остаётся инструментом**, не декорацией.

Мы **не** делаем 3D HD, HD-рендер, faux-3D tilt-shift камеру или фотореализм. Остаёмся в **2D / pixel**, но поднимаем «красоту кадра» до JRPG-premium: ограниченная богатая палитра, живописный свет, ясные планы, персонажи с характером силуэта.

### Одна фраза для команды

> Зал сверху — как vignette ночи из JRPG. Пульт снизу — как инвентарь/меню боя: крупно, подписано, без легенды.

---

## Что сохраняем / что меняем

### Сохраняем (из текущего пикселя)

| Элемент | Почему |
|---|---|
| 2D pixel, Point/Nearest, целые координаты | Технический и читаемый фундамент |
| Зал сверху / пульт снизу | Иерархия кадра и touch/desktop UX |
| 3 состояния толпы (low / mid / high) | Геймплейный feedback без текста |
| A/B деки: холодный vs тёплый + **подпись + форма** | Цвет не единственный канал |
| Reduced flashing полностью гасит стробы/резкие вспышки | Accessibility hard rule |
| Короткий лор (промоутер / плакат), не абзац поверх фейдеров | Пульт свободен |
| Ограниченная палитра **на клуб** | Отличимость площадок |
| MVP-бюджет ассетов (1 клуб → расширение Act1) | Производимость |

### Меняем (Art Direction Shift)

| Было (neon underground default) | Становится (JRPG premium pixel) |
|---|---|
| Плоский неон на тёмном `#151628` | Живописная глубина: ambient fill + rim + accent lamp; ночь «дышит» |
| Силуэты толпы как пятна | Силуэты с **читаемым pose language** + аксессуар/роль (как NPC в JRPG town night) |
| Свет = аддитивный строб/неон | Свет = **mood volume** (конусы, окна, сцена, отражения на пульте); строб — опция, не стиль |
| UI и сцена в одном «клубном» языке | Сцена — живопись; **пульт — UI-инструмент** с чёткими хитбоксами и подписями |
| Клубы отличаются в основном hue неона | Клубы отличаются **композицией зала, архитектурой, температурой света, силуэтом толпы** |
| Декор конкурирует с контролами | Декор только в верхней половине; нижняя — зона работы |

### Явно out of scope

- 3D meshes / HD sprites / parallax-камера как в AAA JRPG-портах.
- Полноэкранные катсцены вместо briefing.
- Случайный AI-шум вместо читаемого силуэта.
- Эпилептический неон как «вау».

---

## Референсный кадр (словами)

**Reference canvas:** `480×270` logical px (16:9), сетка 8/16.  
**Первая половина (верх ~45%):** storytelling club plate — архитектура, толпа, сцена, мягкий свет, плакат/промоутер-бейдж максимум одной короткой репликой.  
**Нижняя половина (~55%):** BoothHud — деки, бит/фраза, пластинка, кроссфейдер, bass kill; воздух вокруг контролов важнее орнамента.

**Приоритет взгляда (сверху вниз):** задача зала + Hype → активная дека → бит/фраза → пластинка → кроссфейдер / bass kill.

---

## Перспектива и планы сцены

JRPG night-town язык без настоящей 3D-камеры:

1. **Far BG** — архитектура/небо/вывески, низкая деталь, 4–8 цветов.
2. **Mid crowd** — полоса танцпола; плотность силуэтов = Hype.
3. **Near stage / DJ booth edge** — подиум, колонки, герой-DJ как якорь (не перекрывает фейдеры).
4. **HUD / Instrument** — отдельный визуальный регистр: крупнее пиксель UI, чёткие рамки, высокий контраст текста.

Перспектива: **лёгкий ¾ / elevated stage view** (как камера над танцполом в JRPG inn/tavern night), не top-down, не чистый side-scroller. Горизонт стабилен; камера не трясётся на дропе (допустим 1 px nudge только если Reduced flashing off).

---

## Lighting mood

| Режим | Настроение | Техника |
|---|---|---|
| Default | Тёплый/холодный dual-key по клубу; мягкий rim на толпе | Отдельный lighting layer; 1–2 источника + ambient |
| Drop (flashing ON) | Короткий подъём яркости accent на 1 такт | Не full-screen white; локальный stage wash |
| Reduced flashing | Только статичный mood light + мягкий breathe ≤5% яркости | **Ноль** стробов, резких flashes, camera shake |

Свет продаёт JRPG-premium; **не** заменяет отсутствующий спрайт процедурной вспышкой на весь экран.

---

## Character silhouette language

- **DJ-протагонист:** узнаваемый профиль (наушники / куртка / стойка за пультом); читается в 32×48; один герой на MVP.
- **Промоутер:** JRPG-NPC постер или bust в PreClub — не в зоне фейдеров.
- **Толпа:** 3–5 архетипов; отличия **позой и аксессуаром**, не только tint.
- Три Hype-состояния = **плотность + амплитуда позы**, не только вспышка света.
- Целые пиксели; без blur «для сочности».

---

## Палитры

### Global UI / Booth (инструмент — стабильный)

| Назначение | HEX | Комментарий |
|---|---|---|
| Night void | `#1A1830` | Глубокий фон, не чистый чёрный |
| Panel wood/metal | `#2A2740` | Рамки пульта |
| Deck A (cold) | `#6EB6FF` | + label **A** + угловая форма |
| Deck B (warm) | `#FFB07A` | + label **B** + скруглённая/иная форма |
| Hype good | `#5ED3B1` | + иконка стрелки/пламени |
| Warning | `#F2CF6E` | + иконка |
| Error | `#E76F80` | + текст события, не только flash |
| Text primary | `#F7F4EA` | Кремовый JRPG-UI текст |
| Text shadow | `#0E0C18` | Обводка 1px для читаемости |

### Act1 club keys (сцена — разная)

| Клуб | Key ambient | Accent | Mood one-liner |
|---|---|---|---|
| Neon Basement (MVP) | `#2B2458` indigo | `#7CF0FF` ice neon | Малый подвал, камерный JRPG-inn night |
| Warehouse | `#24303A` steel | `#9BB7C9` fog cyan | Высокий объём, дым столбами |
| Bass Factory | `#3A2218` ember | `#FF6B3D` forge | Низкий потолок, жар low-end |
| Dream Lounge | `#2A3048` dusk | `#E8A0C8` / `#7FE0C8` soft | Мягкие огни, «lounge vignette» |
| Rave District | `#1E2040` night | `#FFE566` / `#5B8CFF` | Энергия без обязательного строба |
| **The Signal** (boss) | `#14182E` void | `#B8F0FF` signal beam | Пустой центр + луч/антенна; сюжетный пик |

Каждый клуб: **≤16 цветов** на plate (включая тени). Global UI палитра не ломается клубом — клубы тинтуют только сцену.

---

## Hero composition — Act1 + boss The Signal

### Обычный клуб Act1

```text
┌──────────────────────────────────────────────┐
│  [Club name · Timer]     [Hype · Combo · Rep]│  ← тонкая status strip
│                                              │
│     FAR ARCHITECTURE / POSTER (quiet lore)   │
│        CROWD BAND (silhouette density)       │  ← storytelling plate
│           STAGE · DJ silhouette              │
├──────────────────────────────────────────────┤
│  DECK A                 DECK B               │
│  track · beat · PLAY    track · beat · PLAY  │  ← instrument zone
│  BASS · FILTER · CROSSFADER · FILTER · BASS  │
└──────────────────────────────────────────────┘
```

Правила:

- HUD не перекрывает танцпол декоративными панелями.
- Одна короткая реплика промоутера — в PreClub или как плакат ≤2 строк в BG.
- Не класть stats/расписание/промо-чипы на hero media зала.

### Boss: The Signal

- Та же сетка **зал / пульт** — босс не ломает UX.
- Уникальность: **визуальный мотив сигнала** (луч, антенна, пустой круг в толпе, «приёмник» на сцене).
- Фазы босса читаются **силуэтом зала + подписью фазы**, не только сменой цвета.
- Reduced flashing: луч становится статичным glow / медленным breathe; никаких резких full-frame pulses.

---

## Accessibility (жёстко)

1. **Reduced flashing** — выключает стробы, резкие белые flashes, camera shake, агрессивный drop wash.
2. **Цвет ≠ единственный канал:** A/B подписи и формы; Hype — число + fill + иконка; события — текст (`CLEAN MIX`, `BASS CLASH`).
3. Проверка grayscale screenshot: сцена и пульт всё ещё понятны.
4. Android: важные цели ~48dp; ландшафт; safe area. Десктоп плотнее, те же правила иерархии.
5. Строка на экране ≠ id данных.

---

## Production notes

- Aseprite source → PNG export; naming `BG_`, `SPR_`, `UI_`, `VFX_`.
- Pixel Perfect Camera для сцены; UI может быть native-res, но визуально в том же JRPG-UI языке.
- Шрифты: пиксель + кириллица + лицензия; не тащить чужие файлы без прав.
- Палитры `.gpl` / `.ase` — в арт-репо после утверждения этого канона.

---

## Artist / Animator briefs (коротко)

Полные production-briefs (команда пишет параллельно): [`Doc/art/`](art/).

| Роль | Файл | One-liner |
|---|---|---|
| 1 · Environment | [`ENV_ACT1_BRIEFS.md`](art/ENV_ACT1_BRIEFS.md) | Act1 plates + The Signal; JRPG depth; deck-safe lower band |
| 2 · Crowd / Char | [`CHAR_CROWD_BRIEFS.md`](art/CHAR_CROWD_BRIEFS.md) | DJ + promoter + 3–5 archetypes × 3 Hype; pose > flash |
| 3 · Booth UI | [`UI_HUD_BRIEFS.md`](art/UI_HUD_BRIEFS.md) | Instrument chrome; A/B dual-code; 48dp; no card-soup |
| 4 · Lighting/VFX | [`LIGHTING_VFX_BRIEFS.md`](art/LIGHTING_VFX_BRIEFS.md) | Mood volume + Hype tiers; drop = 1 bar; RF profile |
| Animator | [`ANIMATION_BIBLE_ACT1.md`](art/ANIMATION_BIBLE_ACT1.md) | Beat-sync loops; discs; RF = static/≤5% breathe |

### Quick AD assigns

1. **ENV** — Neon Basement master plate → template → Warehouse / Dream Lounge → The Signal void+beam.  
2. **CHAR** — DJ 32×48, promoter PreClub bust, crowd archetypes grayscale-pass.  
3. **UI** — Booth panels, A/B forms+labels, crossfader/bass kill hitboxes, beat strip.  
4. **LIGHT** — Club keys + godrays-lite; no full-frame strobe; Signal beam RF-safe.  
5. **ANIM** — Crowd 2–3/3–4/4–6; vinyl phase; drop wash gated by Reduced flashing.

---

## Checklist утверждения кадра

- [ ] Верх = история клуба; низ = работа пульта.
- [ ] Три Hype-состояния отличимы в grayscale.
- [ ] A/B читаются без цвета.
- [ ] Reduced flashing проверен на Neon Basement и The Signal.
- [ ] Нет 3D HD / blur / AI noise.
- [ ] PreClub lore не переезжает на фейдеры.
