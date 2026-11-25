# 16. Music Act 1 — разнообразие до The Signal

**Владелец:** Music Supervisor · **Статус:** P1 design (не P0) · **Дата:** 2026-11-23  
**Связано:** [Vision](00_VISION.md), [GDD](01_GDD.md) §3 Act1 / §3.2 rewards, [MVP](14_MVP.md), [Roadmap](15_ROADMAP.md) DJ-105/106/108, [17 tech stems](17_AUDIO_CONTENT_ACT1.md), [18 boss/reward audio](18_BOSS_AND_REWARD_AUDIO.md), [мастер-док](DECK_ROGUE_MASTER.md)

> **Не ломает FIRST GIG / P0.** `TrackLibrary.CreateMvp()` = 4 пластинки @ 120 / `A_minor`. Всё ниже — **P1 slice «до первого босса»**.  
> **BPM:** весь Act 1 = **120** (ADR-002). Разнообразие = диалект / тембр / энергия / плотность, не смена темпа.  
> **Final Festival** и смена BPM mid-set — вне среза (нужен ADR).

---

## 1. Решения

| Решение | Значение |
|---|---|
| Scope must-ship | Пулы для GDD Act1 route: Neon · Warehouse · Bass Factory · **The Signal** |
| Content Bible packs | Dream Lounge · Rave District — **спроектированы**, ship после Act1 / Act2 (GDD §3.3) |
| Сетка | 120 BPM, 4/4, 8 bars = 16.000 s, stems LOW/PERC/MUSIC, WAV (§17) |
| keyFamily Act1 | `keyfam_a_min` на всём must-ship — run-crate можно нести между клубами без time-stretch |
| Совместимость | RNG и reward roll только внутри одной семьи; Act1 must-ship = одна семья |
| 2-bar identity | Каждый клуб отличим от соседа тембром/грувом за 2 такта |
| Права | Новые id = `licenseStatus: planned` (`PLANNED-ACT1-*`) until licensed |
| Объём must-ship | **4 P0 + 10 новых** (см. §6). Dream/Rave +4 later |
| Прогрессия | Music = слоты, id, rarity, sonic role. Systems = *когда* выдать (GDD §3.2, DJ-106) |

### Контракт systems (DJ-105 / DJ-106)

1. Performance library = club hot-set ∪ run-crate (cap Act1 = 8, GDD). Все must-ship ids совместимы @ 120 / `keyfam_a_min`.
2. После ordinary: roll из `reward_pool_act1_common` / `_mid` (состав — §5). Music не назначает очки.
3. Boss clear: fixed `track_signal_carrier` → run-crate **и** `meta_unlock` того же id (GDD).
4. `boss_exclusive` (`track_signal_breach`) — только во время The Signal.
5. P0 `CreateMvp()` не расширять Act1 id до ClubTrackPool loader.

Техника файлов / RAM / preload: [`17`](17_AUDIO_CONTENT_ACT1.md), [`18`](18_BOSS_AND_REWARD_AUDIO.md).

---

## 2. Сетки и диалекты (не темп)

| Тег | Значение |
|---|---|
| `bpmFamily` | `tempo_120` (все Act1 must-ship + deferred packs ниже) |
| `keyFamily` | `keyfam_a_min` |
| `dialect` | Вкусовая метка клуба для briefing / club match (не отдельный clock) |

| dialect | Клуб | 2-bar identity |
|---|---|---|
| `neon_house` | Neon Basement | Чистый four-on-floor, яркий pluck / electro edge |
| `warehouse_techno` | Warehouse | Пыльный reverb, cascading 16ths, длинные линии (Mutabor-density) |
| `bass_tech` | Bass Factory | Мгновенный тяжёлый sub, sparse tops |
| `signal_industrial` | The Signal | Радио-импульс + static bed + напряжённый MUSIC |
| `dream_deep` | Dream Lounge *(deferred)* | Мягкий kick, широкие пады |
| `rave_hard` | Rave District *(deferred)* | Hardgroove attack, «строб»-стабы |

Роли стемов: **LOW** = kick/bass/sub (kill-safe); **PERC** = доля; **MUSIC** = хук/гармония. Файлы: `AudioSources/act1/<track_id>/exports/<track_id>_{LOW,PERC,MUSIC}_v01.wav` (§17).

---

## 3. Must-ship пулы (GDD Act 1)

### 3.1 Neon Basement — `club_neon_basement`

