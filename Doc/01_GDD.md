# 01. Game Design Document (GDD)

**Версия:** 0.5 · **Дата:** 2026-11-25 · **Владелец:** Game Design  
**Связанные документы:** [Vision](00_VISION.md) ← **north star**, [MVP](14_MVP.md), [мастер-док](DECK_ROGUE_MASTER.md).

## 1. Pitch

Симулятор диджея в pixel-art roguelite-оболочке: почти полноценный пульт (с упущениями сложного), живой клуб, который реагирует на сет, и проход дальше только если зал принял выступление. Музыкальная ось — разнообразное techno (референс атмосферы: Mutabor); обычные клубы в забеге идут в случайном порядке, босс-клубы — сюжетные якоря. Прогрессия внутри сета и общая прогрессия забега/меты — **две разные системы**.

## 2. Core loop

`Случайный клуб из пула → Briefing → Свести / сменить треки / вести динамику → Живая реакция зала → Проход по реакции? → Награда (трек ± gear) → Следующий клуб → … → Boss-клуб → Финал или поражение`.

Краткий цикл внутри одного выступления: `заметить потребность зала → выбрать следующий трек из crate → подготовить деку (cue/load) → свести и изменить динамику → получить слышимую и визуальную реакцию → адаптироваться`.

## 3. Session structure

### MVP (P0) — миниатюра видения

- 1 тестовая локация, 2 выступления подряд по 100–140 секунд (весь забег с выбором награды ~5–7 минут). Проверяем: пульт + реакция зала + проход по Hype.
- 4 пластинки × 3 stems (LOW, PERC, MUSIC); каждая 8 тактов/16 секунд при 120 BPM, с гарантированно совместимыми тональностями.
- Особое правило только у второго сета: seed выбирает одно из `maintain_energy` и `perform_one_clean_drop`. Первый сет — обучение, без особого правила. Контракт DJ-013 записан в ClubDefinition мастер-дока.
- 2 из 3 улучшений между выступлениями — срез DJ-014. В DJ-013 карточек нет: победа первого сета ведёт во второй.
- Первое выступление: закончить с `Hype ≥ 60`; второе: `Hype ≥ 65` и выполнить особое условие. **Принцип видения:** дальше пускает реакция зала, не «дожил до таймера».
- Поражение сразу, reason `hype_floor`: `Hype = 0` дольше одной фразы (16 с). Конец таймера без цели — `timeout`. Победа первого сета продолжает забег; победа второго или любое поражение заканчивает его и делает Reset.
- Стартовый Hype каждого сета = 35 и не переносится. Reputation копится, Combo сбрасывается в ×1. Числа — гипотеза плейтеста.
- **P0 не включает** mid-set crate pressure, phrase-locked load Act1, route map, track-reward после клуба, **Mixer depth** и ночь 12 клубов (§3.5) — это P1 (§3.1–§3.5). Не раздувать FIRST GIG этими правилами. FIRST GIG остаётся единственным P0-путём: Play/Stop/Crossfader/Bass Kill (+ filter по ADR).

### Act 1 (P1) — до первого босса включительно

