# UI / HUD — Art Briefs (DECK//ROGUE)

**Роль:** UI/HUD художник · **Статус:** v0.1 lock для M5  
**Канон:** [`19_ART_DIRECTION_JRPG.md`](../19_ART_DIRECTION_JRPG.md) · [`DECK_ROGUE_MASTER.md`](../DECK_ROGUE_MASTER.md) §11 / §12 / Android  
**Связано:** MVP [`14_MVP.md`](../14_MVP.md) · **AD lock:** пульт = JRPG-menu clarity, сцена = storytelling plate

---

## 1. North star

**Визуальный тезис:** *JRPG-premium pixel* — плотная, «дорогая» 2D-графика в духе лучших пиксельных RPG-меню (чёткие рамки, тактовая сетка, статусные полосы, микро-анимация), **перенесённая на диджейский пульт**, а не на колодный roguelike.

| Делаем | Не делаем |
|---|---|
| Встроенные панели пульта, как физический дэск | Плавающие «карточки» поверх микса |
| Один слой обратной связи ≤2 с, без блокировки фейдера | Toast-спам, stacked modal, full-screen VFX |
| Иконка + подпись + форма (A холодная / B тёплая) | Только цвет для состояния |
| Сцена клуба — окно вверху, пульт — рабочая зона | Лор-тексты поверх crossfader |
| Touch 48dp на P0; декор не уменьшает hitbox | Декоративные пиксели как «кнопка» |

**Приоритет чтения (сверху вниз):** цель зала / Hype → активная дека → бит / фраза → пластинка → crossfader & bass kill → вторично (Combo, reputation, wave).

---

## 2. JRPG-premium pixel — язык формы

### 2.1 Рамки и панели

- **Базовая панель:** `#2A2740` fill, 1px outer `#3A4260`, inner highlight `#1A1830` (нижний/правый bevel JRPG-style).
- **Уголки:** 2×2 px «ступеньки» по сетке 8px; на Performance **без** золотой обводки на каждом элементе — золото/неон только у **primary CTA** и milestone (Victory/ Boss briefing).
- **Разделители:** 1px `#3A4260` + 4px «вкладка» с подписью секции (как заголовок окна в RPG).
- **Глубина:** максимум 2 уровня (панель → inset control well). Третий уровень — только для modal Briefing/Reward/Result.

### 2.2 Типографика

- Пиксельный шрифт с **кириллицей**, лицензия на распространение.
- **Primary** `#F7F4EA` на `#2A2740`; secondary `#A8ADBC`.
- Минимальный body на phone (после UI scale 1.0): **11–12 px** эквивалент; заголовки экранов 14–16 px.
- Моноширинный ряд для beat/phrase (`BAR 3/4`) — optional, но один стиль на весь HUD.
- Числа Hype/Combo — tabular figures если шрифт поддерживает.

### 2.3 Движение

- Кнопки: сдвиг **1–2 px** вниз при press, без scale-blur.
- Полосы Hype/таймер: stepped или smooth fill — **один** стиль в билде; fill привязан к game state, не к FPS.
- Feedback (`CLEAN MIX +8`): появление слева или над **Feedback rail** (см. §5), fade 1.5–2 с.
- Reduced flashing: нет строба, дроп = один такт мягкого additive glow на сцене, не пульс всего Canvas.

### 2.4 Сцена vs UI

- Клубная сцена `480×270` ref — **Pixel Perfect Camera**.
- HUD — native resolution, crisp; не апскейлить пиксельный UI дробными множителями.
- Неон-glow — отдельный слой поверх сцены; **не** поверх текста кнопок Play/Bass.

---

## 3. Палитра и семантика (v0.2 · AD lock)

| Токен | HEX | UI-использование |
|---|---|---|
| `night-void` | `#1A1830` | Letterbox, modal затемнение |
| `panel-dark` | `#2A2740` | Пульты, inset wells |
| `deck-a` | `#6EB6FF` | Рамка/вкладка A + label **A** + угловая форма |
| `deck-b` | `#FFB07A` | Рамка/вкладка B + label **B** + иная форма |
| `hype-pos` | `#5ED3B1` | Рост Hype, success tick + иконка |
| `warning` | `#F2CF6E` | Low time, pending queue + иконка |
| `error` | `#E76F80` | BASS CLASH, fail — **+** иконка/текст |
| `text-primary` | `#F7F4EA` | Подписи, titles |
| `text-shadow` | `#0E0C18` | 1px обводка читаемости |

Клуб MVP может сдвигать backdrop и accent ±15% hue; **Deck A/B и семантика Hype/error не меняются** между клубами.

---

## 4. Экран Performance — компоновка

### 4.1 Desktop 16:9