| | |
|---|---|
| Hot-set | **4 P0** (core preload) |
| dialect | `neon_house` |
| vs сосед | Ярче Warehouse, легче Bass Factory, без «сигнала» |

| id | Display | E | Mood | Роль | rarity | licenseId | status |
|---|---|---:|---|---|---|---|---|
| `track_neon_pulse` | Neon Pulse | 5 | Bright | Старт, переходы | `starter` | `LIC-001` | test (P0) |
| `track_bass_reactor` | Bass Reactor | 9 | Dark | Дроп / басовый риск | `starter` | `LIC-002` | test (P0) |
| `track_afterhours` | Afterhours | 3 | Dreamy | Контраст | `starter` | `LIC-003` | test (P0) |
| `track_city_static` | City Static | 7 | Edgy | Развитие, hard cut | `starter` | `LIC-004` | test (P0) |

Club-unique не нужны: Neon = FIRST GIG / core crate.

---

### 3.2 Warehouse — `club_warehouse_pulse`

| | |
|---|---|
| Hot-set | core ∩ mood + **2 club_unique** (§17) |
| dialect | `warehouse_techno` |
| 2-bar | Dusty reverb + rolling hats |

| id | Display | E | Mood | Роль | rarity | licenseId |
|---|---|---:|---|---|---|---|
| `track_warehouse_line` | Warehouse Line | 6 | Dusty | Rolling techno bed | `club_pool` | `PLANNED-ACT1-010` |
| `track_smoke_column` | Smoke Column | 4 | Hypnotic | Длинные drone-линии | `club_pool` | `PLANNED-ACT1-011` |

`licenseStatus`: planned. Id `track_warehouse_line` совпадает с примером §17.

---

### 3.3 Bass Factory — `club_bass_factory`

| | |
|---|---|
| Hot-set | core ∩ mood + **2 club_unique** |
| dialect | `bass_tech` |
| 2-bar | Instant sub pressure |

| id | Display | E | Mood | Роль | rarity | licenseId |
|---|---|---:|---|---|---|---|
| `track_press_line` | Press Line | 7 | Heavy | Sub-инструмент | `club_pool` | `PLANNED-ACT1-020` |
| `track_torque_drum` | Torque Drum | 6 | Mechanical | Swung perc bed | `club_pool` | `PLANNED-ACT1-021` |

---

### 3.4 The Signal — `club_the_signal` (Boss)

| | |
|---|---|
| Hot-set | **boss pack only** (P0 не подмешивать в фазы босса без явного design exception) |
| dialect | `signal_industrial` |
| 2-bar | Radio pulse + static — ни один ordinary так не звучит |
| Фазы (music) | Carrier → Breach на phrase boundary; детали preload — §18 |

| id | Display | E | Mood | Роль | rarity | licenseId |
|---|---|---:|---|---|---|---|
| `track_signal_carrier` | Signal Carrier | 7 | Uneasy | Фаза 1 + **fixed clear drop** + meta unlock | `boss_pool` + `run_reward` + `meta_unlock` | `PLANNED-ACT1-050` |
| `track_signal_breach` | Signal Breach | 10 | Critical | Фаза 2 / пик | `boss_exclusive` | `PLANNED-ACT1-051` |

Оба @ 120 / `keyfam_a_min`, чтобы Carrier после unlock жил в будущих стартовых crate. Breach остаётся exclusive боя.

---

## 4. Deferred packs (Content Bible → Act 2)

Не в GDD `act1_regular_pool`. Каталог готов, производство после must-ship.

### Dream Lounge — `club_dream_lounge`

| id | Display | E | Роль | rarity | licenseId |
|---|---|---:|---|---|---|
| `track_mint_drift` | Mint Drift | 3 | Deep bed | `club_pool` | `PLANNED-ACT2-030` |
| `track_pink_horizon` | Pink Horizon | 5 | Арп-хук | `club_pool` | `PLANNED-ACT2-031` |

2-bar: soft kick + wide pads. Всё ещё 120 / `keyfam_a_min` (или отдельный ADR, если позже захотим 118).

### Rave District — `club_rave_district`

| id | Display | E | Роль | rarity | licenseId |
|---|---|---:|---|---|---|
| `track_strobe_grid` | Strobe Grid | 8 | Hardgroove | `club_pool` | `PLANNED-ACT2-040` |
| `track_yellow_siren` | Yellow Siren | 7 | Stab identity | `club_pool` | `PLANNED-ACT2-041` |

2-bar: faster *feel* via programming (не BPM clock), hard hats, siren stab.

---

## 5. Reward slots (поддержка GDD §3.2)

