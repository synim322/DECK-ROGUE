# 18. Boss & Reward Audio — The Signal, награды, preload

**Владелец:** audio-engineer · **Статус:** design P1 (после Audio Gate A0) · **Дата:** 2026-11-23  
**Связано:** [`00_VISION.md`](00_VISION.md), [`06_AUDIO_TECH.md`](06_AUDIO_TECH.md), [`16_MUSIC_ACT1.md`](16_MUSIC_ACT1.md) (Signal track ids / rarity), [`DECK_ROGUE_MASTER.md`](DECK_ROGUE_MASTER.md) (§05 клубы, §13 pipeline, §20 TrackDefinition)

> Код в этом документе не описывается как реализация. Контракт для content + backend + library. MVP (Neon Basement, 4 пластинки) **не требует** boss/reward packs; этот файл — целевая форма P1+.

---

## 1. Цель

Зафиксировать, как **boss-клуб The Signal** и **специальные пластинки после клуба** живут в той же аудиосистеме, что и обычный сет:

- две деки × `LOW` / `PERC` / `MUSIC`;
- один авторитетный источник звука на платформу;
- `MusicClock` от DSP/`AudioContext`, не от FPS;
- без второго параллельного «босс-движка» времени.

Отличие босса и наград — в **контенте, фазах сета, флагах библиотеки и политике preload/unload**, а не в другой сетке BPM.

---

## 2. Жёсткий контракт часов (не ломаем)

| Параметр | Значение | Источник |
|---|---|---|
| BPM | **120** | ADR-002, `MusicClock.ReferenceBpm` |
| Meter | **4/4** | `BeatsPerBar = 4` |
| Phrase (игровой boundary) | **8 bars = 16 s** | `BarsPerPhrase = 8` |
| Beat / Bar | 0.5 s / 2 s | `60/BPM`, ×4 |
| Scheduling | команда с id; опоздание → следующий beat/bar/phrase с учётом `schedulingLeadTime` | `06_AUDIO_TECH` |

**Правило:** boss-сет и reward-пластинки **не меняют** глобальный BPM, meter и длительность игровой фразы `MusicClock`. Scoring, Hype decay по фразе, drop на phrase boundary, resume после interruption — всё продолжает считать от той же сетки.

### 2.1 Что можно менять без трогания часов

| Рычаг | Да / нет | Комментарий |
|---|---|---|
| Тембр / энергия / плотность аранжировки | Да | Фирменный саунд The Signal |
| Набор пластинок клуба (boss pool) | Да | Отдельный pool, не RNG обычных клубов |
| Фазы сета (какие stems/треки «разрешены») | Да | Логика клуба / set director, не clock |
| `barsPerLoop` у клипа (= длина одного loop-файла) | Да, с оговоркой | См. §3.3; **игровой** phrase boundary остаётся 8 bars |
| Отдельный BPM / 3/4 / 6/8 / 16-bar phrase в clock | **Нет в P1** | Только через явный ADR (§2.2) |
| Time-stretch / pitch-shift mid-set | Нет | Вне scope; ломает A0 и совместимость |

### 2.2 Исключения (только явным ADR)

Любое из ниже требует **нового ADR** + пересмотр scoring/UI timeline + повторный Audio Gate:

1. **BPM ≠ 120** на boss-клубе.
2. **Игровая фраза ≠ 8 bars** (например, phrase = 16 bars для «эпических» границ).
3. **Смена BPM mid-set** между фазами босса.
4. Отдельный clock instance на босса, пока идёт обычный run clock.

До принятия ADR: композитор и level design подстраивают аранжировку под 120 / 4/4 / 8-bar phrase. Характер The Signal достигается **фазами и стем-паками**, не сменой сетки.

---

## 3. Boss-набор: The Signal

### 3.1 Продуктовый смысл (из vision)

Boss-клуб — **сюжетная площадка**, не HP-файт. Проход = реакция зала. Саунд должен читаться как пик акта: узнаваемый на слух, жёстче правила/фазы, чем у обычного клуба.

Рабочее имя клуба: **The Signal**. Final Festival — отдельный boss/финал с похожим техническим каркасом (несколько фаз), отдельный content pack.