```text
┌─────────────────────────────────────────────────────────────────┐
│ [Club strip  ~38% h]     TIMER │ GOAL REQ │ HYPE BAR │ COMBO     │
│        pixel crowd + lights (no text over center dancefloor)    │
├─────────────────────────────────────────────────────────────────┤
│ DECK A panel          │ BEAT RAIL (4/4)           │ DECK B panel  │
│ badge A · track       │ phrase marker · queue     │ badge B       │
│ disc · PLAY · BASS    │                           │ disc · PLAY   │
│ FILTER (drag)         │ FEEDBACK RAIL (3 events)    │ FILTER      │
├───────────────────────┴───────────────────────────┴───────────────┤
│          CROSSFADER (full width, thumb ≥48dp touch)             │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Android landscape (touch-first)

- Клуб — **узкая полоса** 18–22% высоты (crowd silhouette + Hype bar), не сжимать P0.
- Deck A | Deck B — **две колонки** 44% / 44%, gutter 4% под beat rail.
- Crossfader — **нижняя 14–16%** высоты, thumb track min height **56 dp**.
- Waveform / лишние метаданные — за `DETAILS` (toggle 48×48), не за счёт Play.

### 4.3 Z-order и «анти-карточный» закон

1. **Performance:** нет modal, нет draggable cards над фейдером.
2. **Пластинки:** в панели деки — **inset slot** (JRPG inventory cell), не freestanding card sprite.
3. **Upgrade между сетами:** только экран Reward (§8), не popup на Performance.
4. Одновременно на экране ≤ **1** крупный баннер + **3** строки feedback rail.

---

## 5. Компоненты пульта

### 5.1 Deck panel

**Структура (сверху вниз):**

| Зона | Содержимое | Визуал |
|---|---|---|
| Header | `DECK A` / `DECK B` + state | Вкладка с цветом деки; state: `PLAYING` / `QUEUED` / `PAUSED` — разная **иконка** (▶ / ⏳ / ‖) |
| Track row | Short name, BPM tag | Ellipsis; полное имя в DETAILS |
| Disc well | 32×32+ sprite, rotation tied to phase | Inset `#1A1E2E`; idle shimmer 1px |
| P0 row | PLAY / queue · BASS KILL | Две кнопки **равной ширины**, min **48×48 dp** |
| Filter | Horizontal strip or 1D slider | Label `FILTER`; не единственный способ на Android |

**A vs B без цвета:** A — скошенный левый угол панели + синяя LED-точка; B — зеркально + оранжевая точка.

### 5.2 Crossfader

- Track: recessed channel `#1A1E2E`, метки `A` — `B` на концах (не только градиент).
- Thumb: 24×32 px sprite + **invisible hitbox** min 48×56 dp (шире визуала).
- Position indicator: numeric optional (P1); MVP — tick marks каждые 25%.
- **Multi-touch:** thumb drag не глотает второй палец на Bass Kill (separate raycast targets).
- Active mix hint: subtle glow с стороны доминирующей деки (1px LED), не fullscreen tint.

### 5.3 Beat rail (4/4)

- Центральная полоса между деками; высота 24–32 px (desktop), 28–36 touch.
- 4 деления; **beat 1** — высота/яркость ×1.5.
- **Upcoming phrase:** маркер «след. такт» за 1 bar (warning color), без мигающего строба.
- Queued actions: иконки над соответствующей долей (queue to B, bass off).

### 5.4 Hype / Combo / Goal

**Hype bar (JRPG HP-style):**

- Позиция: top strip, **левая 40%** после club timer; height 12–16 px + label `HYPE`.
- Fill `hype-pos`; threshold line для цели клуба (dashed 1px).
- Числовое значение справа от бара; анимация fill — stepped по phrase (предсказуемо).

**Combo:**

- Правее Hype; **не** full-screen combo counter.
- Формат: `COMBO ×N` в рамке 48×24 min; при N=0 скрыть или `—`.
- Burst: +1 scale 1px на такт, max 2 такта.

**Goal / requirement:**

- Одна строка `Зал хочет: …` под Hype или в club strip; icon + text.
- Reputation (run): P1 chip рядом с timer, не конкурирует с Hype.

### 5.5 Feedback rail

- Под beat rail: **последние 3** события, стек снизу вверх.
- Success: `CLEAN MIX +8` — `hype-pos` text; error: `BASS CLASH -6` — `error` + ⚠ glyph.
- Max width 40% центра; **never** перекрывает crossfader thumb path.
- Новое событие — slide-in 4px, без shake camera.

### 5.6 Club strip

- Только визуал + macro UI (timer, hype). **Без** paragraph lore.
- Promoter line — ticker **одной** строки, 8–12 с, затем fade; повтор по cooldown, не loop каждые 2 с.

---

## 6. Touch & hit targets (48 dp)

| Элемент | Min touch (dp) | Примечание |
|---|---|---|
| PLAY / queue | 48 × 48 | Весь inset row P0 |
| BASS KILL | 48 × 48 | Toggle; pressed state inset |
| Crossfader thumb | 48 × 56 | Hitbox > sprite |
| Filter drag area | 48 × 40 | Full strip height |
| DETAILS toggle | 48 × 48 | Secondary info only |
| Modal choices (Reward) | 48 × 64 | Full row per option |
| Settings sliders | 48 touch height | Thumb extended |

**Safe area:** P0 вне notch и gesture bar; отступ 8 dp min от краёв.

