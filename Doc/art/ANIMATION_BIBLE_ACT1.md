# Motion Bible — Act 1 (Performance & Run Flow)

**Проект:** DECK//ROGUE · **Владелец:** Animation / Art Director  
**Источник истины:** [`19_ART_DIRECTION_JRPG.md`](../19_ART_DIRECTION_JRPG.md) · [`DECK_ROGUE_MASTER.md`](../DECK_ROGUE_MASTER.md) §12 / MusicClock · [`ENV_ACT1_BRIEFS.md`](ENV_ACT1_BRIEFS.md)  
**Статус:** v0.2 · **Дата:** 2026-11-23 · **AD lock:** beat-sync ok; reduced flashing = static/breathe ≤5%, no screen strobe

---

## Scope Act 1

Все motion-спеки ниже применяются к:

| Контекст | Клубы / экраны |
|----------|----------------|
| Performance | `club_neon_basement`, пул обычных Act 1, boss `club_the_signal` |
| Межсетовой flow | `Reward` (reveal пластинки), `PreClub` (boss intro pose) |
| **Out of scope** | Final Festival, клубы вне Act 1, полноэкранные кат-сцены без пульта |

Движение синхронизируется с **audio clock** (`MusicClock`, 120 BPM), не с `Time.deltaTime` как первичным ритмом. Визуал не опережает слышимый удар.

---

## 0. Time base — квантизация `MusicClock`

| Единица | Длительность @ 120 BPM | `MusicalBoundary` | Типичное использование motion |
|---------|-------------------------|-------------------|-------------------------------|
| **Beat** (четверть) | **0.5 s** | `Beat` | Crowd hit-frame, vinyl micro-snap, beat grid pulse |
| **Bar** (такт 4/4) | **2.0 s** | `Bar` | Vinyl полный оборот, сильная доля сетки, deck `QUEUED → PLAYING` |
| **Phrase** (8 bars) | **16.0 s** | `Phrase` | Смена crowd band (гистерезис), bar light «чин», phrase wash |

**Правила планирования (зеркало `DeckScheduler` + `MusicClock`):**

1. Любой «ударный» ключ кадра или событие с подписью **beat/bar/phrase** публикуется на `GetNextBoundary(boundary, audioTimeNow)`, не на arbitrary frame.
2. Внутри кадра **интерполяция допустима** (диск крутится плавно между долями); **смена клипа crowd / flash peak / смена иконки деки** — только на границе, если не указано иное.
3. `SchedulingLeadTime` = **0.05 s** — UI может показать «ЖДЁТ ТАКТ» до boundary; анимация **не** считает деку «в эфире» до backend ack (см. §5).
4. После `ResumeKeepingElapsed` фаза винила и crowd **пересчитывается** от сохранённого elapsed, без скачка rotation.

**Формула фазы винила (канон, совпадает с runtime readout):**

```text
spinDegrees = (audioTimeNow - deckStartAudioTime) / barDuration × 360°
           = elapsed_on_deck / 2.0 s × 360°   → 1 об/такт
```

---

## 1. Vinyl spin — вращение пластинки на доле

**Объект:** `UI_DeckA_Disc`, `UI_DeckB_Disc` (и дубль в Performance HUD).

| Режим | Вращение | Скорость / привязка |
|-------|----------|---------------------|
| **Empty / Loaded (stopped)** | 0° | Статичный спрайт; лёгкий idle **не** крутит диск |
| **Queued** | Интерполяция + **micro-snap на beat** | Базовая скорость = 1 об/такт (§0). Амплитуда snap: **±2°** на beat 1 такта (опционально 1 px eccenter на groove ring) |
| **Playing** | Непрерывная фаза §0 | Маркер/белая точка на краю проходит beat grid; при reduced flashing snap **отключён**, только плавный spin |
| **Paused** | Freeze последнего угла | Без decay spin |

**Визуал:** Point filter, rotation вокруг центра hub; без motion blur. Deck A/B — те же правила, tint по палитре деки (`#62A4F4` / `#F4A06B`) только на rim, не на весь диск.

**Не делать:** независимые скорости A/B «для красоты»; spin приглушённой деки не останавливать, если track queued/playing в scheduler.

---

## 2. Crowd loops — три состояния Hype

**Домен:** `CrowdBand` = `0 | 1 | 2` (см. `RunChrome`):  
`Hype < 40` → Low · `40 ≤ Hype < 70` → Medium · `Hype ≥ 70` → High.

### 2.1 Архетипы (MVP: 3–5 силуэтов, один набор клипов)