Music заполняет пулы; systems крутит выбор 1-of-2 / 1-of-3.

| Pool id | Rarity band | Track ids | Sonic role |
|---|---|---|---|
| `reward_pool_act1_common` | Common / run_reward | `track_voltage_alley`, `track_mutabor_pulse` | Neon-acid opener · Warehouse density tool |
| `reward_pool_act1_mid` | Uncommon / run_reward | `track_foundry_drop`, `track_afterglow_key` | Bass weapon · Deep contrast tool |
| Boss fixed | Rare / Signature + meta | `track_signal_carrier` | См. §3.4 |

| id | Display | E | Mood | rarity | licenseId | Куда файл |
|---|---|---:|---|---|---|---|
| `track_voltage_alley` | Voltage Alley | 6 | Acid | `run_reward` | `PLANNED-ACT1-001` | `AudioSources/act1/track_voltage_alley/exports/` |
| `track_mutabor_pulse` | Mutabor Pulse | 7 | Dense | `run_reward` | `PLANNED-ACT1-012` | `…/track_mutabor_pulse/exports/` |
| `track_foundry_drop` | Foundry Drop | 10 | Brutal | `run_reward` | `PLANNED-ACT1-022` | `…/track_foundry_drop/exports/` |
| `track_afterglow_key` | Afterglow Key | 4 | Warm | `run_reward` | `PLANNED-ACT1-032` | `…/track_afterglow_key/exports/` |

Все planned. `track_afterglow_key` звучит «lounge», но остаётся Act1 reward (контраст в crate), не требует клуба Dream Lounge в маршруте.

---

## 6. Сводная таблица пулов Act 1

| Клуб | clubId | Hot pool | Extra | BPM | keyFamily | 2-bar identity | Slice |
|---|---|---|---|---:|---|---|---|
| Neon Basement | `club_neon_basement` | 4 P0 | — | 120 | `keyfam_a_min` | Clean neon house/tech | **P0** |
| Warehouse | `club_warehouse_pulse` | 2 unique | via reward pools | 120 | `keyfam_a_min` | Dusty reverb + rolling hats | **P1 must** |
| Bass Factory | `club_bass_factory` | 2 unique | via reward pools | 120 | `keyfam_a_min` | Instant sub | **P1 must** |
| The Signal | `club_the_signal` | Carrier + Breach | Carrier = meta drop | 120 | `keyfam_a_min` | Radio pulse + static | **P1 boss** |
| Dream Lounge | `club_dream_lounge` | 2 unique | — | 120 | `keyfam_a_min` | Soft pads | Act2 pack |
| Rave District | `club_rave_district` | 2 unique | — | 120 | `keyfam_a_min` | Hardgroove stabs | Act2 pack |
| Final Festival | — | — | — | — | — | — | P2+ |

**Must-ship новых композиций:** 10  
(= 2 Warehouse + 2 Bass + 4 reward-pool + 2 Signal).  
**Deferred:** +4 (Dream/Rave).  
**P0:** 4 без изменений.

---

## 7. Метаданные P1 (поверх TrackDefinition)

| Поле | Пример | Назначение |
|---|---|---|
| `dialect` | `warehouse_techno` | Вкус клуба / club match |
| `bpmFamily` | `tempo_120` | Фильтр (Act1 все равны) |
| `keyFamily` | `keyfam_a_min` | Фильтр |
| `rarity` | `starter` \| `club_pool` \| `run_reward` \| `boss_pool` \| `boss_exclusive` \| `meta_unlock` | Слот прогрессии |
| `licenseStatus` | `test` \| `planned` \| `licensed` \| `blocked` | Шип |
| `rewardPoolIds` | `["reward_pool_act1_common"]` | Членство в roll-пуле |

Флаги overlap с §18 (`reward` / `boss_drop` / `meta_unlock`) — не дублировать смысл; rarity выше = канон Music.

---

## 8. Acceptance (music)

- [ ] P0 четыре id по-прежнему только в `CreateMvp()`.
- [ ] Warehouse / Bass / Signal отличимы друг от друга и от Neon за 2 такта (blind A/B).
- [ ] Любая пара must-ship @ 120 / A_min сводится без гармонической грязи.
- [ ] LOW kill на каждом новом треке оставляет читаемый PERC+MUSIC.
- [ ] `reward_pool_*` и boss drop id совпадают с GDD §3.2.
- [ ] Ни один planned-трек не в credits как licensed OST.
- [ ] Final Festival отсутствует в манифесте Act1.
