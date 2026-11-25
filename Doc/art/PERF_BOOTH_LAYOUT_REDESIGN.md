# Performance Booth — Layout Redesign (AD lock)

**Роль:** Art Director + UX · **Статус:** handoff Client Engineer · **Дата:** 2026-11-23  
**Канон:** [`UI_HUD_BRIEFS.md`](UI_HUD_BRIEFS.md) · [`19_ART_DIRECTION_JRPG.md`](../19_ART_DIRECTION_JRPG.md)  
**Код (якоря as-is):** `DJ/Assets/_Game/Audio/BoothHud.cs` — uGUI anchors; mix horizontal; FILTER full pad; crossfader center band.  
**Скрин-проблема:** status / reply / FILTER·PERC·MUSIC / crossfader / СТАРТ·СТОП — слишком длинные треки, тонкие thumbs, chrome ест высоту пульта.

> **Не трогать:** геймплейный C#, аудиограф, логику stem/filter. Только вёрстка anchors, размеры hitbox/thumb, иерархия chrome-текста, ориентация mix-слайдеров.

---

## 1. Visual brief — один adaptive layout

**Решение:** один layout-скелет для desktop 16:9 и Android landscape. Разница — **density tokens** (высоты chrome, высота клуба, max длина track), не два разных UI.

| | Desktop landscape @1920×1080 | Android landscape (touch-first) |
|---|---|---|
| Клуб / Stage | **36–40%** высоты safe area | **20–24%** (silhouette + mood; не зал-герой) |
| Chrome (status+cue) | **1 полоса** 36–40 px | **1 полоса** 44–48 px |
| Crate | 40–44 px | **48 px** min chips |
| Console / пульт | остаток ≥ **48%** | остаток ≥ **55%** |
| Mix controls | FILTER горизонтальный укороченный; PERC/MUSIC **вертикальные** | то же (touch-primary) |
| Crossfader | center band, track ≤ **22%** ширины экрана | track ≤ **26%** ширины; thumb hit **48×56** |
| Safe area | 8–12 px inset | ≥ **8 dp** от notch / gesture |

**Иерархия взгляда (не менять):** Hype + цель → активная дека → бит/фраза → пластинка → crossfader & BASS → secondary (crate, loop, reply).

**Тезис кадра:** зал — vignette сверху; пульт — JRPG command window снизу. Chrome не конкурирует с фейдерами. Нет карточек-оверлеев поверх микса.

---

## 2. Конкретные размеры @1080 ref

Референс: **высота safe = 1080 px** ≈ 1 px : 1 dp на phone scale 1.0. Ширина desktop 1920; phone 20:9 ≈ 2340×1080 — проценты по **ширине safe**, не по pad alone где отмечено.

### 2.1 Вертикальный стек (доля высоты safe)

| Зона | Desktop | Android | Примечание |
|---|---|---|---|
| Stage (клуб) | 0.52 → 1.00 (~48%) → **ужать до 0.58–1.00 (~42%)** | **0.76–1.00 (~24%)** | Сейчас stage ~0.448–0.992 — слишком много vs пульт на phone |
| Chrome unified | **одна** полоса ~36–48 px | 44–48 px | Сейчас Status 0.396–0.440 + Reply 0.348–0.388 = ~92 px — **слить** |
| Crate | 40–44 px | 48 px | Chips ≥48×40 hit |
| Deck console | низ → под chrome | то же | Деки + center + crossfader |
| Crossfader band | **12–14%** высоты console / ~56–72 px полоса | **14–16%** / thumb track ≥56 dp | Не вылезать за panel |

### 2.2 Max длина track (критично)

| Контрол | Max длина **видимого track** | Thumb (visual) | Hitbox |
|---|---|---|---|
| **FILTER** | **≤ 62% ширины pad** (~ 0.62 pad) · @1080 pad≈620 → track ≤ **≈380 px** | 28×40 px | **≥48×48** |
| **PERC** | **вертикальный** track ≤ **48% высоты mix-колонки** · ≤ **120 px** travel @1080 | 40×28 px | **≥48×48** |
| **MUSIC** | как PERC, рядом | 40×28 px | **≥48×48** |
| **Crossfader** | **≤ 22% ширины экрана** desktop · **≤ 26%** Android · @1080 ≈ **420–500 px** max, не «на всю center column» | 32×48 px | **≥48×56** |

**Правило мёртвой зоны:** длинный horizontal track с тонким thumb запрещён. Если travel > 380 px @1080 — укорачивать track или менять ориентацию, не растягивать fill «для красоты».

### 2.3 Transport (СТАРТ / СТОП / БАСС)

| | Spec |
|---|---|
| Высота ряда | **≥48 px** (@1080) |
| Ширина кнопки | ≥ **31%** pad каждая (3 equal) или 2+1 (СТАРТ шире только если СТОП/БАСС всё ещё ≥48×48) |
| Gap | 8–12 px |
| Шрифт | 16–18 px; caption целиком в кнопке, без ellipsis на RU «СТАРТ» |

### 2.4 Chrome — иерархия текста

