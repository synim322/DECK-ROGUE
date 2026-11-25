# Lighting & VFX Briefs — JRPG-club language

**Проект:** DECK//ROGUE · **Владелец:** VFX / Lighting (ROGUE deck) · **Статус:** v0.1 · **Дата:** 2026-11-23  
**Связано:** [`19_ART_DIRECTION_JRPG.md`](../19_ART_DIRECTION_JRPG.md) (north star), [`00_VISION.md`](../00_VISION.md), [`DECK_ROGUE_MASTER.md`](../DECK_ROGUE_MASTER.md) §12, [`06_AUDIO_TECH.md`](../06_AUDIO_TECH.md)

Документ задаёт **единый язык света и VFX**: JRPG mood volume + читаемые Hype tier внутри ночной площадки. Свет **служит Hype и музыке**, не конкурирует с пультом. Ambient base: `#1A1830` (не чистый чёрный).

---

## 1. North star

| Принцип | Правило |
|---|---|
| **JRPG readability** | Состояние зала читается за 0.5 с без текста: tier → палитра, плотность лучей, амплитуда толпы. Переход между tier — **blend 4–8 тактов**, не мгновенный скачок. |
| **Club neon** | Аддитивные accent-контуры, лампы сцены, лёгкий bloom; фон `#1A1830`; неон = акцент, не единственный свет. |
| **Godrays-lite** | 1–2 мягких «клиньев» света из источников сцены (не full-screen volumetric). Максимум **~12% opacity** базового слоя, без белой заливки кадра. |
| **Beat-sync** | Пульсация привязана к **audio clock** (`Beat` / `Bar` / `Phrase` из `06_AUDIO_TECH`). **Никакого строба на каждый beat** и никакого flash чаще **2 Hz** в default. |
| **Drop = один такт** | Успешный дроп: один **Bar-accent** (пик на downbeat + decay 3–4 beats), затем возврат к tier-baseline. |
| **Safety first** | Default уже безоп seizure-риска; `reducedFlashing` — отдельный **контент-профиль**, не «ослабленный стиль». |

---

## 2. Слои (render order)

Согласовано с Pixel Art Style Guide:

```
BG (стены, постеры) 
  → Crowd silhouettes 
  → Stage / fixtures (спрайты приборов) 
  → VFX_Lighting (аддитив: неон, godrays-lite, tier tint) 
  → VFX_Event (дроп, transition spark — короткоживущие) 
  → DJ & booth props 
  → UI / HUD (не затемнять критические readouts)
```

**Правило кадра:** верхняя половина — зал и свет; пульт не перекрывается godrays. Камера **не трясётся**; допустим micro-nudge **1 px** на downbeat только в tier **Peak** и только вне `reducedFlashing`.

---

## 3. Hype tiers (визуальные состояния)

Одна шкала `Hype [0, 100]` → четыре tier + аварийный **Floor**. Tier меняется по **гистерезису** (вход/выход ±3 Hype), чтобы избежать мерцания на границе.

| Tier | ID | Hype (вход) | JRPG-чтение | Свет / VFX |
|---|---|---:|---|---|
| **Floor** | `tier_floor` | ≤ 0 (поражение) | «Партия проиграна» — зал гаснет | Почти только ambient `#151628`; единственный источник — слабый силуэт exit sign; **без** beat-pulse |
| **Cold** | `tier_cold` | &lt; 25 | Спокойная сцена, мало духа | Приглушённый неон клубной палитры **−40% saturation**; godrays **off**; beat-pulse только на **Bar** (kick bloom 4 px) |
| **Warm** | `tier_warm` | 25–49 | Бой «разогревается» | Полная клубная палитра; godrays-lite **1 луч**; beat-pulse на **beats 1 и 3** (мягкий, &lt;8% Δ luminance) |
| **Hot** | `tier_hot` | 50–74 | Mid-fight pressure | 2 луча godrays-lite, лёгкий **crowd rim** (additive outline); beat-pulse на каждом **downbeat**; Combo ≥ ×2 добавляет **неоновое кольцо** вокруг сцены (1 px, 2 beats fade) |
| **Peak** | `tier_peak` | ≥ 75 | «Limit break» зала | Максимальная насыщенность **в пределах клуба** (не белый); второй луч + **slow sweep** fixture (8 bars loop); Hype UI glow синхронен **Bar**, не Beat |

**Предупреждение Hype &lt; 20 (GDD):** не строб — **amber wash** `#F2CF6E` на 15% opacity, медленное дыхание **1 цикл / 4 bars**. Пульс ускоряется только если Hype падает **три фразы подряд** (тогда 1 цикл / 2 bars), всё ещё &lt; 2 Hz.

**Переход tier:** crossfade палитры и godrays **минимум 2 bars**; на границе Phrase предпочтительно **4 bars**.

---

## 4. Beat-sync контракт (для client/audio)

Все VFX-триггеры подписываются на domain events, не на `Update()` FPS.

