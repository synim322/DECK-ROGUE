# 14. MVP Specification / Acceptance Criteria

## Определение MVP

**FIRST GIG v0.1** — небольшой, полностью проходимый пиксельный DJ roguelite с **двумя выступлениями**, в котором можно свести музыкальные деки, слышать результат, получать очки, выбрать случайный upgrade и использовать его во втором выступлении. Нужны playable Windows/Web/Android builds.

> North star полной игры: [`00_VISION.md`](00_VISION.md). MVP — миниатюра столпов «пульт + реакция зала + проход по реакции»; рандомный пул клубов, boss-клубы и dual progression в полном виде — P1+.

## Игровые ограничения

| Объект | MVP P0 |
|---|---|
| Контент | 1 клуб (Neon Basement), 2 последовательных выступления |
| Время | 100–140 секунд на выступление, ~5–7 минут на забег включая награду |
| Музыка | 4 пластинки × 3 stems, 120 BPM, 4/4, loop 8 bars |
| Пульт | 2 деки, Play/Queue, Crossfader, Bass Kill, Low-pass (зависит от прохождения Web Audio Gate) |
| Scoring | Hype, Reputation, Combo, transition/drop/match evaluators |
| Рандом | стартовый порядок доступных пластинок, один из 2 клубных запросов, 2 из 3 upgrade карточек |
| Прогрессия | выбор 1 награды между выступлениями, сброс после забега |
| UI | menu, audio unlock, briefing, performance, reward, result, settings, tutorial |
| Арт | 1 пиксельный клуб, 3 состояния толпы, 2 анимированных диска, базовый свет |
| Сборки | Windows, desktop Web, Android native landscape |

## Scope lock: P0, P1, P2

### P0 — без этого MVP не готов

- [ ] Audiogate A0: две деки, stems, чистый loop на 3 платформах.
- [ ] Один аудиоисточник авторитетен на каждой платформе; нет двойного Unity + Web Audio.
- [ ] Рабочие Play/Stop/Queue, crossfader, Bass Kill; фильтр P0 **только после подтверждённого решения** (см. ADR-003).
- [ ] 4 музыкальные пластинки с правами и качественной совместимостью.
- [ ] Состояния двух выступлений, цели, таймер, pause/resume.
- [ ] Hype / Combo / Reputation, прозрачные причины начислений.
- [ ] Detectors перехода / bass conflict / drop с анти-фармом.
- [ ] 3 валидных upgrades, 2 предлагаются случайным образом, один выбирается.
- [ ] Раздельный desktop/touch UI; Android landscape; Web audio unlock.
- [ ] Pixel club + crowd feedback с reduced flashing.
- [ ] UX tutorial и экран результата.
- [ ] Автотесты домена и smoke/e2e чеклист всех сборок.

### P1 — milestone **Act1 → First Boss** (после FIRST GIG)

Следующая играбельная версия **после** M6: проходимый акт до сюжетного босса **The Signal**. Детали и ID — [`15_ROADMAP.md`](15_ROADMAP.md) (DJ-101…115; Mixer depth = DJ-112…115).

| Входит в P1 / M7 | Не входит (отложено) |
|---|---|
| Act1 spine: Shuffle 3 ordinary → N=2 → **The Signal** (GDD §3) | Final Festival / Dream·Rave ship / Act2+ |
| Club pool + reaction hooks + deck-mgmt events | Elite / shop / event nodes |
| Music pools + reward 1-of-2/1-of-3 + Carrier meta | Meta currency, linear +%Hype unlocks |
| Deck depth: phrase-locked load, mid-set crate, CueReady (DJ-104) | Echo/Reverb/FX pack, Hot Cue suite, EQ3 |
| **Mixer depth** Act1: Filter 0..1 + MUSIC/PERC gain + Loop **1\|2\|4\|8**; CrowdMotion 0..3 + `FilterPhrase` / `StemPunch` / `LoopSnap` / `PhraseStart` / `BassHandoff` / `LineLift` ([GDD §3.4](01_GDD.md)) | Обязательный headphone cue; per-stem filter; knobs как единственный P0-путь |
| JRPG art pass на клубы акта + Signal | Desktop Safari, localization polish |
| Dual progression check (in-set ≠ run crate/gear) | Time-stretch, user music, leaderboards, iOS, сеть |
| Local SaveGame shell: Settings + `UnlockedTrackIds` + `BestReputation` (flush on change / meta / run end); Lose не откатывает meta | Mid-run / resume закрытого забега; cloud; meta gear/club gates |