Маршрут одной ночи Act 1 (гипотеза плейтеста, seed-driven). Муз. канон / ids: [`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md) — весь must-ship @ **120 / `keyfam_a_min`** (разнообразие = dialect, не смена BPM).

```
Старт → Shuffle(act1_regular_pool) взять N=2 без повтора
  → клуб A → Reward (track pick + gear)
  → клуб B → Reward (track pick + gear)
  → boss club_the_signal → Act1 clear / поражение
```

| Слот | Источник | Тип |
|---|---|---|
| 1–2 | `act1_regular_pool` = `club_neon_basement`, `club_warehouse_pulse`, `club_bass_factory` | Ordinary, **рандомный порядок**, N=2 |
| 3 | `club_the_signal` («The Signal») | **Boss**, фиксированный якорь; **не** в RNG |

`club_dream_lounge` / `club_rave_district` — Content Bible packs, **ship Act2** (не в `act1_regular_pool`). Пороги ниже — гипотезы плейтеста.

| clubId | Briefing goal | targetHype | Особое условие прохода | initialHype | durationBars |
|---|---|---:|---|---:|---:|
| `club_neon_basement` | Тёплый зал, чистые смены | 60 | нет (мягкий вход) | 35 | 64 |
| `club_warehouse_pulse` | Длинные линии; готовь B | 62 | ≥1 `CleanPrepTransition` **или** нет `StaleGroove` | 35 | 64 |
| `club_bass_factory` | Мощный low без грязи | 63 | ≤1 `BassConflict` **и** ≥1 clean transition | 35 | 64 |
| `club_the_signal` | Фазы: удержать сигнал → пробой | 70 | `signal_lock`: Hype≥55 к mid-timer; `signal_surge`: ≥1 Drop + ≥1 `CueReadyHonor` | 40 | 72 |

- **Win club:** реакция OK → Reward → next. **Win boss:** Act1 clear.
- **Lose:** `hype_floor` / `timeout` / fail special → Reset run (crate+gear); meta не откатывается.
- Hype не между клубами; Reputation копится; Combo ×1 на клуб. Run-crate cap Act1 = 8; все must-ship совместимы → crate переносится между ordinary без time-stretch.
- Полная ночь 12 клубов — §3.5. Здесь cap 8 и «Win boss = Act1 clear» остаются коротким актом. FIRST GIG (5–7 минут) этой ночью не становится.

### Полная версия (P1/P2) — по [00_VISION](00_VISION.md)

- Пул обычных клубов с **разной** музыкой/вкусом; порядок в забеге **рандомный** (seed).
- Ночь кампании — 3 акта с боссом на якоре. Длина, pass и награды — §3.5. Короткий Act1 спроектирован выше.
- Редкие пластинки и разные стратегии живут в той ночи. Сумма сетов меню не включает.
- **Двойная прогрессия:** внутри сета (фазы, локальные запросы, Combo/напряжение, deck-prep) ≠ забег (crate + gear) ≠ мета (unlocks выбора, не линейный +% Hype).

### 3.1 Deck management depth (P1 Act 1) — не ломает P0

P0 остаётся: две деки, Load/Queue на границу такта, 4 пластинки, без mid-set crate UI. **P1 Act 1** усложняет *управление деками*, а клуб реагирует именно на качество этой работы — не только на число Hype.

| Усложнение | Правило (гипотеза) | Что слышит/видит игрок |
|---|---|---|
| **Phrase-locked load** | `LoadTrack` на неиграющую деку применяется на **границе фразы** (8 bars); mid-phrase → состояние `QueuedLoad` | Индикатор QUEUED до границы фразы |
| **Mid-set crate** | Во время сета можно взять трек из run-crate на пустую/остановленную деку; **играющую** деку нельзя перезаписать — только `Stop → Load → QueueStart` | Crate panel mid-set; нет «тихого» hot-swap |
| **Library pressure** | Старт crate = 4 MVP-трека; после наград crate растёт (cap Act1 = 8). Нужный energy-класс часто лежит в crate, а не на деке | Давление выбора «что держать на B» |
| **Cue readiness** | Дека B (или неиграющая) с Loaded+Queued ≥1 бар до смены = `CueReady` | Маркер CUE на деке |
| **Dual-deck constraint** | Обе деки ниже порога слышимости >2 доли → `DeadAir`; Stop доминирующей mid-phrase без handoff → `HotSwapFumble` | Толпа «остыла», короткий callout |

**Реакция клуба на управление деками** (новые события скоринга; только Performing; один `eventId` — одна награда):

| eventId kind | Когда | Hype Δ | Reputation | Зал / UI |
|---|---|---:|---:|---|
| `CueReadyHonor` | Клуб запросил подготовку (`request_prep_b`); `CueReady` выполнен до конца окна (1 фраза) | +3 | +40 | «Готов» / кивок толпы |
| `LateLoad` | Load завершился после окна запроса | −3 | 0 | «Опоздал с пластинкой» |
| `DeadAir` | Тишина на обеих деках >2 доли | −5 | 0 | «Тишина на пультах» |
| `HotSwapFumble` | Stop доминирующей mid-phrase без handoff | −4 | 0 | «Сорвал смену» |
| `CleanPrepTransition` | Переход, где B была `CueReady` ≥1 бар до старта crossfade и transition score≥70 | +2 поверх clean transition | +30 | «Чистая подготовка» |

Запросы клуба Act1 (seed выбирает из пула клуба, не чаще 1 активного окна): `request_prep_b`, `request_energy_shift`, `request_swap_before_phrase`. Это **внутрисетовый** слой — не апгрейд и не мета.

### 3.2 Level-clear track rewards (P1) — решение systems

Music заполняет пулы ([`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md) §5). Systems решает *когда* и *какой режим выбора*. Вся Act1 must-ship семья одна → **выбор 1-of-N треков валиден**.

| После | Режим track | Пул / id | Слой |
|---|---|---|---|
| Ordinary слот 1 | **Выбор 1 из 2** | `reward_pool_act1_common` = `track_voltage_alley`, `track_mutabor_pulse` | Run crate |
| Ordinary слот 2 | **Выбор 1 из 3*** | `reward_pool_act1_mid` = `track_foundry_drop`, `track_afterglow_key` (+ 1 leftover из common, если нужно 3 карты) | Run crate |
| Boss win | **Fixed drop** | `track_signal_carrier` → run-crate **и** `meta_unlock` того же id | Run + мета |

