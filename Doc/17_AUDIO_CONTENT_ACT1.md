# 17. Audio Content — Act 1 ordinary clubs (tech contract)

> Техспека стемов и лупов для **обычных клубов до первого босса**. Канон синхронизации и гейта: [`06_AUDIO_TECH.md`](06_AUDIO_TECH.md). Производственный пайплайн MVP: мастер-док §13 Audio Content Pipeline. **Track ids / dialects / reward pools / права:** [`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md) (music-supervisor). Этот документ — инженерный контракт файлов, памяти и loop-качества.

**Срез Act 1 (реалистичный):** `Neon Basement` (вход/MVP) → пул обычных `Warehouse` · `Bass Factory` · `Dream Lounge` · `Rave District` → **до** boss `The Signal`. Босс-контент и Final Festival сюда не входят.

---

## 1. Техспека стема (обязательная)

### 1.1 Grid / timing

| Параметр | Норма Act 1 |
|---|---|
| BPM | `120` fixed (ADR-002) |
| Meter | `4/4` |
| Bars per loop | `8` |
| Duration | **ровно** `16.000 s` = `768_000` samples @ 48 kHz |
| Beat / bar / phrase | `0.5 s` / `2 s` / `16 s` |
| Phase origin | audio clock провайдера (`dspTime` / WebAudio), не кадр |

Все три стема одной пластинки стартуют с **одного sample 0** и имеют **одинаковую длину в сэмплах**. Scheduler играет через `PlayScheduled` / эквивалент; один авторитетный backend на платформу.

### 1.2 Sample format

| Параметр | Норма | Примечание |
|---|---|---|
| Container | **WAV** PCM (не AAC/MP3 как source-of-truth) | AAC loop glitch на Web — риск из Unity notes |
| Sample rate | **48 000 Hz** | совпадает с `GenerateTestLoops` и §13 |
| Bit depth (master) | 24-bit или float32 в DAW; delivery 16-bit PCM OK для runtime | 16-bit уже в тестовом генераторе |
| Channels | **LOW: mono**; **PERC: mono**; **MUSIC: stereo** предпочтительно | mono режет RAM ~2× на LOW/PERC; MUSIC держит ширину |
| Interleave / endian | standard little-endian PCM WAV | без кастомных chunk'ов, кроме опционального `smpl` loop |

Оценка PCM RAM (декомпресс):  
`16 s × 48 kHz × bytesPerSample × channels`.  
Mono 16-bit stem ≈ **1.5 MB**; stereo ≈ **3.0 MB**. Три стема трека ≈ **6 MB** (mono+mono+stereo). Две активные деки ≈ **12 MB** PCM + overhead decode/Unity.

### 1.3 Gapless / loop boundary

1. Sample 0 и последний sample всех трёх стемов — **near-zero** (после микрофейда).
2. Хвост: линейный fade **последние ≤12 ms** (как в `GenerateTestLoops`), без обрезания mid-waveform.
3. Никаких «хвостов» за пределами 16.000 s — реверб/delay либо умещаются в луп с фазовой стыковкой, либо обрезаны композитором осознанно.
4. Опционально: WAV `smpl` loop marker `0 … N-1` для native; **не считать маркером гарантией** gapless на Web.
5. Приёмка: **50** последовательных границ лупа без системного gap/click (Gate A0); целевой drift стемов ≤ **~20 ms** за 5 минут.
6. Runtime source для Web — WAV или проверенный non-AAC path; если платформа перекодирует в AAC — нужен отдельный QA-прогон или WebAudio own buffers.

### 1.4 Loudness / headroom targets

Ориентиры калибровки (согласованы с пик-скейлом тестового генератора; финальные LUFS утверждает music-supervisor + инж. check на двух деках):

| Stem | Peak target (full-scale abs) | Роль в миксе |
|---|---:|---|
| `LOW` | **0.50** | kick + sub/bass; громче остальных |
| `PERC` | **0.12–0.18** | hats/clap; не давит LOW |
| `MUSIC` | **0.28–0.40** | pad/lead; слышен при bass kill |

Дополнительно:

- Сумма трёх стемов одной пластинки **без** клиппинга.
- Пара двух пластинок через equal-power (`gA=cos(πx/2)`, `gB=sin(πx/2)`, mid ≈ 0.707): **pair peak ≤ 1.0** (лучше ≤ 0.95) — как `PairPeak` в генераторе.
- Master headroom: stems уже с запасом; не нормализовать «в ноль» каждый файл отдельно без проверки пары.
- Integrated loudness (рекомендация, не блокер v0): целевой **примерно −14 … −12 LUFS** на сумме одной пластинки; итоговый микс двух дек не должен упираться в 0 dBFS.
- Bass kill: только gain на `LOW`, fade **10–30 ms**; PERC+MUSIC остаются.

### 1.5 Stem roles (инженерный смысл)

| Stem | Содержит | Не содержит | Gameplay dependency |
|---|---|---|---|
| **LOW** | kick, bass, sub | melodic leads, open hats | Bass Kill; Bass Conflict (`gain_low` обеих дек) |
| **PERC** | hats, snare/clap, shakers, short fills | kick/sub foundation | Groove при выключенном LOW |
| **MUSIC** | chords, pad, lead, atmosphere | kick/bass body | Drop/атака «возврата» энергии; трек не должен «умирать» без LOW |

Три стема = одна пластинка (`TrackDefinition`). Нельзя подменять stem другой пластинки на той же деке без смены всего track id (кроме явного shared-asset, см. §2).

---

## 2. Разнообразие без раздувания RAM

### 2.1 Бюджет Act 1 (обычные клубы)

| Слой | Треки × stems | Политика загрузки |
|---|---|---|
| **Core pool (Neon / starter)** | 4 × 3 = **12** (MVP: Neon Pulse, Bass Reactor, Afterhours, City Static) | **Preload** на boot/after AudioUnlock |
| **Club-unique packs** | +2 трека на каждый из 4 обычных клубов = **+8 × 3 = 24** | **Decode on club enter**; выгрузка при уходе |
| **Shared optional** | до **4** переиспользуемых PERC/ambient beds (см. правила) | Preload если входят в ≥2 клуба маршрута |

Итого файлов authoring ≈ **36–40** стемов; **в RAM одновременно** — core 12 + текущий клуб (до 6) + optional warm next (до 6) ≈ **18–24** буфера, не вся Act 1 библиотека.

PCM оценка (mono/mono/stereo): ~6 MB/track → hot set **~36–48 MB** + decode overhead. Полный Act 1 decode-all ≈ **~100+ MB** — **запрещён** на Web/mid Android.

### 2.2 Preload vs streaming

| Режим | Когда | Что |
|---|---|---|
| **Preload / DecompressOnLoad** | Core 4 пластинки; активные 2 деки | Нужен мгновенный `ScheduleStart` на границе доли/такта/фразы |
| **Warm decode** | PreClub следующего клуба / конец сета | Async decode club pack; UI не стартует Performing, пока hot stems ready |
| **Streaming (`stream=true`)** | **Не** для активных игровых стемов P0 | Unity Web ограничивает stream create; риск late read на loop. Допускается только для **не-лупового** SFX/ambience вне сетки, если отдельно прогнано |
| **Unload** | Выход из клуба / смена маршрута | `Dispose`/unload club-unique; core остаётся |

Правило: всё, что крутится на деке под `PlayScheduled` + gapless loop — **полностью в памяти** к моменту schedule. Streaming не спасает разнообразие Act 1; спасает **пул + unload**.

### 2.3 Shared stems — правила

Разрешено:

1. **Один WAV → несколько `TrackDefinition`**, если metadata явно указывает `stemRef` на shared asset (один decode, много id).
2. Shared только **PERC** или короткий **MUSIC bed**, и только если phase/length идентичны контракту 16.000 s @ 120.
3. Shared asset **нельзя** одновременно играть с двух дек как разные «логические» треки, если это один buffer без раздельных gain path — на практике: две деки = два playback instance / два `AudioSource`, один clip asset OK.
4. **LOW никогда не shared** между разными track id в Act 1: bass identity + Bass Conflict завязаны на уникальный low-end пластинки.

Запрещено:

- «Склеивать» разнотемповые/разнотональные куски в один shared stem.
- Держать filtered-duplicate stems (low-pass bake) без ADR — фильтр P0 только после явного решения; иначе нет ни на одной платформе.
- Двойной плейбек одного стема из Unity **и** Web Audio.

### 2.4 Разнообразие на маршруте

- Neon Basement: только core 4 (или subset seed).
- Warehouse / Bass Factory / Dream Lounge / Rave District: **core ∩ preferredMood/genre** + **2 club-unique**.
- Случайный порядок обычных клубов не требует держать все unique packs в RAM — только текущий (+ warm next).
- Повтор пластинки на другой площадке — OK; ощущение новизны даёт club-unique + правило сета, не полный decode библиотеки.

---

## 3. Контракт файлов и папок

### 3.1 Authoring layout (вне / рядом с Unity)

```text
AudioSources/
  act1/
    _shared/                          # optional shared stems
      exports/
        shared_perc_four_on_floor_v01.wav
    track_neon_pulse/
      session.*                       # DAW, не в StreamingAssets
      exports/
        track_neon_pulse_LOW_v01.wav
        track_neon_pulse_PERC_v01.wav
        track_neon_pulse_MUSIC_v01.wav
      manifest.json                   # см. ниже
    track_warehouse_line/
      exports/
        track_warehouse_line_LOW_v01.wav
        ...
    clubs/
      club_neon_basement.json         # trackPoolIds, preferredMoods
      club_warehouse.json
      club_bass_factory.json
      club_dream_lounge.json
      club_rave_district.json
```

### 3.2 Runtime Unity layout (предложение)

```text
DJ/Assets/_Game/Audio/
  Resources/Tracks/                   # или Addressables — решение инж., не контента
    track_neon_pulse/
      track_neon_pulse_LOW_v01.wav
      track_neon_pulse_PERC_v01.wav
      track_neon_pulse_MUSIC_v01.wav
  Resources/TestLoops/                # только synth gate / CI; не боевой саунд
```

Имена файлов: `{track_id}_{STEM}_v{NN}.wav` где `STEM ∈ {LOW, PERC, MUSIC}`, `track_id` = `snake_case` с префиксом `track_`.

### 3.3 `manifest.json` (на трек)

```json
{
  "id": "track_warehouse_line",
  "displayName": "Warehouse Line",
  "act": 1,
  "bpm": 120,
  "beatsPerBar": 4,
  "barsPerLoop": 8,
  "sampleRate": 48000,
  "lengthSamples": 768000,
  "key": "A_minor",
  "genre": "Techno",
  "energy": 6,
  "mood": "Dark",
  "clubs": ["club_warehouse"],
  "stems": {
    "low": { "file": "track_warehouse_line_LOW_v01.wav", "channels": 1, "peakTarget": 0.50 },
    "perc": { "file": "track_warehouse_line_PERC_v01.wav", "channels": 1, "peakTarget": 0.16 },
    "music": { "file": "track_warehouse_line_MUSIC_v01.wav", "channels": 2, "peakTarget": 0.32 }
  },
  "sharedRefs": [],
  "licenseId": "LIC-TBD",
  "checksum": { "algo": "sha256", "files": {} },
  "reviewer": "",
  "status": "wip"
}
```

**Validation (инженерия / CI):**

- `lengthSamples == 768000` на каждом stem (±0).
- `sampleRate == 48000`.
- peak abs ≤ 1.0; soft-fail если LOW/PERC/MUSIC вне коридора §1.4.
- pair-peak с каждым track из `compatibleWith` (или со всем core) ≤ 1.0 @ equal-power mid.
- `licenseId` обязателен до ship.
- Три файла существуют; channels совпадают с манифестом.

### 3.4 Club pool contract

```json
{
  "id": "club_bass_factory",
  "act": 1,
  "tier": "ordinary",
  "trackPoolIds": [
    "track_bass_reactor",
    "track_neon_pulse",
    "track_bass_factory_press",
    "track_bass_factory_line"
  ],
  "preloadPolicy": "core_plus_club",
  "preferredMoods": ["Dark"],
  "preferredGenres": ["TechHouse"]
}
```

`preloadPolicy`: `core_only` | `core_plus_club` | `explicit_list`. Hot set = пересечение pool ∩ policy.

### 3.5 Ориентир клубов → саунд (тех, не лор)

| Club | Жанровый уклон (из Content Bible) | Упор стемов | Unique tracks (бюджет) |
|---|---|---|---|
| Neon Basement | Tech / house groove | сбалансированный core | 0 unique (MVP 4) |
| Warehouse | techno, длинные линии | LOW глубже, MUSIC длинные pads | 2 |
| Bass Factory | tech house, мощный low | LOW плотнее; аккуратный pair-peak | 2 |
| Dream Lounge | deep / hypnotic | MUSIC+PERC мягче, energy↓ | 2 |
| Rave District | harder techno, дропы | PERC/MUSIC атака; drop-friendly phrasing | 2 |

Имена unique-треков утверждает music-supervisor; инженерия принимает только id + манифест + WAV.

---

## 4. Music-supervisor vs инженерия

| Зона | **Music-supervisor** | **Инженерия (audio-engineer)** |
|---|---|---|
| Сетка BPM/bars/length | Соблюдает контракт; не предлагает free-BPM в Act 1 | Валидирует 16.000 s / sample counts |
| Жанр, mood, energy, «вкус клуба» | Задаёт таблицы, бриф композитору | Пишет поля в `TrackDefinition` / pool |
| Мелодия, аранжировка, совместимость тональностей | Утверждает fun-test и harmonic OK | Даёт tooling pair-peak / loop QA |
| Громкость «на слух», LUFS-финал | Утверждает художественный баланс | Фиксирует peak corridors, headroom, CI |
| Права / лицензии / sample sources | Владеет `licenseId` и доказательствами | Блокирует import без `licenseId` в ship |
| Stem split LOW/PERC/MUSIC | Следит за bleed и musical intent | Требует раздельные файлы + bass-kill test |
| Shared stems | Решает, *что* можно шарить музыкально | Решает decode/refcount/`stemRef` |
| RAM / preload / unload | Не раздувает заказ «30 треков сразу» | Политика hot set, warm, unload |
| Web AAC / gapless | Не отдаёт AAC как master | Gate A0, WAV path, backend choice |
| Low-pass / filter banks | Не рисует UI-эффект «в вакууме» | Только после ADR; иначе нет на всех платформах |
| Босс The Signal / сюжетные фазы | Отдельный бриф позже | Вне этого документа |
| GenerateTestLoops / synth stubs | Не путает с боевым саундом | CI/gate до реальных мастеров |

**Content gate:** не штамповать 8 club-unique до прохождения Audio Gate A0 на core/test loops и первого fun-test на Neon Basement.

---

## 5. QA checklist (контент Act 1)

На каждый новый track:

1. Solo sum LOW+PERC+MUSIC — полноценный грув.
2. Bass kill 1–2 bars — форма жива.
3. Cross-mix с ≥3 пластинками core — безHarmonic грязи (supervisor) + pair-peak (eng).
4. 50 loop boundaries на Win + Chrome (+ Android native когда доступен).
5. Две деки, equal-power mid, 30+ с — без клипа.
6. Manifest checksum + `licenseId`.
7. Club enter: decode ≤ приемлемого PreClub wait; после exit — unload unique (профилировщик RAM).

Ручной прогон платформ после смены пакета: **Windows player**, **Chrome WebGL**, **Android native** — те же границы лупа и bass kill, что в Gate A0.

---

## 6. Связанные доки

- [`06_AUDIO_TECH.md`](06_AUDIO_TECH.md) — sync, backends, Gate A0, RAM estimate.
- [`DECK_ROGUE_MASTER.md`](DECK_ROGUE_MASTER.md) — Content Bible (клубы), §13 pipeline, TrackDefinition schema.
- `tools/GenerateTestLoops` — reference: 48 kHz, 16 s, mono WAV, peak targets, 12 ms tail fade, A minor test set.