| Event | Допустимые VFX | Запрещено |
|---|---|---|
| `Beat` (1) | Tier Warm+: 4 px bloom на активной deck-lamp; Cold: **skip** | Полноэкранный flash, invert |
| `Bar` | Baseline pulse всех tier; Drop decay; fixture sweep tick | Strobe train |
| `Phrase` | Tier re-eval blend; «chapter sting» (тонкая horizontal scanline, 1 frame @ 480×270) | &gt;3 quick flashes / phrase |
| `drop_completed` | `VFX_Drop_Burst` — 1 bar | Повторный burst &lt; 2 bars cooldown |
| `transition_completed` (high quality) | `VFX_Transition_Spark` на активной деке, 2 beats | — |
| `bass_conflict` | Краткий **magenta fringe** на низу кадра, 1 beat, **без** белого | Rapid red flash |
| `HypeDelta` positive | Небольшой **upward godray widen** (+2 px), 1 bar | Screen shake |
| Combo tier-up | Кольцо сцены (см. Hot) | — |

**Scheduling:** визуальный beat indicator и свет используют **тот же origin**, что и scoring (`06_AUDIO_TECH`). Допустимый визуальный lead: 0–33 ms после слышимого удара; **никогда** визуал раньше аудио на &gt;50 ms.

**BPM 120 (MVP):** 1 beat = 0.5 s → максимум 2 пульса/с на чистом beat-sync уже на грани; поэтому **Cold/Warm ограничивают частоту** (см. таблицу tier).

---

## 5. Godrays-lite (спецификация)

| Параметр | Default | Reduced |
|---|---|---|
| Форма | Растровый клин 32×128, feather 8 px, angle ±12° от вертикали fixture | Тот же спрайт, **статичный**, без angle lerp |
| Цвет | `clubKey × tierKey` (multiply), никогда чистый `#FFFFFF` | Desaturate 30%, max opacity 8% |
| Анимация | Медленное «дыхание» opacity ±3% за **4 bars** | **Статичная** opacity |
| Количество | Cold: 0; Warm: 1; Hot/Peak: 2 (разнесены по X) | Max 1 |
| Производительность | 1–2 quads, no realtime blur; опциональный pre-blurred sprite | 1 quad |

**JRPG note:** godray = «свет сюжетной сцены», не реалистичный туман. Края **пиксельные**, без HD gradient mesh.

---

## 6. Неон и fixture vocabulary

Базовые спрайты (переиспользование по клубам):

| Asset ID | Описание |
|---|---|
| `VFX_Neon_Tube_Loop_8f` | 8-кадровый loop лампы, sync **Bar** |
| `VFX_Neon_Flicker_Soft_4f` | Редкий glitch (1 раз / 8 bars), **не** &lt; 2 Hz |
| `VFX_Fixture_Sweep_16f` | Pan light, 16 frames = 8 bars @ 15 fps logical |
| `VFX_Godray_Lite_A/B` | Два угла клина |
| `VFX_Drop_Burst_6f` | Radial ring, lifetime 6 frames, additive |
| `VFX_Transition_Spark_4f` | Deck accent |
| `VFX_Tier_Ring_8f` | Combo / Peak accent |

**Неон-линии:** 1 px core + 1 px outer glow (second pass additive). Цвета только из палитры клуба + tier modifier.

### Палитра клубов (key light)

| Club | Key neon | Godray tint | Примечание |
|---|---|---|---|
| Neon Basement (MVP) | `#7B5CFF` / `#62A4F4` | `#62A4F4` @ 10% | Референс всех tier |
| Warehouse | `#8A9BA8` / `#3D5C73` | `#4A6FA5` @ 8% | Дым — **статичный** overlay, не стробящийся |
| Bass Factory | `#F4A06B` / `#E76F80` | `#F27B50` @ 10% | Акцент на **low-end**: низ кадра warm pulse on **Bar** only |
| Dream Lounge | `#5ED3B1` / `#C9A0DC` | `#7EC8B8` @ 8% | Медленные tier blends (**8 bars**) |
| Rave District | `#F2CF6E` / `#62A4F4` | Alternating **4 bars** | **Default:** без классического strobe — только **color crossfade**; см. §7 |
| The Signal | см. §8 | `#E8F4FF` + glitch | Босс-профиль |

---

## 7. Reduced flashing (`reducedFlashing = true`)

Полное выключение строба и интенсивных flashes (канон MVP). Контент **не** «ломается» — зал остаётся живым за счёт tier palette, crowd motion и UI.

| Эффект | Default | Reduced |
|---|---|---|
| Beat/bar pulse Δ luminance | до 8% (tier-dependent) | **≤ 4%**, только **Bar** |
| Drop burst | 6f ring + godray widen | **UI icon pulse** + 2f soft ring (opacity cap 6%) |
| Phrase scanline sting | 1 frame | **Off** |
| Neon flicker / glitch | редкий | **Off** |
| Godrays | animated breathe | static |
| Rave District strobe | color crossfade 4 bars | **single held split** (жёлтый лево / синий право), **no** alternation |
| Camera nudge | Peak only | **Off** |
| Warning Hype &lt; 20 | amber breathe | **solid amber border** 2 px top edge, no pulse |

