# 06. Audio Tech Design: синхронизация, stems, WebAudio

> **Критический документ.** Прежде чем делать арт и большой контент, подтвердить звук во всех обязательных средах. Аудиосистема — основной инженерный риск проекта.

## 1. Аудиоцели

- Два музыкальных деки × по 3 синхронных stems (`LOW`, `PERC`, `MUSIC`).
- Общий музыкальный таймлайн (BPM 120, 4/4, 8 тактов), независимый от FPS.
- Без слышимых щелчков, пропусков такта и накопления рассинхронизации.
- Crossfader и bass kill реально управляют звуком; фильтр работает или явно исключён из тестовой сборки.
- Один и тот же gameplay code получает **подтверждённые семантические события** от platform audio backend.

## 2. Почему нужен интерфейс аудиобэкенда

Unity на PC/Android использует развитую аудиосистему. На Web Unity строит звук поверх браузерного Web Audio; многие DSP-эффекты Unity **не поддерживаются**. В официальном руководстве Unity 6.3 для Web доступен `AudioSource.PlayScheduled()`, но AudioMixer на Web позволяет менять **только громкость**, не обычные эффекты; есть ограничение `AudioClip.Create(stream=true)` и риск глитча на петле AAC. Поэтому нельзя просто нарисовать UI-фильтр и рассчитывать, что native AudioMixer «заработает сам» в браузере.

**Архитектурный контракт:**

- `IAudioBackend`: `InitializeAfterUserGesture`, `LoadStems`, `ScheduleStart`, `ScheduleStop`, `SetDeckGain`, `SetStemGain`, `SetLowPass`, `GetAudioClock`, `GetTransportState`, `Suspend`, `Resume`, `Dispose`.
- `IMusicClock`: `GetBar`, `GetBeat`, `GetNextBoundary`, `ToAudioTime`; владеет временной сеткой, без `Time.deltaTime` как основы ритма.
- `DeckController`: orchestration, но не знает `AudioSource`/`AudioContext`.
- `GameplayEventBus`: `DeckStarted`, `TransitionCompleted`, `DropCompleted`, `AudioInterrupted`; scoring на **семантических** событиях.

## 3. Бэкенд A — NativeUnityAudioBackend (Windows/Android)

- Предпочтительно `AudioSettings.dspTime` + `AudioSource.PlayScheduled(time)` с lead time на загрузку/декодирование.
- Отдельные `AudioSource` на stems; плавная регулировка гейна; защита от обрывов.
- `AudioMixer` для мастер-громкости и доступных native FX. **Не допускать двойной обработки эффектов** при добавлении Web-моста.
- Учитывать аудиопаузу при уходе Android в background, смене audio route и телефонном звонке.

## 4. Бэкенд B — Web

### Вариант B1: Unity Web AudioSource для MVP

В Unity 6.3 Web официально перечисляет `AudioSource.PlayScheduled`, `Play`, `Stop`, `volume` как поддерживаемые. Это позволяет **протестировать** 2 деки и stem mixing без отдельного JS-движка. Но нужны реальные проверки: фазовая точность, gapless loop, поведение после tab switch, `AudioSettings.dspTime`, одновременные источники и декодирование. Unity Web AudioMixer ограничен громкостью: native low-pass эффект НЕ гарантируется.

### Вариант B2: BrowserWebAudioBackend (рекомендуем, если нужен реальный фильтр в Web)

JS `.jslib` bridge вызывает Browser Web Audio API: `AudioContext` (источник времени), `AudioBufferSourceNode` (плейбек и расписание), `GainNode` (кроссфейдер и bass kill), `BiquadFilterNode` (low-pass), `AudioParam` automation (плавные изменения). Stems загружаются в WebAudio через `fetch`/`decodeAudioData`, **не играть одни и те же stems одновременно через Unity и браузер**. Мост передаёт планируемое время запуска и состояние Unity, backend управляет аудио; Unity визуализирует события. Сохранять audio scheduling в WebAudio time, а не пытаться гонять миллисекундные события из C# через `Update()`.

**Gate:** на раннем spike сравнить B1 и B2. Решение зафиксировать в [ADR](19_RISKS_AND_DECISIONS.md). Для паритета фильтра предпочтительнее B2, но это дополнительная разработка и отдельная загрузка/размещение аудио. Не объявлять готовым до теста.

## 5. Autoplay / user gesture