### 3.2 Как саунд отличается от обычного клуба

Обычный клуб: один совместимый pool пластинок, относительно стабильный вкус на весь сет.

**The Signal** отличается тремя слоями:

1. **Content pack** — отдельный набор пластинок / stem-вариантов с общим sonic identity (сигнал, индустриальный edge, характерный MUSIC-мотив). Не «ещё четыре house-лупа».
2. **Set phases** — смена *доступного* и *ожидаемого* материала по ходу сета (см. §3.4). Часы не перезапускаются; меняется library window + club rules / crowd taste.
3. **Gameplay constraints** — особые запросы зала и фазы реакции (владелец: systems/GDD). Аудио лишь обеспечивает слышимые границы фаз (drop на phrase, смена пластинки на bar/phrase, без щелчка).

Не требуется четвёртый stem-тип. Остаются `LOW` / `PERC` / `MUSIC`. Bass kill и equal-power crossfade без изменений.

### 3.3 Loop length vs phrase length

| Термин | Кто владеет | P1 default |
|---|---|---|
| **Phrase** (игровой boundary) | `MusicClock` | всегда 8 bars / 16 s |
| **Loop asset** (`barsPerLoop` в TrackDefinition) | клип / scheduler | **предпочтительно 8 bars** |

**Рекомендация P1:** все boss stems = 8 bars, как MVP. Scheduler лупит на границе клипа; scoring слушает phrase = тот же размер → одна ментальная модель.

**Допустимое исключение без ADR по BPM:** клип длиной **N×8 bars** (16, 24, …), кратный игровой фразе, при том что:

- BPM = 120, 4/4;
- все три stems одной пластинки одной длины;
- `PlayScheduled` / loop seam на кратной phrase-границе;
- UI/scoring по-прежнему эмитят `Phrase` каждые 8 bars.

Не допускать в P1 произвольные 7/9/12 bars или «фраза босса = 16 bars в clock».

Если композитору нужен long-form без кратности 8 — **стоп**, эскалировать в ADR (§2.2).

### 3.4 Фазы босса (аудио-модель)

Фазы — **логическое состояние сета**, привязанное к audio clock (bar/phrase index от origin сета), не к wall-clock и не к FPS.

Пример каркаса The Signal (числа уточняет design; аудио фиксирует *механику*):

| Phase id | Ориентир длины | Слышимое поведение | Library window |
|---|---|---|---|
| `signal_probe` | ~2–3 phrases | Сдержанный groove, читаемый «сигнал» в MUSIC | Starter boss records (низкая–средняя energy) |
| `signal_lock` | ~3–4 phrases | Плотнее PERC/LOW; зал ждёт чистые переходы / drop | Полный boss pool акта |
| `signal_surge` | ≥2 phrases | Пик энергии; возможен **boss_drop** stem-set или смена MUSIC-слоя | Peak + optional `boss_drop` flagged tracks |

**Технические правила смены фазы:**

1. Переключение phase **только** на будущей `Bar` или `Phrase` boundary с lead time (как любая schedule-команда; свой command id).
2. Смена phase **не** обязана перезагружать играющие stems в тот же кадр: либо crossfade на другую деку уже preload’нутым треком, либо stem-gain automation внутри текущего loop, либо schedule stop/start на границе.
3. Не стартовать второй экземпляр того же stem pack параллельно (нет double playback Unity+Web).
4. После `Suspend`/`Resume` (Android call, tab hide): origin/resync по общим правилам `06_AUDIO_TECH`; phase index пересчитывается от сохранённого musical position, не «догоняется» в фоне на Web.

### 3.5 Boss stems — варианты поставки

| Вариант | Описание | Когда |
|---|---|---|
| **A. Отдельные TrackDefinition** | Новые `track_signal_*` в boss pool | Default P1 |
| **B. Phase alternate MUSIC** | Тот же id пластинки, доп. файл `MUSIC_phase2` (опционально позже) | Только если library API явно поддерживает variant; иначе = отдельные track id |
| **C. Pre-rendered filtered** | Запасной путь для Web без Biquad | Только по ADR фильтра; не путать с boss identity |

Для P1 предпочтителен **вариант A**: ясные id, checksums, preload, unload.