\*Если после слота 1 уже взяты оба common — mid roll из mid+уже имеющихся с skip дубликатов / reroll.  
`track_signal_breach` = `boss_exclusive` (только performance The Signal), **не** clear-награда.

**Отличие от upgrade gear:**

| | Track reward | Upgrade gear |
|---|---|---|
| id | `track_*` | `upgrade_*` |
| Что даёт | Материал сведения в run-crate | Меняет правила микса / управление |
| Стак | Уникален по id; дубликат в offer → reroll | Act1: эффекты одного типа не стакаются |
| Мета | Boss: `track_signal_carrier` в `UnlockedTrackIds` | Мета-gear / currency — вне Act1 clear |

Порядок ordinary Reward: **1) Track pick → 2) Upgrade pick (2 из 3)**. Boss: fixed Carrier (+ meta); gear необязателен.

### 3.3 Сознательно вне Act 1 must-ship

`club_dream_lounge` / `club_rave_district` (Act2 packs), Final Festival, смена BPM mid-set, Record Shop, stacking upgrades, обязательный live headphone cue как skill-check, Hot Cue suite, EQ3, Echo/Reverb pack. Маршрут этих клубов в ночи кампании — §3.5; в must-ship Act1 и в FIRST GIG их нет.

### 3.4 Mixer depth (P1 Act 1) — поверх пульта, не ломает P0

**Правило одним абзацем:** Act1 углубляет сведения *крутилками* (filter + stem gains + loop length), а зал отвечает JRPG-`CrowdMotion` tier bump’ом на *удачные* жесты — не на три кнопки FIRST GIG. P0 остаётся единственным обязательным путём (Play/Stop/Crossfader/Bass Kill + filter по ADR); Mixer depth — Act1 playable слой поверх. Успех по `MixSnapshot` (слышимый микс), не по клику. Числа — гипотезы плейтеста; data id — `snake_case`.

#### Обязательный набор контролов (слышно + влияет на реакцию)

| Контрол | id / command | Диапазон | Слой | Слышит | Видит / реакция |
|---|---|---|---|---|---|
| **Filter** low-pass per deck | `SetFilter(deck, …)` | **0..1** (0 open → 1 closed); сглаживание 10–30 мс; ADR/Web Audio Gate | P0 после ADR; **обязателен Act1** | Тон закрывается/открывается | Knob FILTER |
| **MUSIC gain** per deck | `SetMusicTrim` | **0..1** | Act1 | Melody/synth громче/тише | Knob MUSIC |
| **PERC gain** per deck | `SetPercTrim` | **0..1** | Act1 | Hats/perc громче/тише | Knob PERC |
| **LINE** channel fader per deck | `SetLineGain(deck, 0..1)` | **0..1**, дефолт **1**; не кроссфейдер | Act1 | Громкость деки поверх кроссфейдера | Knob/fader LINE |
| **Loop Length** | `SetLoopLength` | **1 / 2 / 4 / 8** bars; **quantize start** на ближайшую допустимую границу (1–2 → bar; 4 → half-phrase; 8 → phrase) | Act1 | Луп короче/длиннее с границы | `LOOP 1\|2\|4\|8` + QUEUED |
| Crossfader | `SetCrossfader` | 0..1 equal-power | P0+Act1 | Баланс A/B | Fader |
| Bass Kill | `SetBassKill` | bool → LOW 0 | P0+Act1 | Кик/бас уходит | Toggle |
| Play / Stop | QueueStart / Stop | квант на boundary | P0+Act1 | Старт/стоп | Transport |
| Crate load | `LoadFromCrate` / phrase-locked (§3.1) | mid-set | Act1 | Новый трек с границы | Crate + QUEUED |
| CueReady | state | Loaded+Queued ≥1 бар | Act1 | — | Маркер CUE |

LOW только через Bass Kill + deck/crossfader gains; отдельный LOW-trim в Act1 **не** must-ship.

#### Реакция зала (`CrowdMotion`)

Удачный **filter sweep в фразу** / **stem contrast** / **clean transition** / **drop** / **loop snap** → `CrowdMotion` tier bump (visual density/bob/arms на 1–2 бара) + optional small Hype. Tier ∈ {0,1,2,3} (= runtime `cheerIntensity`); в одном баре берётся **max** tier, Hype суммируется по разным `eventId`. Только Performing; пауза / audio focus не штрафуют.