На Web звук не должен запускаться без пользовательского жеста. Первый экран: большая кнопка **«Включить звук и начать»**; по ней `AudioContext.resume()` (или эквивалент Unity-процедуры) выполняется внутри обработчика пользовательского действия. Если аудио заблокировано — показать инструкцию; не расходовать игровой таймер. После возврата в активную вкладку можно требовать повторного действия.

## 6. Audio clock и фразы

`beatDuration = 60 / BPM` (0.5 с), `barDuration = beatDuration*4` (2 с), `phraseDuration = barDuration*8` (16 с). События `Beat`, `Bar`, `Phrase` вычислять от **audio clock origin** и времени аудиосистемы. Пороговый ввод «следующий такт» назначает время не ранее `now + schedulingLeadTime`. Одна команда `ScheduleStart` имеет свой ID и scheduled timestamp. При пересоздании backend origin и очереди корректно обнуляются.

## 7. Audio graph (концептуально)

```text
Deck A: LOW/PERC/MUSIC → stem gains → Deck A gain → (filter) ┐
                                                         Master → Output
Deck B: LOW/PERC/MUSIC → stem gains → Deck B gain → (filter) ┘
```

- Crossfader: `gA=cos(pi*x/2)`, `gB=sin(pi*x/2)`.
- Bass Kill: target gain LOW = `0/1`, fade 10–30 мс (гипотеза).
- Filter: lowpass cutoff по экспоненциальной шкале частот, сглаживание параметров; без резких скачков gain.
- Master headroom: подготовленные stems с запасом; проверять пиковый уровень в реальном миксе.

## 8. Аудиоконтент и память

Четыре пластинки × три stems = 12 аудиофайлов. 16 с × 48 000 Hz × 2 каналов × 4 байта × 12 ≈ **73,7 MB** только PCM после декомпрессии, без движка и других ассетов. Это **оценка**, браузерный decode/resample и overhead могут изменить её. Не загружать бесконтрольно все будущие треки. Проверять размер RAM и decode time на реальных Android / desktop browsers. Для MVP можно сделать моно там, где не требуется стерео, но не жертвовать качеством из-за произвольной цифры.

Бюджет Act 1 (обычные клубы до босса), preload/unload и folder contract: [`17_AUDIO_CONTENT_ACT1.md`](17_AUDIO_CONTENT_ACT1.md).

## 9. Техническая приёмка — Audio Gate A0

1. После явного запуска звук идёт в Win build + Chrome + Firefox + Android native.
2. Слуховая проверка двух одновременных tracks и переключений `A↔B`, без щелчков.
3. 5 минут игры при искусственном снижении FPS: расхождение stems/битов **целевой ориентир ≤20 мс** (проверяется записью/анализом; точный порог уточнить).
4. 50 последовательных loop boundaries в Web без систематических gap/click; для Web Unity документирует потенциальную AAC-проблему.
5. 20 циклов `Pause / resume / background` без начисления score в тишине и без зависшего Play.
6. Android: входящий звонок/системное audio interruption, BT/проводные наушники — устойчивое восстановление.
7. Фильтр на каждой обязательной платформе реально меняет спектр либо по решению ADR имеет честную замещающую реализацию.

## 10. План борьбы с AAC loop glitches в Web

Unity 6.3 отдельно предупреждает о возможном искажении первых 1024 samples AAC; предлагает WAV source, предварительный silence/padding и корректный WAV `smpl` loop marker. Это **не универсальная гарантия**. Сначала тестировать реальные end-to-end зацикливания. Если проблема сохраняется, использовать запланированные соседние буферы/Web Audio own looping, экспортные корректировки и короткие fades на стыках; сохранить музыкальную фазу.

## 11. Запрещённые предположения

- «На Web работает весь AudioMixer, как на Windows» — **нет**.
- «`PlayScheduled` где-то упомянут, значит звук идеально синхронен» — **требует проверки**.
- «Можно запускать звук автоматически после загрузки страницы» — **нельзя считать допустимым**.
- «Можно использовать OnAudioFilterRead на Web» — **нет гарантированной scriptable DSP pipeline**.
- «Все Android телефоны имеют одинаковую задержку» — **нет**.

## Источники

См. [официальные ссылки и дату проверки](../references/TECH_SOURCES.md). Основные: Unity 6.3 Audio in Web, Unity PlayScheduled, MDN Web Audio API, MDN autoplay policies.