### 3.6 Совместимость и права

- Boss pack совместим **внутри себя** (как MVP-библиотека). Смешение boss↔обычный mid-run — по правилам design; технически stems всё равно 120/4/4/8.
- Licensing / interactive use — как в pipeline §13; отдельный `licenseId` на каждый track.
- Music-supervisor утверждает sonic bible The Signal; этот документ не задаёт мелодию/hype-числа.

---

## 4. Спец-треки «за конец уровня»

После победы в клубе (не финал) игрок может получить **новую пластинку** как run-reward или meta-unlock. Аудио должно выдать её без щелчков, OOM и без поломки уже идущего (или только что закончившегося) транспорта.

### 4.1 Формат ассета

Тот же стандарт, что MVP / §13 pipeline:

| Поле | Норма |
|---|---|
| Container | **WAV PCM** source (48 kHz, stereo; bit depth по pipeline). Не AAC как master loop source |
| Stems | ровно 3: `LOW`, `PERC`, `MUSIC` |
| Grid | 120 BPM, 4/4, **8 bars / 16.000 s** (или кратно 8 bars per §3.3) |
| Naming | `track_<id>_<STEM>_vNN.wav` |
| Manifest | id, bpm, barsPerLoop, key, genre, energy, mood, stem paths, checksums, licenseId, **flags** |

UI-SFX и one-shot riser для карточки награды — **не** TrackDefinition; отдельные короткие clips, не в stem graph дек.

### 4.2 Flags в TrackDefinition

Расширение контракта данных (§20 master). Flags — набор ортогональных меток (bitset / string list). Отсутствие flags = обычная club/run пластинка.

| Flag | Смысл | Когда выдаётся / доступна |
|---|---|---|
| *(none)* | Обычный club/run track | Starter library, club pool |
| `reward` | Может выпасть как **run reward** после клуба | Reward roll; живёт до конца забега (сброс с Reset) |
| `boss_drop` | Контент пика / signature drop The Signal (или аналог) | Boss pool / surge phase; обычно **не** в обычном RNG клубов |
| `meta_unlock` | Постоянная разблокировка между забегами | Save `UnlockedTrackIds`; не путать с run inventory |

**Допустимы комбинации**, например:

- `reward` + совместимый genre — типичная карточка «новая пластинка»;
- `boss_drop` alone — только в Signal surge / scripted beat;
- `reward` + `meta_unlock` — «получил в этом забеге и открыл навсегда» (два слоя прогрессии: run inventory **и** meta list);
- `boss_drop` + `meta_unlock` — редкий трофей после победы над боссом.

**Запрещено подразумевать:**

- `boss_drop` ⇒ другой BPM;
- `meta_unlock` ⇒ файл уже в RAM;
- `reward` ⇒ автозапуск на обеих деках без жеста/команды.

Пример манифеста (концепт, не сериализация Unity):

```json
{
  "id": "track_signal_carrier",
  "displayName": "Signal Carrier",
  "bpm": 120,
  "beatsPerBar": 4,
  "barsPerLoop": 8,
  "key": "A_minor",
  "genre": "Industrial Techno",
  "energy": 8,
  "mood": "Signal",
  "flags": ["boss_drop", "meta_unlock"],
  "stems": {
    "low": "audio/track_signal_carrier_LOW_v01",
    "perc": "audio/track_signal_carrier_PERC_v01",
    "music": "audio/track_signal_carrier_MUSIC_v01"
  },
  "licenseId": "LIC-SIGNAL-001",
  "bytesHintPcm": 9200000
}
```

`bytesHintPcm` — опциональная подсказка для budgeter’а (§5); не замена профилированию.

**Validation (добавок к §20):**

- `flags` ⊆ {`reward`, `boss_drop`, `meta_unlock`};
- `boss_drop` треки перечислены в `ClubDefinition` boss pack или phase table;
- `reward` треки входят в reward table rarity/tags;
- `meta_unlock` требуют стабильный id в save schema.

### 4.3 Когда слышно / когда только в библиотеке