**PC:** те же визуалы; hover tooltip на FILTER/FX (Android не требует).

---

## 7. Flow-экраны (modal layer)

Общие правила modal:

- Фон: `night-backdrop` 85% + **blur optional P1**; пульт на Performance **не виден** как интерактив.
- Одна **primary** кнопка внизу (JRPG «command window»).
- Иллюстрация клуба — **статичный** 240×135 или full-width banner **above** text block, not behind buttons.

### 7.1 Briefing

```
┌─────────────────────────────────────┐
│  [Club banner / exterior sprite]    │
│  NEON BASEMENT                      │
│  ─────────────────────────────────  │
│  Цель: довести Hype до 70 за 120 с  │
│  Зал: любит плавные переходы        │
│  Старт: Track A «…» · Track B «…»   │
│  [ icon row: 2 discs, no card deck ]│
│                                     │
│              [ НА СЦЕНУ ]           │
└─────────────────────────────────────┘
```

- Стартовые треки — **два inset cells**, не hand of cards.
- Secondary: `Как играть` → How To overlay (skippable).

### 7.2 Reward (между сетами)

```
┌─────────────────────────────────────┐
│  ВЫБЕРИ ОДНО УЛУЧШЕНИЕ              │
│  ┌──────────────┐ ┌──────────────┐  │
│  │ Option A     │ │ Option B     │  │
│  │ icon + 2line │ │ icon + 2line │  │
│  └──────────────┘ └──────────────┘  │
│  (ровно 2; tap = commit, no deck)   │
└─────────────────────────────────────┘
```

- Две **равные** JRPG choice boxes; selected = 1px `hype-pos` border + 1px depress.
- **Запрещено:** fan of cards, scrollable gallery, rarity sparkles на весь экран.

### 7.3 Results

```
┌─────────────────────────────────────┐
│  VICTORY / SET FAILED               │
│  [mini Hype trace sparkline]        │
│  Очки · причина · 1 lesson line     │
│  [ В МЕНЮ ]    [ Повторить P1 ]      │
└─────────────────────────────────────┘
```

- Sparkline — 120×32 px max; не синхронная «карта забега».
- Failed: объяснение текстом (`Hype не дотянул`, `Bass clash`), не только red flash.

### 7.4 Main Menu / Settings

- Menu: вертикальный command list (JRPG), не tile grid из 12 иконок.
- Settings: Music / SFX, UI scale, reduced flashing, captions, input note — **scroll** one column.

---

## 8. Состояния и доступность

| Состояние | Визуал |
|---|---|
| Playing | LED on, disc spin, header bold |
| Queued | ⏳ + pulse LED 1px/phrase |
| Paused | ‖ dim 60%, timer frozen icon |
| Bass kill active | `KILL` inset pressed, strikethrough on LOW icon |
| Low FX mode | crowd 2-frame max, glow off, feedback text only |

- **Grayscale test:** A/B, Hype threshold, error отличимы без hue.
- **UI scale:** 0.85–1.25; при 0.85 не ломать 48dp — увеличивать hitbox, не shrink text below min.
- Captions: дублировать promoter line + major feedback strings.

---

## 9. MVP asset list (UI)

| Asset | Spec | Naming |
|---|---|---|
| Panel 9-slice | 8px grid corners | `UI_Panel_Standard_9.png` |
| Button primary | 3 states | `UI_Btn_Primary_{Normal,Pressed,Disabled}.png` |
| Deck badge A/B | 16×16 LED | `UI_DeckA_Badge.png` |
| Crossfader track + thumb |  | `UI_Crossfader_Track.png`, `UI_Crossfader_Thumb.png` |
| Hype bar fill | 1×8 repeatable | `UI_Hype_Fill.png` |
| Icons P0 | 16×16 set | `UI_Icon_{Play,Queue,BassKill,Filter,Warning}.png` |
| Modal frame | taller 9-slice | `UI_Panel_Modal_9.png` |
| Feedback font atlas | Cyrillic | licensed TTF → SDF |

Export: PNG alpha, Point filter, no EXIF.

---

## 10. QA checklist (UI art)

- [ ] 90 с tutorial: видны играющая дека, queue B, bass kill, fader, Hype — **без** чтения лора.
- [ ] Feedback ≤2 с, crossfader всегда доступен.
- [ ] Android 20:9: 2-finger fader + bass smoke layout.
- [ ] Нет stacked cards / popups on Performance.
- [ ] Reduced flashing: drop без full-screen pulse.
- [ ] Grayscale screenshot pass.
- [ ] Track name readable на 5.5" landscape @ scale 1.0.

---

## 11. Open for ADR / engineering

- **UI Toolkit vs uGUI:** визуальный язык не зависит; префабы `desktop` / `touch` с shared sprites.
- **Filter control:** vertical knob vs strip — strip preferred for 48dp; knob P1 desktop-only OK if touch has strip parity.
- **Waveform:** decorative P1; не обязателен MVP if beat rail + phrase clear.

---

*Версия: 2026-11-23 · Следующий артефакт: wireframe mock `480×270` club + `1920×1080` Performance desktop ref.*