**QA:** 30 s запись @ 60 fps → подсчёт кадров с global luminance Δ &gt; 25%: **0** в reduced; в default — **не чаще 1 раз / 2 s** и никогда 3+ flashes за 1 s (WCAG-inspired, не медицинский claim).

---

## 8. Boss: **The Signal** — signature look

**Фантазия:** сюжетный «клуб-антенна» — ночь ловит **перехват эфира**. JRPG: фазы боя как **Acts**, визуал как **limit cut без белой вспышки**.

**Базовый сет (весь бой):**

- Backdrop: тёмный `#0D0F1A`, горизонт **tower + dish** silhouette, мерцающие **редкие** LED (1 раз / 8 bars).
- Primary neon: **cyan-white** `#B8E8FF` / **corrupted magenta** `#C45CFF` — никогда одновременно на 100% opacity.
- Godrays: **3 статичных луча** из dish (lite only); в default — лёгкий **phase rotation ±6°** за **16 bars** (0.006 Hz), не строб.

**Фазы (привязка к сюжетным act rules, не к HP):**

| Phase | ID | Световое поведение | Геймплейный hook |
|---|---|---|---|
| **Scan** | `signal_scan` | Horizontal **scan band** 8 px, travels top→bottom за **2 phrases**; tier capped Warm max | Зал «проверяет» сет |
| **Lock** | `signal_lock` | Vertical bars **за музыкой** ( Bar sync ), цвет lock `#62A4F4` | Требование чистых переходов |
| **Interference** | `signal_interfere` | RGB split **1 px** max, 4 bars on / 4 bars off (**0.125 Hz**) | Контраст энергии / bass discipline |
| **Transmit** | `signal_transmit` | Peak-tier визуал + **slow white godray** из dish (opacity cap 12%) | Порог Hype + особое правило акта |
| **Fade** | `signal_fade` | Все лучи сходят в точку dish за **1 phrase** | Победа/переход сюжета |

**Запреты для The Signal:**

- Нет full-screen white &gt; 1 frame.
- Нет strobe &gt; 2 Hz даже в `signal_interfere` (только **hold/split**, не blink).
- Drop burst заменяется на **`VFX_Signal_Ring`** — концентрические квадраты (пиксель), 4 frames.

**Reduced для босса:** Interference → **постоянный 1 px split без toggle**; Scan → **статичная dim band** в центре; остальная идентичность сохраняется.

---

## 9. MVP срез (Neon Basement only)

Обязательный минимум для DJ-013 / pixel club:

1. Tier Cold / Warm / Hot (Peak опционально, если Hype ≥ 75 достижим в плейтесте).
2. `VFX_Godray_Lite_A` + `VFX_Neon_Tube_Loop_8f` на сцене.
3. `drop_completed` → `VFX_Drop_Burst_6f` + reduced fallback.
4. Hype &lt; 20 amber treatment.
5. Settings flag `reducedFlashing` переключает профиль §7.
6. Свет **не** реагирует на каждое нажатие кнопки — только scoring/audio events (канон Living club).

---

## 10. Naming & data hooks

```
VFX_{Category}_{Name}_{variant}
ClubLightingProfile_{clubId}.asset   // ScriptableObject / JSON
tierState: tier_cold | tier_warm | tier_hot | tier_peak | tier_floor
```

Рекомендуемые поля профиля клуба:

```json
{
  "clubId": "club_neon_basement",
  "neonPrimary": "#7B5CFF",
  "neonSecondary": "#62A4F4",
  "godrayTint": "#62A4F4",
  "tierCurves": { "cold": {}, "warm": {}, "hot": {}, "peak": {} },
  "reducedProfileId": "reduced_standard",
  "bossPhaseOverrides": null
}
```

Для `club_the_signal`: `bossPhaseOverrides` → map phase ID → lighting preset.

---

## 11. Acceptance checklist

- [ ] Tier читается на grayscale screenshot (без цвета Hype).
- [ ] Beat indicator и первый кадр beat-pulse в пределах ±33 ms от аудио.
- [ ] Нет flash чаще 2 Hz в default на 2-минутном Peak-сценарии.
- [ ] Reduced: strobes/flicker/glitch off; зал по-прежнему различим по tier.
- [ ] Drop не спамится при cooldown &lt; 2 bars.
- [ ] The Signal: все 5 фаз узнаваемы, reduced профиль проверен.
- [ ] Android mid-tier: additive layer не роняет FPS ниже целевого budget (см. platform docs).

---

## 12. История

| Дата | Изменение |
|---|---|
| 2026-11-23 | v0.1 — JRPG-club language, Hype tiers, beat-sync safety, reduced profile, The Signal signature |