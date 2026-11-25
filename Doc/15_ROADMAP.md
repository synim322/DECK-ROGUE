# 15. Roadmap, backlog, зависимости

> **Не сроковое обещание.** Этапы зависят от готовности оригинальной музыки и прохождения Web Audio Gate. Оценки в днях даны для **одного разработчика** как ориентир активной работы, без учёта полной музыки/арт-производства и непредвиденных проблем Web.

## Milestones

| Milestone | Работы | Оценка* | Gate |
|---|---|---:|---|
| M0 — Setup | Unity, repo, 2D сцена, платформенные пустые build | 1–2 дня | 3 target builds стартуют |
| M1 — Audio Spike | 2 деки, clock, Unity Web check, JS fallback research, looping | 5–10 дней | Audio Gate A0, ADR бекенда |
| M2 — DJ Controls | Crossfader, bass kill, queue, filters, 4 tracks | 5–8 дней | чистые переходы и слышимый контроль |
| M3 — Scoring | Transition/drop, Hype, Combo, result | 4–7 дней | playable club 1 |
| M4 — Roguelite | 2 выступления, 3 upgrades, RNG, restart | 3–6 дней | полный 5–7 мин забег |
| M5 — Art/UI | responsive HUD, touch, pixel club, crowd, onboarding | 7–14 дней | Win/Web/Android UI parity |
| M6 — QA/Polish | audio test, device matrix, balance, legal/credits | 7–14 дней | публичный playtest build |
| **M7 — Act1 → First Boss** | Пул клубов, реакция зала, music pools, reward tracks, deck depth, JRPG art, boss **The Signal** | после M6 | проходимый акт до сюжетного босса |

*Примерный суммарный объём M0–M6: 32–61 рабочих дня плюс производство музыки и тестирование устройств; **не планировать релиз по календарю до M1**. M7 **не** входит в FIRST GIG v0.1. Часть art/UI идёт параллельно после стабильной звукосистемы; контент Act1 можно проектировать параллельно, но **не мержить в P0-критический путь**.

## Dependency graph

`M0 -> M1 (Audio gate) -> M2 -> M3 -> M4 -> M5 -> M6` → **gate «приятно сводить два трека в браузере»** → `M7 (Act1 → The Signal)`.

Ветка `Music Content (12 stems)` начинается после первых двух тестовых лупов M1. Ветка `Pixel Art` может начаться после фиксации UI skeleton и reference resolution, но не должна блокировать M1. Android touch UX рисовать рано, проверять уже в M1/M2.

**Scope lock:** P0 = только FIRST GIG (1 клуб, 2 сета, 4 пластинки, 3 crowd states, 3 upgrades). Всё ниже в DJ-1xx — **P1 / M7**, даже если параллельные команды уже думают про деки/реакцию/арт.

## Backlog (приоритеты P0)

| ID | Приоритет | Задача | Зависимость | Проверка |
|---|---|---|---|---|
| DJ-001 | P0 | Bootstrap Unity & 3 empty builds | — | Win/Web/Android boot |
| DJ-002 | P0 | MusicClock, 120 BPM / 4/4 / boundary | DJ-001 | deterministic unit tests |
| DJ-003 | P0 | Native 2-deck audio/stems | DJ-002 | 5-min sync |
| DJ-004 | P0 | Web audio feasibility / ADR backend | DJ-003 | Chrome + Firefox audio gate |
| DJ-005 | P0 | Audio interruption + pause/resume | DJ-004 | tab switch / Android focus |
| DJ-006 | P0 | TrackDefinition and library | DJ-003 | 4 records loaded safely |
| DJ-007 | P0 | Crossfader + Bass Kill | DJ-003 | distinct audio changes |
| DJ-008 | P0* | Cross-platform low-pass | DJ-004 | audible spectral change |
| DJ-009 | P0 | Input adapters desktop/touch | DJ-007 | multitouch test |
| DJ-010 | P0 | Transition evaluator | DJ-007 | no double reward |
| DJ-011 | P0 | Bass conflict & drop detector | DJ-010 | unit tests |
| DJ-012 | P0 | Hype/Combo/Reputation | DJ-011 | exact deterministic outcomes |
| DJ-013 | P0 | ClubDirector and 2-session flow | DJ-012 | win/lose complete |
| DJ-014 | P0 | 3 upgrades, 2 random choice | DJ-013 | choices affect next set |
| DJ-015 | P0 | Responsive DJ HUD | DJ-009 | phone & desktop |
| DJ-016 | P0 | Pixel scene & crowd reactions | DJ-012 | readable feedback |
| DJ-017 | P0 | Tutorial, settings, accessibility | DJ-015 | no help needed |
| DJ-018 | P0 | Platform QA and release prototype | all | complete acceptance |