**Не раздувать P0:** 1 клуб (Neon Basement), 2 сета, 4 пластинки, 3 crowd states, 3 upgrades — без пула клубов, без босса, без reward-vinyl, без phrase-locked crate mid-set, без обязательного Mixer depth. Канон Act1 design: [`01_GDD.md`](01_GDD.md) §3.1–§3.4.

### P2 — после проверки аудитории

Ручной BPM / time-stretching, harmonic mixing между тональностями, расширенная DJ-симуляция, пользовательский контент, cloud saves, mobile browser polish, новые платформы, online leaderboards, Act2+.

## Точная приёмка по вертикальным срезам

### Audio / Input

- PC: A и B всегда запускаются по музыкальной сетке и не расходятся за 5 минут; проверить метрономной/тестовой записью, ориентир ≤20 мс.
- Web: явное user gesture открывает AudioContext; 50 loops без систематического gap/click, 20 tab hide/resume без потерянной фазы; Chrome, Firefox, Edge.
- Android: все P0-действия доступны пальцами, multi-touch работает, audio focus interruption не даёт поражение.
- При движении crossfader звук реально меняется; при Bass Kill исчезает LOW, а остальные слои остаются.
- Filter (если оставлен в scope): audible low-pass на всех P0-targets. Не подменять UI-заявление тишиной.

### Gameplay

- Игрок может сделать **минимум 2 разных типа перехода** (smooth и hard cut).
- До и после награды gameplay различим: Auto Bass/Riser/Hard Cut имеет слышимый либо управляющий эффект.
- Scoring не зависит от FPS; clamp Hype [0,100], Reputation≥0.
- Два события с одним ID не удваивают reward.
- Победа первого сета не заканчивает забег: экран результата и второй сет. Победа второго сета или любое поражение заканчивает забег; есть Reset/new run. Карточки между сетами — DJ-014.
- Финиш 1-го сета требует Hype≥60, 2-го Hype≥65 плюс дополнительное условие.

### UI / Accessibility

- Игрок понимает 4 действия DJ-пульта без внешнего объяснения после короткого tutorial.
- Android важные touch-targets имеют минимум ~48dp, safe area корректна.
- Текущий трек/активная дека/бит/цель видны без цветового различения.
- Reduced flashing полностью выключает strobes и интенсивные flash.
- В Web ошибки звукозапуска/загрузки дают Retry вместо «тишина и проигрыш».

## MVP не готов, если

- Web build играет только через native фильтр и часть функций молчит.
- Игрок набирает Hype многократным переключением bass kill без слышимого результата.
- В Android нельзя выполнить переход без клавиатуры.
- Каждое выступление выглядит одинаково и upgrade никак не влияет на второй сет.
- Разные платформы дают принципиально разные правила scoring.

## Definition of Done для каждой P0 задачи

1. Есть документ требований и тестов.
2. Реализовано в отдельной ветке, нет критических ошибок компиляции.
3. Unit/PlayMode tests проходят для затронутых правил.
4. Проверено хотя бы на Windows + Web + Android smoke (если platform sensitive).
5. Описан reproducible manual test и подтверждён acceptance.
6. Обновлены профильный .md, GDD/MVP при изменении правил.
7. В PR указаны риски, скрин/видео/аудиодемонстрация для визуальных/аудио задач.

## Exit criteria к публичной демоверсии

- Предварительный playtest 8–12 человек, ≥60% добровольно повторяют забег (рабочая гипотеза).
- Нет crash/blocker в обязательных браузерах и тестовом Android.
- Музыка и sample packs легальны для всех заявленных дистрибуций.
- Пройдены QA с фокусом на слуховые дефекты, зрительное восприятие, паузы и touch.
- Готовы инструктаж, настройки звука, экран лицензий/credits, способ сообщить о проблеме.