| Момент | Аудио поведение |
|---|---|
| Экран Reward (между клубами) | Транспорт сета остановлен/suspend; опционально короткий UI confirm SFX; **не** крутить полные 3 stems в фоне на Web зря |
| Игрок взял пластинку | Metadata в run library сразу; **PCM decode** — по preload policy (§5), не обязательно до confirm |
| Следующий клуб briefing | Prewarm выбранных + predicted loadout |
| Boss surge `boss_drop` | Preload до входа в phase (см. §5.3) |

---

## 5. Preload / unload и память (Web / Android)

### 5.1 Бюджет (ориентир из 06 / master)

Оценка PCM одной пластинки (3 stems × 16 s × 48 kHz × stereo × 4 bytes ≈ **~27.6 MB** raw float/PCM order-of-magnitude; в master для 4×3 ≈ 73.7 MB). Реальный decode + resample + backend overhead выше на Web.

**Правило продукта:** не держать «десятки stem packs» decoded одновременно. Mid-run выдача пластинок без policy = риск R-05 (memory Web).

### 5.2 Состояния ассета

```text
CatalogOnly → Downloading/Reading → Decoding → Ready → (Playing) → Evictable → Released
```

- **CatalogOnly:** TrackDefinition + flags в памяти, stems на диске/CDN не decoded.
- **Ready:** можно `ScheduleStart` без ожидания decode.
- **Evictable:** не на деках, не в ближайшем predicted window; кандидат на unload.
- **Released:** буферы отданы; повторный play = снова decode.

### 5.3 Preload rules

| Контекст | Что держать Ready | Что нельзя |
|---|---|---|
| Старт забега / MVP | Starter library (≤4 records) после user gesture (Web) | Весь future reward catalog |
| Обычный клуб | Pool клуба ∩ run inventory, плюс 2 слота на деках | Все meta tracks |
| Перед boss The Signal | Boss pack фазы `probe`+`lock`; **до surge** — ещё и predicted `boss_drop` set | Весь meta catalog «на всякий» |
| Reward screen | CatalogOnly новых кандидатов; decode **выбранной** (и опционально 2nd offer, если хватает budget) | Decode всех rolled + всех невзятых |
| Meta unlock mid-run | Флаг в save; decode только если пластинка сразу в loadout следующего клуба | Держать unlocked-but-unused decoded до конца акта |
| Смена клуба | Unload stems предыдущего клуба, не входящих в run loadout и не в next club pool | Накапливать все посещённые клубы в RAM |

**Lead time:** decode начинать на экране briefing / loading gate **до** `ScheduleStart`. Если decode не успел — не стартовать сет в тишине; показать wait/retry, не крутить gameplay clock.

**Web:** первая загрузка только после gesture (`InitializeAfterUserGesture`). Фоновая вкладка не decode’ит «в догон» ради награды.

**Android:** interruption (call, route change) не должна оставлять полу-decode в broken state; после Resume — догрузить/перепривязать по общим правилам backend.

### 5.4 Mid-run grant — конкретные риски

| Риск | Платформа | Симптом | Митигация |
|---|---|---|---|
| Heap spike на decode 3 stems | Web, mid Android | Tab kill / activity kill | Decode одного track за раз; cap concurrent decodes = 1 |
| Retention всех reward offers | Web | RAM растёт каждый клуб | Unload non-chosen offers сразу после confirm |
| Boss pack + starter + rewards | Web/Android | OOM на Signal | Перед boss: unload non-loadout club stems; budget check `Ready` count |
| AAC/transcode path | Web Unity | loop gap / click на новом треке | Source WAV policy; gate 50 loops на *новом* reward asset, не только на MVP |
| Double backend | Web | echo | Один backend; не Play Unity+JS одного stem |
| Sync drift после hot-load | все | stems >20 ms apart | Новые stems стартуют только через `PlayScheduled` на boundary; hot-swap mid-buffer запрещён |
| Meta catalog preload at boot | Web | долгий TTI, memory | CatalogOnly at boot; decode per need |

### 5.5 Жёсткие caps (проектные ориентиры P1)

Числа — **стартовые**; подтвердить профайлером на целевых Chrome / Android устройствах.