`DJ-008`: P0 under **ADR-approved implementation**; if Web low-pass fails feasibility, revise GDD/MVP and remove feature from *all* builds rather than release inconsistent game rules.

## Milestone M7 — Act1 → First Boss (P1)

**Цель:** один проходимый акт: 2–3 случайных обычных клуба из пула → награды между клубами → сюжетный boss-клуб **The Signal**. Столпы Vision 4–6 в миниатюре акта (dual progression, разные клубы/музыка, босс = сюжетная площадка).

**Жёсткий вход:** M6 / Audio Gate «два трека приятно сводятся в браузере» + стабильный FIRST GIG. Параллельный дизайн/музыка/арт **до** входа разрешены; интеграция в playable Act1 — только после входа.

### Backlog Act1 (DJ-1xx)

| ID | Приоритет | Задача | Зависимость | Стол | Приёмка |
|---|---|---|---|---|---|
| DJ-101 | P1 | **Act1 run spine** — Shuffle 3 ordinary → N=2 → `club_the_signal`; win/lose по реакции | M6 + DJ-013 | systems → gameplay | Seed → тот же порядок; fail ordinary обрывает акт; Hype не между клубами |
| DJ-102 | P1 | **Club pool Act1** — `club_neon_basement`, `club_warehouse_pulse`, `club_bass_factory` + пороги GDD §3; босс не в RNG | DJ-101 | systems + creative | 3 ordinary различимы briefing+особым; Signal только fixed boss |
| DJ-103 | P1 | **Club reaction + deck events** — specials + `CueReadyHonor`/`LateLoad`/`DeadAir`/`HotSwapFumble`/`CleanPrepTransition` | DJ-012, DJ-016, DJ-101, DJ-104 | systems → gameplay | ≥2 hook-типа кроме Hype; callouts; unit one-shot eventId |
| DJ-104 | P1 | **Deck depth** — phrase-locked load, mid-set crate, CueReady, dual-deck constraints (Loop Length → DJ-113) | M2/DJ-007, Audio Gate | audio + gameplay | QueuedLoad на фразе; Playing нельзя hot-load; UI читает CueReady/DeadAir; sync 5 мин |
| DJ-105 | P1 | **Music pools** — [`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md) must-ship @ 120/`keyfam_a_min`; P0 CreateMvp не трогать | DJ-006, DJ-102 | music | ≥2 пула с 2-bar identity; stems §17; planned until licensed |
| DJ-106 | P1 | **Reward tracks** — 1-of-2 common / 1-of-3 mid + boss fixed `track_signal_carrier` (+ meta); gear отдельно | DJ-014, DJ-101, DJ-105 | systems → gameplay | Не `+Hype%`; Reset снимает run-vinyl; breach не в clear; reroll дубликатов |
| DJ-107 | P1 | **JRPG art pass** — 3 ordinary + Signal plates, DJ-силуэт, crowd, свет | DJ-016, DJ-102 | art | Смена клуба без UI-текста; reduced flashing |
| DJ-108 | P1 | **Boss: The Signal** — фазы lock/surge, reaction, саунд, арт | DJ-101…107, DJ-115 | multi → gameplay | Проход по реакции; ≥2 фазы; win=Act1 clear; нет HP-бара |
| DJ-109 | P1 | **Dual-progression check** — in-set ≠ crate/gear; meta только `track_signal_carrier` unlock, без currency/+%Hype | DJ-103, DJ-106 | systems + qa | Слои не схлопываются в одну шкалу |
| DJ-110 | P1 | **Act1 playable QA** — прогон до Signal Win+Web (+Android) | DJ-108, DJ-109, DJ-115 | qa | 1 clear, 1 fail mid-act, audio smoke, Deep Mixer ≥6 actions, legal stems |
| DJ-111 | P1 | **Club requests UI** — `request_prep_b` / `request_energy_shift` / `request_swap_before_phrase` | DJ-103, DJ-104 | gameplay + ui | ≤1 окно; понятно <10с; пауза не штрафует |
| DJ-112 | P1 | **Mixer depth knobs** — Filter 0..1 + MUSIC/PERC gain per deck + Loop **1\|2\|4\|8**; `MixSnapshot` stem/filter/loop | DJ-104, Audio Gate (+ DJ-008 path) | audio → gameplay | Слышны filter / PERC / MUSIC; loop quantize start; P0 path без knobs целый |
| DJ-113 | P1 | **Loop Length** — toggle `8 ↔ 4` bars; apply на phrase (8) / bar boundary (4); QUEUED overwrite | DJ-104 | audio → gameplay | Слышимый короткий/длинный луп с границы; спам в фразе = 1 apply, без score |
| DJ-114 | P1 | **Mixer expression scoring** — `FilterPhrase`, `StemPunch`, `LoopSnap`; CrowdMotion tier; антифарм ≥1 bar | DJ-103, DJ-112 | systems → gameplay | Unit: filter/stem/loop/spam/CrowdMotion; ≤1/kind/phrase; Hype без FPS-зависимости |
| DJ-115 | P1 | **Crowd impulse visual** — JRPG-cheer 0..3 на 1–2 бара (clean / drop / filter); density/bob/arms | DJ-016, DJ-103, DJ-114 | art → client | Толпа двигается на clean+drop+filter; reduced flashing; **пакет:** ≥6 различимых слышимых mix-действий; Win+Web smoke |
| DJ-117 | P1 | **Deck quality gestures** — PhraseStart `phrase_start` (Hype +2 / Rep +25 / Nod, ≤1/фраза/дека), BassHandoff `bass_handoff` (Hype +3 / Rep +35 / Nod, ≤1/фраза; Drop в том же Observe не платит), LineLift `line_lift` LINE 0..1 (Hype +3 / Rep +40 / Bounce, ≤1/фраза/дека); MixSnapshot, только Performing; один eventId — одна награда; числа — гипотезы плейтеста; линейный фейдер по умолчанию 1 | DJ-114 | systems → audio + gameplay → client → qa | EditMode на три kind + антифарм; LINE слышен и по умолчанию 1; P0 без линейного фейдера целый |
| DJ-118 | P1 | **Ночь кампании** — 3 акта × (3 shuffle ordinary + 1 boss) = 12 клубов; сеты 1824 с = 30:24 на clock 120 (бар = 2 с); `signal_clear` и `throne_clear` продолжают ночь, `campaign_clear` только у Festival; pass поверх вкуса с индекса ≥ 1; слот 0 = common акта, слоты 1–2 = mid (до 3) + одна невзятая аура акта; боссы Signal Carrier / Static Crown / Festival Anthem в crate и мету; cap 16; аура меняет Hype только у доминирующей деки. Гипотеза плейтеста. P0 `ClubDirector` не трогать: это длина P1, не новый audio clock. Канон: [`01_GDD.md`](01_GDD.md) §3.5 | DJ-101, DJ-106, DJ-116, DJ-117 | systems → gameplay | EditMode: маршрут 12 клубов; сумма сетов 1824 с; Signal продолжается (`signal_clear`); Festival кончает ночь (`campaign_clear`); новые pass; Hype новых аур. Тесты FIRST GIG зелёные |
| DJ-120…125 | P1 | **Дневной хаб** — выплата за завершённый сет (Win `15+n/5`, Lose `2+n/10`, ключ один раз), деки `deck_starter` / `deck_filter` 60 / `deck_stems` 120 (черновая без фильтра и стемов, Срез только фильтр, Полосы только стемы), `gear_spare_leash` 40 без скоринга и без прогулки, прогулка +4 за день хаба. Не +% Hype. Вне FIRST GIG, пороги 35/60/65 не двигаются. Канон: [`01_GDD.md`](01_GDD.md) §5.1 | DJ-116 | gameplay → client | EditMode: выплата один раз; покупка и надевание; прогулка +4 и отказ; миграция v4 снова открывает coach и хранит треки |

**Пакет Mixer depth + living crowd (DJ-112…115):** не P0. FIRST GIG без knobs/CrowdMotion-impulse. ≥6 audible actions = Play/Stop + Crossfader + Bass Kill + Filter + PERC + MUSIC (+ Loop 1\|2\|4\|8). EQ3 / FX / Hot Cue / per-stem filter — не в этом срезе. Канон: [`01_GDD.md`](01_GDD.md) §3.4. DJ-117 добавляет к сводке жестов PhraseStart, BassHandoff и LINE, все три кормят CrowdMotion и Hype.

**DJ-116** в эту таблицу не ставится: id занят срезом [`20_GAMESUP42_SLICE.md`](20_GAMESUP42_SLICE.md). Следующий свободный после DJ-117 — **DJ-118** (ночь кампании, не пакет микшера и не FIRST GIG).

### Зависимости Act1 (кратко)

```
M6 ──► DJ-101 (spine) ──► DJ-102 (pool) ──┬──► DJ-105 (music) ──► DJ-106 (reward tracks)
         │                                 │
         ├──► DJ-104 (deck depth) ──┬──► DJ-103 (reaction+deck events) ──► DJ-111 (request UI)
         │                          │         │
         │                          ├──► DJ-112 (knobs) ──► DJ-114 (expr score) ──► DJ-115 (crowd impulse)
         │                          └──► DJ-113 (loop length) ─────────────┘
         │
DJ-016 ──► DJ-107 (JRPG art) ─────────────────────────────────────────────┐
                                                                          ▼
                                                                DJ-108 (The Signal)
                                                                          │
                                                                DJ-109 → DJ-110 (QA)
```

Параллель после входа в M7: **music** (DJ-105), **systems** (DJ-101…104, 106, 109, 111–114), **art** (DJ-107, 115). Сшивка — DJ-108/110. Deep Mixer **после** DJ-104; scoring/impulse **после** DJ-103.

Ночь 12 клубов (Act2–3, Festival) больше не безымянный P2: это строка **DJ-118** в бэклоге выше. Один boss на акт. P0 и audio clock она не двигает.

### Сознательно отложено (не Act1 / не раздувать P0)

| Что | Куда | Почему |
|---|---|---|
| Echo / Reverb / FX pack, Hot Cue suite, EQ3, per-stem filter | P1.5 / после DJ-112…115 | Deep Mixer must-ship = knobs+loop+expr+cheer; Web FX только после отдельного ADR |
| Широкие persistent unlocks / mid-run save | P1.5+ | Shell уже пишет Settings + `track_signal_carrier` meta + BestReputation; широкий meta gear/club — позже. Дневной хаб DJ-120…125 — плоские монеты, не +% Hype |
| Desktop Safari, localization polish, long-session balance | polish backlog | Не блокируют «дошли до Signal» |
| Time-stretch / manual BPM, harmonic mixing, user music | P2 | Vision: умные упущения |
| Cloud saves, leaderboards, iOS, сеть | P2 | После проверки аудитории |
| Elite / Record Shop / Event nodes | позже P1 | Опциональный сахар маршрута |
| Расширение P0 до «уже как Act1» / Deep Mixer в FIRST GIG | **запрещено** | FIRST GIG остаётся 1 клуб / 2 сета / 4 пластинки / базовый пульт |

Старые однострочники `DJ-101…110` (map/FX/Safari/…) **заменены** этой таблицей (`DJ-101…115`, `DJ-117`, `DJ-118`). `DJ-116` живёт в [`20_GAMESUP42_SLICE.md`](20_GAMESUP42_SLICE.md) и в таблицу не дублируется. Safari/FX/meta живут в отложенном списке выше, без отдельных ID до выхода Act1.

## Review rhythm

- Перед задачей: коротко сформулировать цель, контракт и acceptance.
- После задачи: unit tests/manual tests + короткий отчёт и фиксация документации.
- Раз в milestone: playable build с коммит-хешем и списком известных проблем.
- Не приступать к **интеграции** M7 до прохождения критерия «можно приятно сводить два трека в браузере».
- Не тащить DJ-1xx в Definition of Done FIRST GIG.