| Band | Имя | Кадров / loop | FPS авторинга | Движение |
|------|-----|---------------|---------------|----------|
| **0 Low** | `Crowd_Low` | 2–3 | 8 | Редкое покачивание; руки у корпуса; **hit на beat 1** каждого **2-го** такта (bar pulse без прыжка) |
| **1 Med** | `Crowd_Med` | 3–4 | 10 | Ритмичный step/tap; **hit каждый beat 2 и 4** (backbeat sync) |
| **2 High** | `Crowd_High` | 4–6 | 12 | Руки вверх, hop на **beat 1** каждого такта; вторичный кадр на beat 3 (опционально) |

**Stagger:** копии одного архетипа сдвигаются на **0 / 0.25 / 0.5 bar** phase offset + tint ±1 step из клубной палитры — без уникальных rig на каждого.

### 2.2 Переход между band

| Параметр | Значение |
|----------|----------|
| Переключение clip | Ближайший **bar boundary** после устойчивого band |
| Гистерезис | Вход в High: Hype ≥ 70 **2 beats**; выход из High: Hype < 65 **1 bar**; Med ↔ Low: ±5 Hype от порога, **1 bar** |
| Blend | **0 frames** crossfade (pixel snap); опционально 1-frame anticipation **до** boundary, не после |

### 2.3 Синхронизация команд аниматора

Keyframe labels: `hit_b1`, `hit_b2`, `hit_b3`, `hit_b4`. Unity: `clip.Sample(audioPhase)` или Animator с **fixed musical phase**, не normalized time free-run.

**Asset naming:** `SPR_Crowd_<Arch>_Low_##.png`, `…_Med_##`, `…_High_##` · `ANIM_Crowd_<Arch>_Low` (2–3 frames @ 8fps, loop).

---

## 3. Light pulses — bar & phrase

**Слой:** аддитивный `VFX_Club_*` + сценовые light rigs (не baked в crowd BG). Яркость привязана к **сетке**, не к Hype каждый кадр.

| Событие | Boundary | Длительность | Peak | Примечание |
|---------|----------|--------------|------|------------|
| **Beat tick** (фон) | Beat | 0.5 s cycle | +8–12% intensity baseline | Субдольные «дыхания» запрещены |
| **Bar pulse** | Bar (beat 1) | **1 beat** attack, **1 beat** decay | +25% над baseline | Ключевой акцент light wash / LED strip |
| **Phrase marker** | Phrase (bar 1 beat 1) | **2 beats** ramp | +15% sustained 1 bar | Мягче bar pulse; «новая восьмёрка» |
| **Drop feedback** (gameplay) | Ближайший **Beat** после ack Drop | **1 bar** total | +35% **только light layer** | Не full-screen; см. §7 |

**Hype модуляция (медленная):** baseline intensity lerp за **2 bars** при изменении band, не строб при каждом +8 Hype.

**The Signal (boss):** phase palette из ENV brief; pulse те же boundary, цвет из phase table — **без** full-screen flash на смене фазы.

---

## 4. Deck state — `QUEUED → PLAYING`

**Состояния UI:** `ПУСТО` → `ЖДЁТ ТАКТ` (Queued) → `ИГРАЕТ` (Playing) · отдельно `Paused` (иконка ‖).

### 4.1 Timeline (отправка `TryScheduleStart`, boundary = Bar по умолчанию)

| Фаза | Триггер | Motion |
|------|---------|--------|
| **T0** | Команда принята, start > now | Иконка **queue** (жёлтый rim `#F2CF6E`), label `ЖДЁТ ТАКТ`, spin начинается по §1 |
| **T1** | `audioTime ≥ deckStart`, backend ack playing | **Bar boundary:** flash rim **1 beat**, иконка → **on-air** (deck accent), label `ИГРАЕТ` |
| **T1+** | — | Beat grid на пульте: **первая доля первого такта в эфире** — усиленный маркер (совпадает с бар pulse §3) |

**Queued vs Playing:** разная **иконка + текст + rim**; не только alpha. Crossfader **не** анимируется автоматически.

### 4.2 Ошибка / reject

Scheduler reject → **shake 2 px, 2 beats**, warning tint; без strobing. Состояние остаётся предыдущим.

---

## 5. Reward — track reveal flourish (короткий)

**Экран:** `RunState.Reward` (между сетами / клубами Act 1). Длительность **≤ 2 bars** (4 s @ 120 BPM), можно пропустить tap.

| Beat offset (от boundary входа) | Элемент |
|---------------------------------|---------|
| **0** (bar 1 b1) | Карточки наград уже на месте; **новая пластинка** скрыта (силуэт / `?`) |
| **b2** | Sleeve **slide up 4 px** (pixel step) |
| **b3** | Label track id **pop** 1 px → 0 (no scale blur) |
| **b4** | Rim burst **deck color** 1 beat |
| **bar 2 b1** | Idle; кнопка выбора активна |

**Синх:** старт flourish = `GetNextBoundary(Bar)` при открытии экрана (или немедленно, если экран открыт ровно на boundary).

**Не делать:** вращение 3D sleeve; particle flood; звук reveal **не** обязан совпадать с b1 (UI SFX on bar 2 допустим).