| Cap | Ориентир |
|---|---|
| Max `Ready` full records (3 stems) одновременно | **6** (2 playing + 2 queued/swap + 2 boss lookahead) |
| Max concurrent stem decodes | **1** (Web), **1–2** (native) |
| Evict when | track not on decks AND not in next-2 predicted ids AND not current boss phase window |
| Fail closed | если grant не влезает в budget — оставить CatalogOnly, показать «готовим пластинку», не крашить сет |

---

## 6. Связь с ClubDefinition / Run

Минимальные поля (концепт):

```json
{
  "id": "club_the_signal",
  "type": "Boss",
  "trackPool": ["track_signal_carrier", "track_signal_noise", "track_signal_pulse"],
  "phases": [
    { "id": "signal_probe", "minPhrases": 2, "pool": ["track_signal_pulse"] },
    { "id": "signal_lock", "minPhrases": 3, "pool": ["track_signal_pulse", "track_signal_noise"] },
    { "id": "signal_surge", "minPhrases": 2, "pool": ["track_signal_carrier"], "requireFlags": ["boss_drop"] }
  ],
  "clock": { "bpm": 120, "beatsPerBar": 4, "barsPerPhrase": 8 }
}
```

`clock` в club data — **документация/валидация** («этот клуб не отклоняется от global»). Runtime по-прежнему один `MusicClock` run’а. Если `clock` ≠ global → build/validation error до ADR.

Reward table ссылается на track ids с flag `reward`. Meta save хранит ids с `meta_unlock`.

---

## 7. Что слышно (acceptance на слух)

### Boss The Signal

1. Стартовый groove узнаваемо «не Neon Basement».
2. Смена фазы на bar/phrase — без gap/click; kick/perc остаются на сетке.
3. В surge signature `boss_drop` материал слышен как пик, не как случайный другой house-луп.
4. Bass kill по-прежнему уводит только LOW; MUSIC/PERC живы.
5. 5 минут на фазах босса при низком FPS: drift stems ≤ ~20 ms (ориентир A0).

### Reward / special

1. После выбора пластинки следующий клуб может поставить её на деку; старт на scheduled boundary, без щелчка.
2. Невзятые offer’ы не продолжают звучать и не едят RAM.
3. Meta-unlock без немедленного decode не слышен и не обязан грузиться.

---

## 8. Тесты и ручной прогон

Код тестов в этом документе не пишется; ниже — **обязательные границы**, которые закрывать EditMode/Core.Tests при появлении API.

| Граница | Проверка |
|---|---|
| Clock invariant | Boss club config с bpm≠120 или barsPerPhrase≠8 → reject / ADR gate |
| Flag parse | `reward` / `boss_drop` / `meta_unlock` round-trip; unknown flag → error |
| Phase boundary | Phase transition command id не исполняется дважды; schedule ≥ now+lead |
| Preload budget | Grant beyond max Ready → CatalogOnly, no throw into Playing |
| Gain | Crossfade equal-power и bass kill на boss stems = те же формулы |

### Ручной прогон ещё нужен

| Платформа | Сценарий |
|---|---|
| **Windows** native | Полный Signal set: 3 фазы, смена пластинок, bass kill, 50 loop boundaries на boss pack |
| **Chrome** Web | Gesture → briefing → клуб → reward grant mid-run → следующий клуб с новой пластинкой; DevTools memory до/после unload; tab hide на reward screen |
| **Android** | То же + phone call во время surge phase; BT route change после grant; нет OOM на mid-tier device при cap=6 Ready |

Повторить Audio Gate A0 ориентиры на **boss pack** и на **хотя бы одном reward track**, не только на MVP four.

---

## 9. Out of scope (явно)

- Hype числа, толпа, жесты UI (client-engineer / systems).
- Мелодия, точная sonic bible, имена финальных треков (music-supervisor).
- Low-pass filter appearance (только после ADR; паритет Web).
- Final Festival content list (тот же каркас фаз, отдельный pack).
- Реализация `TrackDefinition.Flags` в C# (следующая инженерная задача после A0).

---

## 10. История

| Дата | Изменение |
|---|---|
| 2026-11-23 | Первый design: Signal на 120/4/4/8; фазы; flags reward/boss_drop/meta_unlock; preload caps; memory risks Web/Android |