**Одна полоса** `ChromeBar` вместо Status + Reply:

| Слот | Content | Style | Priority |
|---|---|---|---|
| L1 | Venue short (`Neon Basement`) | 14–16 px · `text-primary` | P1 |
| L2 | Set / phase (`клуб 1/3`) | 12–14 px · `text-secondary` | P2 |
| Center | **Goal one-liner** или live cue | 14–16 px · `warning` если запрос зала | **P0** |
| R1 | Timer `2:08` | 14 px mono-ish | P1 |
| R2 | **HYPE 35/60** + mini bar 12–14 px | 16 px bold tabular | **P0** |

Reply / tutorial / Act1 hint:

- **Не** вторая full-width полоса на Performance.
- Одна строка → **Feedback rail** в center column (≤2 с fade) **или** ticker поверх Stage (верх 8%, не над фейдером).
- Tutorial Act1: один cue, потом в rail; не держать вечно между залом и crate.

Crate: header `ЯЩИК` 12–14 px; chip labels 12–14; active chip = accent fill + 1px inset, не только hue.

---

## 3. Что укоротить / уплотнить / переставить

### Укоротить

1. **FILTER** — снять full-pad width (`0–1` → track inset + max 62% pad). Label слева 56–72 px, track справа от label.
2. **Crossfader track** — визуальный канал короче center column; поля A|B + воздух; thumb не клипается за низ console.
3. **Status+Reply** — две полосы → одна; длинные фразы не в 5 колонках в ряд.

### Уплотнить

1. Stage на Android: −14…18% высоты → отдать пульту.
2. Deck header: title + badge в один ряд; detail/state короче (1 строка BPM/tag).
3. Loop button — компакт 48×40 у края mix, не конкурирует с PERC/MUSIC.

### Переставить (lock)

```
PAD (сверху вниз):
  Header (DECK · badge · track)
  Disc well | Track meta
  MIX WELL:
    FILTER  ──horizontal short──
    [ PERC vertical ] [ MUSIC vertical ] [ LOOP ]
  TRANSPORT: СТАРТ | СТОП | БАСС
```

**Почему vertical PERC/MUSIC:** на landscape phone горизонтальные half-pad слайдеры дают длинный «мертвый» ход и thumb перекрывает значение пальцем. Вертикальные strips: короткий travel, thumb ≥48, взгляд сверху-вниз совпадает с «stem trim». FILTER остаётся horizontal (один главный EQ-жест, канон strip).

Center column (сверху вниз):

```
  Run CTA («Начать сет») — только pre-set; during set → Beat rail
  Beat rail 4/4
  Feedback rail (≤3)
  Crossfader (short track, fat thumb)
```

---

## 4. ASCII wireframes

### 4.1 Desktop 16:9 (1920×1080)

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ STAGE  ~40%h   crowd · lights · DJ silhouette (no lore over dancefloor) │
├──────────────────────────────────────────────────────────────────────────┤
│ Neon Basement · 1/3 │  цель / cue (1 line)     │ 2:08 │ HYPE████ 35/60 │  ← 40px
├──────┬───────────────────────────────────────────────────────────┬───────┤
│ЯЩИК │ [Neon] [Bass] [Afterhours] [City] … chips ≥48 hit          │       │  ← 44px
├──────┴─────────────────────┬─────────────────┬───────────────────┴───────┤
│ DECK A                     │  [Начать сет]   │                    DECK B │
│ A · Neon Pulse             │  или BEAT 4/4   │             Afterhours · B│
│ ┌────┐ House·Bright        │  ○ ○ ● ○        │        Bright ┌────┐     │
│ │disc│ PLAYING             │  CLEAN MIX +8   │              │disc│     │
│ └────┘                     │                 │              └────┘     │
│ FILTER ════●════           │   A ═══●═══ B   │           ════●════ FILTER│
│        (≤62% pad)          │  cross ≤22% W   │        (≤62% pad)        │
│  PERC ║  MUSIC ║  LOOP     │  thumb 48×56    │   LOOP  ║ MUSIC  ║ PERC  │
│       ║        ║           │                 │         ║        ║       │
│ [СТАРТ][СТОП][БАСС] ≥48    │                 │    [БАСС][СТОП][СТАРТ]   │
└────────────────────────────┴─────────────────┴──────────────────────────┘
  ~ deck 34% W each · gutter/center 28–32% W · console ≥48% screen H