**Asset:** `UI_Reward_TrackReveal_##.png`, `VFX_Reward_Sparkle_01` (≤4 frames, additive).

---

## 6. Boss intro pose — `club_the_signal`

**Момент:** переход `PreClub → Performing` **только** для boss; обычные клубы — **статичный** briefing без pose hold.

| Слой | Intro pose (hold **2 bars**) | Release |
|------|------------------------------|---------|
| **DJ** | 1 extra frame: рука на crossfader, голова к crowd; **freeze** на bar 1 | Bar 3 — crossfade в idle Performance loop |
| **Crowd** | Band forced Med **silhouette** (anticipation), **no High hops** | Crowd band по реальному Hype с bar 3 |
| **Lights / towers** | Symmetric towers **+bar pulse only** (§3), violet base `#5B4FD1` | Phase A cyan по gameplay, не в intro |
| **VFX** | Optional `signal pulse` **beat-sync**, amplitude −50% vs default | После bar 2 — обычная таблица §3 |

**Composition:** ось симметрии ENV brief; **нижний deck safe y ≥ 118** остаётся пустым — pose только выше.

**Нет:** титр-бой «BOSS», HP bar, камера без пульта.

**Asset:** `SPR_DJ_TheSignal_Intro_00.png` (hold), `ANIM_BossIntro_TheSignal` (2 bars, quantize end).

---

## 7. Reduced flashing — что отключается

**Настройка:** `reducedFlashing: true` (Settings, persist в save). Default **off** — допускает мягкие эффекты из таблицы ниже.

При **включённом** reduced flashing **полностью выключается**:

| Категория | Эффект (default ON) | Reduced |
|-----------|---------------------|---------|
| Строб | Alternating on/off light ≤ **2 beats** period | **OFF** |
| Full-screen flash | Белый / near-white overlay **> 1 frame** | **OFF** |
| Drop / bar peak | +35% light **+** screen white lift | Только **+15%** light layer, **без** white lift |
| Reward flourish | Rim burst bar 1 b4 | **Static** rim (no burst) |
| Vinyl beat snap | ±2° micro-snap on beat 1 | **OFF** (smooth spin only) |
| Boss intro / phase change | Gold/coral **full-frame** wash (ENV resolve) | **OFF**; допустим **palette swap 1 frame** max contrast Δ **< 30%** |
| Error feedback | Red full-panel flash | **Shake + icon** only (`BASS CLASH`) |
| Beat grid UI | Strobing whole grid | **Beat 1 bar** marker only; beats 2–4 — static |

**Остаётся включённым (безопасно):**

- Непрерывный vinyl spin (§1)  
- Crowd loop frame advances (не flash)  
- Bar/phrase **умеренные** light pulses без white channel  
- Текст feedback, иконки состояния деки  
- Плавная смена crowd band на boundary  

**QA:** grayscale capture + reduced ON: нет кадров с глобальной luminance spike **> 40%** за 0.5 s; нет **≥ 3 Hz** переключения full-screen.

---

## 8. Naming & deliverables (Act 1 motion)

| Система | Минимум файлов |
|---------|----------------|
| Crowd | 3 архетипа × 3 bands (Low/Med/High) |
| Deck disc | 2 UI + 2 states icon set |
| Club lights | 1 bar pulse clip / клуб (`VFX_Club_*_Pulse`) |
| Reward | flourish ≤ 8 sprites + 1 VFX |
| Boss | DJ intro 1 pose + 2 bar anim |

Префиксы: `SPR_`, `ANIM_`, `VFX_`, `UI_` — см. мастер-док §12.

---

## 9. Engineering hooks (checklist)

- [ ] `MusicClock.GetPosition().BeatInBar` драйвит crowd hit frames.  
- [ ] `SpinDegrees` из audio elapsed, не `Time.deltaTime` интеграл.  
- [ ] `CrowdBand` гистерезис совпадает с §2.2 или документированно в `RunChrome`.  
- [ ] UI Playing только после scheduler + backend ack.  
- [ ] `reducedFlashing` гейтит таблицу §7 до кадра, не post-process.  
- [ ] Reward flourish ≤ 2 bars, skippable.  
- [ ] Boss intro только `club_the_signal`.  
- [ ] Web/Android: crowd + lights **не** едят >1 ms CPU per frame @ 480×270 band.

---

## Связанные документы

- Canvas / safe zones: [`ENV_ACT1_BRIEFS.md`](ENV_ACT1_BRIEFS.md)  
- Audio grid & stems: [`17_AUDIO_CONTENT_ACT1.md`](../17_AUDIO_CONTENT_ACT1.md)  
- Runtime clock: `DJ/Assets/_Game/Core/Music/MusicClock.cs`  
- Crowd band thresholds: `DJ/Assets/_Game/Audio/RunChrome.cs`