| ScoreKind (C#) | event kind id | Когда (гипотеза) | Hype Δ | Rep | CrowdMotion tier | Антифарм |
|---|---|---|---:|---:|---:|---|
| `CleanTransition` | `clean_transition` | Смена доминирующей деки, score≥70 | +8 | +100 | **2** | 1× на завершённую смену |
| `Drop` | `drop` | LOW cut→return на границе | +12 | +140 | **3** | cooldown ≥4 bars |
| `CueReadyHonor` | `cue_ready_honor` | prep-запрос выполнен вовремя | +3 | +40 | **1** | 1× на окно запроса |
| `CleanPrepTransition` | `clean_prep_transition` | clean + CueReady ≥1 бар | +2 поверх | +30 | **1** | тот же transition `eventId` |
| `FilterPhrase` | `filter_phrase` | Доминирующая слышимая дека: \|Δfilter\|≥0.35, слышимое изменение **≥1 bar**, пик/конец на phrase **или** bar boundary | +4 | +50 | **2** | ≤1× / phrase / kind |
| `StemPunch` | `stem_punch` | Stem contrast: ≥1 bar PERC **или** MUSIC ≤0.25 при другом ≥0.55 (LOW слышим/kill path) → на boundary punch restore ≥0.70 **или** punch-up Δ≥0.25 в armed drop; без BassConflict | +3 | +40 | **2** (с Drop в том же баре — cap **3**) | ≤1× / phrase; нужен слышимый Δ ≥1 bar |
| `LoopSnap` | `loop_snap` | `SetLoopLength` применился на квантованной границе (1/2/4/8), дека слышима, длина **сменилась** | +2 | +25 | **1** | ≤1× / phrase; spam overwrite queue без score |
| `PhraseStart` | `phrase_start` | Quantized start слышимой деки (gain≥0.35) на границе фразы (bar-in-phrase 0, beat 0); другая дека тоже играет и слышна | +2 | +25 | **1** (Nod) | ≤1× / phrase / deck. Тихая дека и старт не на фразе — 0 |
| `BassHandoff` | `bass_handoff` | Обе деки playing и gain≥0.35 целый бар (≥2 с); ровно один LOW≥0.45, другой LOW≤0.20; выплата на границе бара. В том же Observe, где есть `Drop`, handoff не создаётся | +3 | +35 | **1** (Nod) | ≤1× / phrase. Одна дека — не handoff |
| `LineLift` | `line_lift` | LINE 0..1 (дефолт 1, не кроссфейдер): провод ≤0.25 → ≥0.75 за ≥1 бар, дека playing, итоговый gain≥0.35, конец на bar boundary | +3 | +40 | **2** (Bounce) | ≤1× / phrase / deck. Кроссфейдер без движения LINE не платит. Без слышимого Δ — 0 |

#### Антифарм крутилок

- Filter / PERC / MUSIC без **слышимого** изменения микса (\|Δ\| после сглаживания < ε≈0.05) на протяжении **≥1 bar** → quality-событие **не** создаётся.
- Повтор того же дешёвого жеста без смены трека/доминирующей деки не кормит Combo streak.
- Loop Length: один apply на boundary на фразу; лишние команды → `Queued` overwrite, без `loop_snap`.
- `PhraseStart` / `BassHandoff` / `LineLift` — по свойствам `MixSnapshot` (playing, gain, LOW, LINE), не по клику. Только Performing; один `eventId` — одна награда; пауза и audio focus не штрафуют.
- `phrase_start`: ≤1× / phrase / deck. Тихая дека (gain<0.35) и старт не на фразе — 0. Нужна вторая играющая слышимая дека. В том же Observe, где у этой деки есть `LineLift`, платит только `line_lift`.
- `bass_handoff`: ≤1× / phrase. Одна дека — не handoff. В том же Observe, где есть `Drop`, handoff не создаётся.
- `line_lift`: ≤1× / phrase / deck. Кроссфейдер без движения LINE не платит. Без слышимого Δ — 0. Движение LINE само по себе, если итоговый gain<0.35, — 0.

#### Пример на одном такте (120 BPM, 1 bar = 2 s)

Доля «раз» фразы: A доминирует, filter 0.85 → игрок открывает до 0.20 за **≥1 bar** и отпускает на bar/phrase boundary → `filter_phrase` (+4 Hype, CrowdMotion **2**). Позже: MUSIC 0.15 при PERC 0.80 **целый бар**, на границе MUSIC→0.85 → `stem_punch` (+3, tier **2**). Смена LOOP 8→4 на phrase boundary при играющей деке → `loop_snap` (+2, tier **1**). На границе фразы B входит слышимо при играющей A → `phrase_start`; позже бар обе вверху, LOW только у B → `bass_handoff`; LINE A с 0.1 до 1 за бар, дека слышна → `line_lift`. FIRST GIG без Act1-knobs по-прежнему только на transition/drop/bass.

#### Что запирает тест / что сознательно не покрыто

- **Запирает (EditMode):** `FilterPhrase_NearBoundary_PaysOnce`, `FilterPhrase_WithoutTravel_DoesNotScore`, `StemPunch_…`, `LoopSnap_…`, `CrowdEnergy_BumpsOnListedKinds`, антифарм без Δmix ≥1 bar; `PhraseStart_OnPhrase_PaysOnce`, `PhraseStart_OffPhrase_DoesNotScore`, `BassHandoff_OneLowForABar_PaysOnce`, `BassHandoff_SingleDeck_DoesNotScore`, `LineLift_TravelOverBar_PaysOnce`, `LineLift_CrossfaderOnly_DoesNotScore`, `CrowdEnergy_BumpsOnDeckActions`.
- **Не покрыто Act1 must-ship:** high-pass/band-pass, EQ3, send FX, Hot Cue, per-stem filter, BPM/key mismatch mid-set, обязательный headphone cue, стак Mixer-depth upgrades. Audio scheduler может временно ship’ить subset границ (4↔8) до полного `1|2|4|8` — **правило канона** всё равно 1/2/4/8. Отдельным тестом не закрыто: `bass_handoff` в том же Observe, где уже есть `Drop`; тихий `phrase_start` (gain<0.35); `line_lift` без слышимого Δ кроме «только кроссфейдер»; пауза и audio focus (штрафа нет — общее правило Performing).

### 3.5 Ночь кампании (DJ-118) — длина P1 на прежнем clock

Гипотеза плейтеста, не вечная истина. **P0 `ClubDirector` этим срезом не меняется.** FIRST GIG остаётся контрактом §3: 1 клуб, 2 сета, ~5–7 минут. Ночь ниже — другой маршрут на том же clock **120 BPM, 4/4, бар = 2 с**. Новый audio clock не вводится. Меню, брифинг и экран награды в сумму сетов не входят.

Ночь = 3 акта по порядку × (3 ordinary в случайном порядке + 1 фиксированный босс) = **12 клубов**. Босс в RNG не входит. Один seed — один порядок ordinary. Hype между клубами не переносится. Reputation копится. Combo сбрасывается в ×1.

```
Act1: Shuffle(club_neon_basement, club_warehouse_pulse, club_bass_factory) → club_the_signal
Act2: Shuffle(club_dream_lounge, club_rave_district, club_chrome_pier) → club_the_static_throne
Act3: Shuffle(club_glass_cathedral, club_night_exchange, club_roof_relay) → club_final_festival
```

| Акт | Ordinary, bars | Босс, bars | Такты |
|---|---|---|---:|
| Act1 | 64 + 64 + 64 | The Signal 72 | 264 |
| Act2 | Dream / Rave / Chrome 72 + 72 + 72 | Static Throne 96 | 312 |
| Act3 | Cathedral / Exchange / Roof 80 + 80 + 80 | Final Festival 96 | 336 |

`64×3+72 + 72×3+96 + 80×3+96 = 912` тактов × 2 с = **1824 с = 30:24** сетов. Бары Act1 (64/64/64/72) и правила Signal не двигаются. Срез N=2 и cap 8 в §3 — короткий акт, не эта ночь.

**Конец ночи.** Победа ordinary → награда слота → следующий клуб. Победа Signal выдаёт босс-пластинку и продолжается, reason `signal_clear`. Победа Throne так же, reason `throne_clear`. Ночь кончает только победа Final Festival, reason `campaign_clear`. Поражение (`hype_floor`, `timeout`, `room_unanswered`, провал pass) кончает ночь и делает Reset run; мета не откатывается. В коротком Act1-spine победа Signal по-прежнему может зваться `act1_clear` — это не reason этой ночи.

**Pass** сидит поверх вкуса зала. Индекс клуба — место в ночи 0…11, на границе акта не сбрасывается. Индекс 0: дополнительного вкуса нет. Индекс ≥ 1: как DJ-116, один `CleanTransition` на вкусе доминирующей деки или два `CleanTransition` на любых. Промах вкуса Hype не режет (`room_unanswered`). Pass читает события микса, не кнопку. Pass Act1 (Neon без особого, Warehouse, Bass Factory, Signal) остаётся. Ниже только новые.

| Клуб | clubId | Pass | Условие прохода |
|---|---|---|---|
| Dream Lounge | `club_dream_lounge` | `soft_hold` | ни одного `StaleGroove`. `PhraseDecay` не считается |
| Rave District | `club_rave_district` | `need_drop` | ≥1 `Drop` и ≥1 `CleanTransition` |
| Chrome Pier | `club_chrome_pier` | `salt_limit` | 0 `BassConflict` и ≥1 `CleanTransition` |
| The Static Throne | `club_the_static_throne` | `throne_phases` | Hype ≥ 52 на половине (бар 48 из 96), плюс ≥1 `Drop` и ≥1 `CueReadyHonor` |
| Glass Cathedral | `club_glass_cathedral` | `echo_twice` | ≥2 dialect clean |
| Night Exchange | `club_night_exchange` | `paid_drop` | ≥1 `Drop` и ≤1 `BassConflict` |
| Roof Relay | `club_roof_relay` | `clear_line` | ≥1 `CueReadyHonor` и ни одного `StaleGroove` |
| Final Festival | `club_final_festival` | `festival_phases` | Hype ≥ 58 на половине (бар 48 из 96), плюс ≥1 `Drop` и ≥1 dialect clean |

Половина — один снимок, когда истекла половина длины сета. Ниже порога фаза босса не взята, даже если Hype вырос позже. Dialect clean = `CleanTransition`, у которого жанр доминирующей деки входит во вкус этой площадки. Запись только в crate не считается. Signal без новых чисел: `signal_lock` Hype ≥ 48 к середине и `signal_surge` (`Drop` + `CueReadyHonor`). Старт / цель Act2–3 (гипотеза): Dream 40/58, Rave 38/60, Chrome 38/62, Throne 42/70, Cathedral 36/64, Exchange 36/66, Roof 36/68, Festival 40/75.

**Вкус.** Списки Act1 не меняются: Neon — House, Deep House, Electro House, Acid House; Warehouse — Warehouse Techno, Dub Techno; Bass Factory — Bass Tech; The Signal — Warehouse Techno, Dub Techno, Bass Tech, Industrial. Добавлено:

| Клуб | Вкус (точная строка жанра) |
|---|---|
| Dream Lounge | Deep House |
| Rave District | Broken Techno, Bass Tech |
| Chrome Pier | Electro, Electro House |
| The Static Throne | Industrial, Electro |
| Glass Cathedral | Glass Techno, Deep House |
| Night Exchange | Electro, Acid House |
| Roof Relay | Industrial, Dub Techno |
| Final Festival | Industrial, Deep House, Glass Techno, Electro, Bass Tech |

**Награды.** Слот — номер ordinary внутри акта (0…2), не индекс ночи. Игрок берёт 1 карту. Дубликат id в offer не кладётся. Слот 0 — common-пара этого акта. Слоты 1–2 — mid этого акта, до 3 id, и ещё одна невзятая special-аура этого акта. Special — дополнительная карта: если ordinary-оффер короче трёх, она дописывается, иначе подменяет последнюю. Если special акта уже все в crate, слот остаётся только mid. Босс — фиксированная пластинка в crate и в мету, без выбора 1-of-N. Cap crate этой ночи = **16** (4 стартовых + до 9 выборов ordinary + 3 босса). Cap 8 короткого Act1 этим срезом не переписывается. Состав common/mid Act2 и Act3 называет Music; режим слота — здесь.

| Акт | Слот 0 | Слоты 1–2 | Босс → crate и meta |
|---|---|---|---|
| Act1 | `reward_pool_act1_common` | `reward_pool_act1_mid` (до 3) + одна из четырёх Act1-аур | `track_signal_carrier` Signal Carrier |
| Act2 | `reward_pool_act2_common` | `reward_pool_act2_mid` (до 3) + одна Act2-аура | `track_static_crown` Static Crown |
| Act3 | `reward_pool_act3_common` | `reward_pool_act3_mid` (до 3) + одна Act3-аура | `track_festival_anthem` Festival Anthem |

**Ауры.** Четыре Act1 без изменений: `track_amber_bloom`, `track_void_static`, `track_glass_rain`, `track_redline_siren`. Новая аура меняет только Hype и только пока эта пластинка — доминирующая дека (выше gain; обе играют и gain равен — B, как у Act1). Добавка к событию, не замена базы и не процент шкалы. Прощение — исключение: отрицательный Hype события обнуляется и в плюс не переворачивается. Reputation аура не трогает. Один `eventId` — одна выплата. Только Performing; пауза и audio focus не штрафуют. Пока пластинка доминирует, зал рисует свой wash (арт, отдельный на id). Это пластинка, не gear `+% Hype`.

| id | Имя | Добавка Hype |
|---|---|---|
| `track_velvet_hush` | Velvet Hush | простить `StaleGroove` и `PhraseDecay`; `Drop` −3 |
| `track_hardline_yellow` | Hardline Yellow | `Drop` +4, `StemPunch` +3, `BassConflict` −4 |
| `track_salt_chrome` | Salt Chrome | `CleanTransition` и `BassHandoff` +3, `MissedTransition` −3 |
| `track_prism_choir` | Prism Choir | `FilterPhrase` +4, `LoopSnap` +3, `Fuss` −4 |
| `track_night_ledger` | Night Ledger | `CleanPrepTransition` +4, `HotSwapFumble` −4 |
| `track_skywire` | Skywire | `LineLift` +4, `PhraseStart` +2, `PhraseDecay` −3 |
| `track_static_crown` | Static Crown | `CueReadyHonor` +4, `StaleGroove` −4, `DeadAir` ещё −3 |
| `track_festival_anthem` | Festival Anthem | `Drop` +4, `CleanTransition` +2, `BassConflict` −5 |

Velvet / Hardline / Salt — special Act2. Prism / Ledger / Skywire — special Act3. Crown и Anthem приходят боссом, не из слота 1–2. У Signal Carrier новой ауры нет.

**Пример на одном такте.** 120 BPM, бар = 2 с. Roof, в эфире Skywire: LINE с 0.1 до 1 за бар, дека слышна, конец на границе → `line_lift` +3 и аура ещё +4, итого +7, пока Skywire доминирует. Соседняя дека без Skywire этот +4 не получает. Тот же `eventId` второй раз не платит. Chrome без единого `BassConflict` и с одним `CleanTransition` берёт `salt_limit`; при индексе ≥ 1 вкус всё ещё ждёт чистый на Electro / Electro House или второй чистый на любом.

**Что запирает тест.** EditMode: маршрут из 12 клубов; сумма длин сетов 1824 с; победа Signal → `signal_clear` и ночь жива; победа Festival → `campaign_clear` и ночь кончилась; новые pass; Hype новых аур на доминирующей деке. Тесты FIRST GIG остаются зелёными.

**Сознательно не покрыто.** Меню в 30:24. Стартовый и финишный Hype Act2/Act3. Списки id в common/mid Act2/Act3. Пиксели wash. Короткий Act1-spine (`act1_clear`, cap 8, N=2 в тексте §3). `ClubDirector`, другой BPM, time-stretch, стак аур, gear `+% Hype`. `CleanPrep` у Salt Chrome не усилен: «clean» там — `CleanTransition`.

## 4. Выступление

### Фазы

1. **Briefing (до запуска):** имя клуба, жанровый вкус, цель, лимит времени, стартовые пластинки / crate. Игрок может прослушать каждый трек в режиме cue (в MVP — превью через UI до старта, при необходимости без одновременного live-monitor).
2. **Performance:** таймер, Hype, Combo, Reputation, деки, (P1) crate mid-set, запросы подготовки, Mixer depth knobs, CrowdMotion / свет.
3. **Result:** победа/поражение, очки и объяснимые причины; в победе — выбор награды (P0: gear; P1 Act1: track ± gear), кроме случаев fixed boss drop.

### Неочевидная важная вещь

Нельзя принуждать игрока к единственной заранее прописанной кнопочной последовательности. Успех начисляется за свойства музыкального результата (соблюдение фразы, чистота баса, разнообразие, попадание в вкус клуба, **готовность второй деки**, **осмысленные filter/stem/loop жесты**), а не за точную комбинацию команд.

## 5. Механические ресурсы

- **Hype:** от 0 до 100, ресурс выживания и цель испытания.
- **Combo:** множитель за качественные и разные действия; ошибка может сбросить его.
- **Reputation:** очки выступления / статистика забега.
- **Pulses of Tension (внутренний параметр):** краткосрочное напряжение для дропа; в MVP может отображаться простым индикатором «BUILD» без числовой шкалы.
- **Deck Loadout:** две активные пластинки; P0 библиотека из четырёх; P1 Act1 — run-crate до 8 с mid-set load.
- **CueReady (P1):** флаг неиграющей деки, готовой к фразовой смене — ресурс *внутри сета*, не валюта забега.
- **Mixer depth state (P1 Act1):** filter / PERC gain / MUSIC gain / loop length (1|2|4|8) — слышимые крутилки; кормят CrowdMotion+Hype только осмысленными жестами (§3.4).

### 5.1 Дневной хаб (DJ-120…DJ-125) — вне FIRST GIG

Плоский кошелёк между ночами. Это не +% Hype и не Reputation. Пороги FIRST GIG не двигаются: старт 35, первый сет ≥ 60, второй ≥ 65.

Завершённый сет платит один раз, ключ `gig:{runId}:{setIndex}`. Hype зажимается в 0…100, `n = floor`. Победа: `15 + n/5` (15…35). Поражение: `2 + n/10` (2…12). Незавершённый сет и пустой runId — 0. Повтор ключа кошелёк не двигает. Проигрыш не забирает монеты и купленное.

В забег входит одна надетая дека. Черновая без фильтра и стемов, Срез только фильтр, Полосы только стемы. `deck_starter` — 0, уже в сейве и надета: play/stop, crossfader, bass. `deck_filter` — 60, только фильтр. `deck_stems` — 120, только стемы. `gear_spare_leash` — 40, не дека: скоринг, ручки и прогулку не открывает, надеть нельзя. Покупка списывает цену целиком и пишет id один раз. Дубликат, нехватка монет, неизвестный id кошелёк не трогают. Черновая без фильтра и стемов; у Warehouse и Bass Factory цель 50, чтобы два чистых и один дроп перекрывали спад фраз. Первый подвал создаётся после первого законченного клуба (`HubUnlocked`), до этого двери нет.

Прогулка считается днём хаба, не часами ОС. `SettleNight(runId)` один раз на финал ночи: тот же runId ничего не делает, иначе день +1 и прогулка снова свободна. Успех: +4 монеты и день потрачен. Вторая в тот же день — 0. Отказ до конца день не тратит и не платит. Hype формула не читает и не пишет.

## 6. Что считается навыком

- Хорошее время запуска и перехода (фразировка), а не миллисекундный twitch-game.
- Работа с контрастом энергии и грамотное развитие сета.
- Чистое сочетание басов и музыкальных слоёв.
- Умение адаптировать переходы под текущую задачу.
- **(P1)** Подготовка следующей пластинки заранее; давление crate; dual-deck без тишины.
- **(P1 Mixer depth)** Filter sweep в фразу; stem contrast punch; loop snap на границе — зал слышит и CrowdMotion поднимается.
- Применение билда; понимание его слабостей.

## 7. Управление и поддерживаемые форматы

- Windows: мышь/клавиатура, 16:9 с адаптацией.
- Web: desktop Chrome, Firefox, Edge; Safari — тестируется до обязательного включения. Аудиозапуск только после жеста пользователя.
- Android: **landscape, native build**, два больших контроллера/панели и одновременный multitouch; без требования физической клавиатуры.
- Browser on Android: **не обещаем паритет в MVP**, даже если версия Web способна запускаться.

Подробности см. [UI](11_UI_UX.md) и платформенные документы.

## 8. Состояния игры

`Boot → AudioUnlock → MainMenu → PreClub → Performing → Result → Reward → PreClub → FinalResult`.

Срез DJ-013 экран Reward пропускает: победа сета 1 идёт с Result во второй сет. Награду между сетами ставит DJ-014.

Пауза во время аудио: паузируется одновременно игровая шкала и все деки. При возвращении из background не «догонять» время и не штрафовать игрока; переинициализировать расписание при необходимости (особенно Web).

## 9. Failure and fairness

- Ошибки объяснимы словами: «басовый конфликт», «переход мимо фразы», «слишком долго один и тот же грув»; P1 также «опоздал с пластинкой», «тишина на пультах», «сорвал смену».
- Рандом выбирает только из музыкально совместимого контента.
- Предупреждение до критического снижения Hype (например, при Hype < 20).
- Нельзя потерять из-за пропуска browser resume, нулевой громкости системного микшера, лагов при загрузке/в свёрнутой вкладке — такой сценарий ставит игру на паузу или предлагает рестарт выступления.

## 10. Контент и атмосфера

Ночной подпольный город в языке **JRPG-inspired premium pixel** (зал сверху / пульт снизу), techno-ось (референс плотности: Mutabor). У клуба — свой саунд (dialect), палитра, свет, толпа, запросы; босс-клубы несут сюжет (`club_the_signal` — пик Act 1). Лор — короткие реплики промоутера и плакаты, не перекрывает пульт. Канон арта: [`19_ART_DIRECTION_JRPG.md`](19_ART_DIRECTION_JRPG.md); Content Bible: [мастер-док](DECK_ROGUE_MASTER.md); музыкальные пулы/rewards: [`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md).

## 11. Definition of fun

Если убрать весь арт и выдачу наград, у игрока остаётся музыкальный микшер, позволяющий **осмысленно создать красивый переход**, (в P1) **подготовить следующую пластинку вовремя** и **крутить слои/фильтр/луп так, что CrowdMotion отвечает**. Если это не работает — GDD нужно пересмотреть, а не расширять.

## 12. Вне области MVP (P0)

Многопользовательский режим, собственная загрузка музыки, ручной битмэтчинг, сложная тональная совместимость, собственная DAW, генерация музыки нейросетью, VR, полноэкранные видеоклипы, онлайн-лидерборды, сложная экономика/микротранзакции, iOS. Act 1 route / deck-depth / track-rewards / **Mixer depth** — **P1**, см. §3.1–§3.4. Ночь 12 клубов — тоже P1, §3.5. Ни то ни другое не блокирует FIRST GIG.