```

### 4.2 Android landscape (touch-first, same skeleton)

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ STAGE ~22%h  silhouette density only · Hype LED bake into chrome if needed│
├──────────────────────────────────────────────────────────────────────────┤
│ Club · phase │  ЗАЛ: …cue…                 │ mm:ss │ HYPE ███ n/max     │ ← 48px
├──────────────────────────────────────────────────────────────────────────┤
│ ЯЩИК │ chips 48h · scroll/clip if >4 visible · no second chrome row      │ ← 48px
├──────────────────────────┬─────────────────┬─────────────────────────────┤
│ DECK A                   │  BEAT · feedback│                   DECK B   │
│ disc+meta compact        │  Run only if    │        compact meta+disc   │
│ FILTER ═══●═══ (short)   │  pre-set        │   (short) ═══●═══ FILTER   │
│  ║PERC  ║MUSIC  [ЛУП]    │  A ══●══ B      │  [ЛУП]  ║MUSIC  ║PERC      │
│  ║      ║                │  fat thumb      │         ║       ║          │
│ [ СТАРТ ][СТОП][ БАСС ]  │  ≥56dp track H  │  [ БАСС ][СТОП][ СТАРТ ]   │
│   each ≥48×48            │                 │    each ≥48×48             │
└──────────────────────────┴─────────────────┴─────────────────────────────┘
  Stage shorter · chrome taller · mix vertical stems · crossfader never under deck buttons
```

### 4.3 Mix well detail (один pad)

```text
┌─ pad inset 12–16px ─────────────────────────┐
│ FILTER                 [========●===]         │  row H ≥48; track ≤62% pad W
│ ┌────┐  ┌────┐  ┌────┐                       │
│ │ P  │  │ M  │  │ЛУП │  vertical travel ≤120px│
│ │ E  │  │ U  │  │ 8  │  gap 8px               │
│ │ R● │  │ S● │  └────┘                       │
│ │ C  │  │ I  │                               │
│ └────┘  └────┘                               │
│ [  СТАРТ  ][  СТОП  ][  БАСС  ]              │  row H ≥48
└──────────────────────────────────────────────┘
```

Deck B — зеркало: disc справа, vertical stems справа-налево опционально (сохранить FILTER top → stems → transport).

---

## 5. Do / Don’t — Client Engineer

### Do

- Один adaptive skeleton; Android = density tokens (stage%, chrome H, track max), не второй префаб без нужды.
- Слить StatusBar + ReplyBar в **ChromeBar**; cue/tutorial → Feedback rail / stage ticker.
- FILTER horizontal **укороченный**; PERC + MUSIC → **Vertical** `Slider.Direction.BottomToTop` (или TopToBottom с invert value — согласовать с audio 0..1).
- Thumb **визуал** может быть уже hitbox; hit ≥ **48×48** (crossfader **48×56**).
- Crossfader: укоротить track, поднять/вписать в center band, **clip** thumb внутри console; метки `A`/`B` текстом + формой.
- Transport row ≥48 dp; multi-touch: bass и crossfader — разные raycast targets.
- Целые px anchors после layout; Point filter; палитра из UI_HUD_BRIEFS.
- Сохранить dual-code A/B: цвет + label + форма угла.
- Safe area insets на Android до P0.

### Don’t

- Не растягивать FILTER/PERC/MUSIC на 100% pad «чтобы заполнить дыру».
- Не оставлять две full-width text strips между залом и crate.
- Не класть promoter paragraph / Act1 debug поверх crossfader или mix.
- Не делать карточки-оверлеи, modal, draggable cards на Performance.
- Не уменьшать thumb visual без увеличения invisible hit.
- Не чинить «длинный слайдер» только утолщением fill — нужен **короткий travel**.
- Не переносить crossfader на всю ширину экрана под обеими деками в этом проходе (канон center band; full-width = отдельный ADR).
- Не менять audio/gameplay API ради вёрстки.
- Не использовать blur/glow на подписях кнопок; Reduced flashing не трогает размеры hitbox.

---

## 6. Acceptance (QA для этого редизайна)

- [ ] @1080: ни один mix/crossfader track длиннее лимитов §2.2.
- [ ] Thumb hit ≥48 dp на FILTER/PERC/MUSIC/crossfader/СТАРТ/СТОП/БАСС/crate chip.
- [ ] Chrome = **одна** полоса; Hype + цель читаются без ReplyBar.
- [ ] Android 20:9 landscape: двупалый жест — fader + Bass Kill без блокировки.
- [ ] Thumb crossfader не торчит за низ console / safe.
- [ ] Grayscale: A/B и Hype всё ещё ясны.
- [ ] Tutorial cue не занимает постоянную вторую полосу.

---

## 7. Mapping к текущему BoothHud (ориентир CE)

| Сейчас | Цель |
|---|---|
| Status `0.396–0.440` + Reply `0.348–0.388` | Одна ChromeBar ~`0.36–0.40` (desktop) / выше на phone |
| Stage `0.448–0.992` | Desktop ~`0.42–1.0`; Android ~`0.76–1.0` |
| Crate `0.304–0.348` | Сразу под chrome; H ≥48 phone |
| FILTER pad `y 0.40–0.62`, x `0–1` | x track ≤0.62 pad; row ≥48px |
| PERC/MUSIC horizontal half-pad | Vertical pair в mix well |
| Crossfader `0.385–0.615` × `0.016–0.148`, track mid | Тот же band, **короче** inner track, thumb clipped |
| Run `0.385–0.615` × `0.208–0.300` | Pre-set only; during set → beat rail занимает слот |

---

*AD lock 2026-11-23 · Артефакт для реализации в `BoothHud` anchors/build helpers · Следом: CE PR + screenshot pass desktop + Android emulator.*
