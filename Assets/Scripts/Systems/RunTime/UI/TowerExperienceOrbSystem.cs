using System;
using System.Collections.Generic;
using Helpers;
using Services;
using Signals;
using UI.Views.Game;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Systems.RunTime.UI
{
    /// <summary>
    /// Визуальный дроп опыта: по TowerExperienceDroppedSignal спавнит несколько орбов у места
    /// смерти врага. Орбы сначала слегка разлетаются в стороны, затем (после паузы) летят к
    /// RectTransform полосы опыта в HUD и гаснут pop/glow-вспышкой.
    ///
    /// Сумма дропа делится поровну между спавнящимися орбами (см. SplitAmount), и КАЖДЫЙ орб
    /// сам зачисляет свою долю через TowerExperienceOrbArrivedSignal ровно в момент, когда
    /// долетает до бара (TowerExperienceService слушает этот сигнал и только тогда двигает
    /// PendingExperience) — то есть опыт реально начисляется по прилёту, а не в момент смерти
    /// врага. Если визуал показать нечем (нет настроек/цели/камеры, враг умер вне экрана, орб
    /// упёрся в потолок MaxActiveOrbs) — сумма зачисляется немедленно тем же сигналом, чтобы
    /// опыт никогда не терялся молча.
    ///
    /// Пул объектов и общий Tick вместо корутины на орб — по образцу ShowDamageNumbersSystem:
    /// при волне смертей врагов дропы прилетают пачками в один кадр.
    /// </summary>
    public class TowerExperienceOrbSystem : IInitializable, ITickable, IDisposable
    {
        private enum Phase
        {
            Scatter,
            Hold,
            Flight,
            Arrival
        }

        private readonly SignalBus _signalBus;

        // Полное имя намеренно: внутри Systems.RunTime простое Camera — это неймспейс проекта.
        private readonly UnityEngine.Camera _camera;

        private readonly SceneHandler _sceneHandler;
        private readonly TowerExperienceOrbSpawnerView _settings;
        private readonly IGameTimeProvider _gameTimeProvider;

        private readonly List<Entry> _active = new();
        private readonly Stack<TowerExperienceOrbView> _pool = new();

        private bool _warnedMissingSetup;

        public TowerExperienceOrbSystem(
            SignalBus signalBus,
            UnityEngine.Camera gameCamera,
            SceneHandler sceneHandler,
            TowerExperienceOrbSpawnerView settings,
            IGameTimeProvider gameTimeProvider
        )
        {
            _signalBus = signalBus;
            _camera = gameCamera;
            _sceneHandler = sceneHandler;
            _settings = settings;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<TowerExperienceDroppedSignal>(OnExperienceDropped);
        }

        public void Tick()
        {
            if (_active.Count == 0)
                return;

            var parent = _sceneHandler != null ? _sceneHandler.ParentForUiEffects : null;
            if (parent == null)
                return;

            var deltaTime = _gameTimeProvider.DeltaTime;

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var entry = _active[i];

                // Сцена могла перезагрузиться под нами — тогда просто забываем запись.
                if (entry.View == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                entry.Elapsed += deltaTime;

                switch (entry.CurrentPhase)
                {
                    case Phase.Scatter:
                        TickScatter(ref entry);
                        break;
                    case Phase.Hold:
                        TickHold(ref entry);
                        break;
                    case Phase.Flight:
                        TickFlight(ref entry, parent);
                        break;
                    case Phase.Arrival:
                        if (TickArrival(ref entry))
                        {
                            Recycle(i);
                            continue;
                        }
                        break;
                }

                _active[i] = entry;
            }
        }

        private void OnExperienceDropped(TowerExperienceDroppedSignal signal)
        {
            if (signal.amount <= 0)
                return;

            // Требование "не должен ломать игру": нет префаба/цели — эффекта нет, но опыт
            // не должен пропасть молча (он больше не начислен в TowerExperienceService заранее,
            // как раньше, — начисление теперь целиком на нашей совести).
            if (_settings == null || _settings.OrbPrefab == null || _settings.TargetBarTransform == null)
            {
                WarnMissingSetupOnce("не назначен orbPrefab или targetBarTransform на TowerExperienceOrbSpawnerView");
                CreditImmediately(signal.amount);
                return;
            }

            var parent = _sceneHandler != null ? _sceneHandler.ParentForUiEffects : null;
            if (parent == null || _camera == null)
            {
                WarnMissingSetupOnce("не найден Canvas (SceneHandler.ParentForUiEffects) или камера");
                CreditImmediately(signal.amount);
                return;
            }

            if (!TryGetLocalPoint(signal.worldPosition, parent, out var spawnPoint))
            {
                // Враг умер вне видимости камеры (screenPoint.z < 0) - показывать нечего,
                // но опыт всё равно должен дойти.
                CreditImmediately(signal.amount);
                return;
            }

            var requestedCount = Random.Range(_settings.MinOrbCount, _settings.MaxOrbCount + 1);
            var capacity = Mathf.Max(0, Mathf.Max(1, _settings.MaxActiveOrbs) - _active.Count);
            var spawnCount = Mathf.Min(requestedCount, capacity);

            if (spawnCount <= 0)
            {
                // Потолок орбов уже выбран другими дропами - визуально показать нечем,
                // но сумму всё равно нужно зачислить, а не потерять.
                CreditImmediately(signal.amount);
                return;
            }

            // Делим сумму поровну между орбами, которые реально будут заспавнены (не между
            // "хотели" requestedCount) - иначе при упоре в потолок остаток терялся бы молча.
            var shares = SplitAmount(signal.amount, spawnCount);
            for (var i = 0; i < spawnCount; i++)
                SpawnOrb(parent, spawnPoint, shares[i]);
        }

        /// <summary>Опыт, который нечем показать визуально, зачисляется тем же путём, что и обычный прилёт.</summary>
        private void CreditImmediately(int amount)
        {
            if (amount <= 0)
                return;

            _signalBus.Fire(new TowerExperienceOrbArrivedSignal { amount = amount });
        }

        /// <summary>Делит total на parts частей как можно ровнее; сумма результата всегда точно равна total.</summary>
        private static int[] SplitAmount(int total, int parts)
        {
            var result = new int[parts];
            var baseShare = total / parts;
            var remainder = total - baseShare * parts;

            for (var i = 0; i < parts; i++)
                result[i] = baseShare + (i < remainder ? 1 : 0);

            return result;
        }

        private void SpawnOrb(RectTransform parent, Vector2 spawnPoint, int creditAmount)
        {
            var view = Rent(parent);
            if (view == null)
                return;

            var scatterAngle = Random.Range(0f, Mathf.PI * 2f);
            var scatterDistance = Random.Range(_settings.ScatterRadius * 0.4f, _settings.ScatterRadius);
            var scatterPoint = spawnPoint + new Vector2(Mathf.Cos(scatterAngle), Mathf.Sin(scatterAngle)) * scatterDistance;

            view.RectTransform.anchoredPosition = spawnPoint;
            view.RectTransform.localScale = Vector3.one;
            view.ResetArrivalGlow(); // на случай, если этот же view уже гас в прошлый раз из пула

            if (view.Icon != null)
            {
                view.Icon.enabled = true;
                SetAlpha(view.Icon, 0f); // появляется плавным фейд-ином в TickScatter, а не сразу целиком
            }

            if (view.ArrivalGlow != null)
                view.ArrivalGlow.SetActive(false);

            _active.Add(new Entry
            {
                View = view,
                CurrentPhase = Phase.Scatter,
                Elapsed = 0f,
                StartPoint = spawnPoint,
                ScatterPoint = scatterPoint,
                FlightDuration = Random.Range(_settings.FlightDurationMin, _settings.FlightDurationMax),
                // Знак решает, в какую сторону выгибается дуга — чтобы соседние орбы одного дропа
                // не летели одной слипшейся линией, а расходились небольшим веером, как в Archero.
                ArcSide = Random.value < 0.5f ? -1f : 1f,
                ArcHeight = Random.Range(_settings.FlightArcHeightMin, _settings.FlightArcHeightMax),
                CreditAmount = creditAmount
            });
        }

        private void TickScatter(ref Entry entry)
        {
            var duration = Mathf.Max(0.01f, _settings.ScatterDuration);
            var t = Mathf.Clamp01(entry.Elapsed / duration);
            var eased = 1f - (1f - t) * (1f - t); // ease-out: резкий рывок в стороны, плавное торможение

            entry.View.RectTransform.anchoredPosition = Vector2.Lerp(entry.StartPoint, entry.ScatterPoint, eased);

            // Орб появляется фейд-ином, а не хлопком - это же самое "не слишком резко", что и полёт.
            if (entry.View.Icon != null)
                SetAlpha(entry.View.Icon, eased);

            if (t >= 1f)
            {
                entry.CurrentPhase = Phase.Hold;
                entry.Elapsed = 0f;
            }
        }

        private void TickHold(ref Entry entry)
        {
            if (entry.Elapsed < _settings.DelayBeforeFlight)
                return;

            entry.CurrentPhase = Phase.Flight;
            entry.Elapsed = 0f;
            entry.FlightStartPoint = entry.View.RectTransform.anchoredPosition;
        }

        private void TickFlight(ref Entry entry, RectTransform parent)
        {
            var target = _settings.TargetBarTransform;
            if (target == null)
            {
                // Цель пропала на лету (например, HUD пересобрался) — мягко гасим орб, без ошибок,
                // но зачисление всё равно должно произойти (см. BeginArrival).
                BeginArrival(ref entry);
                return;
            }

            // Пересчитываем каждый кадр, а не один раз в начале полёта: бар — часть HUD и в
            // принципе может двигаться (ресайз, layout), орб не должен лететь мимо цели.
            //
            // Важно: target - это RectTransform того же Canvas, а не точка в игровом мире, поэтому
            // сюда нельзя подавать camera.WorldToScreenPoint (как для spawnPoint ниже) - для Canvas
            // в режиме Screen Space - Overlay мировая позиция UI-элемента УЖЕ равна экранным
            // пикселям, и повторная проекция через камеру даёт огромные мусорные координаты
            // (орб улетает далеко за пределы экрана - выглядит как "он вообще не летит").
            // InverseTransformPoint переводит world-позицию бара прямо в локальные координаты
            // parent, минуя экран целиком - корректно для любого режима Canvas.
            var targetLocal = parent.InverseTransformPoint(target.position);
            var targetPoint = new Vector2(targetLocal.x, targetLocal.y);

            var duration = Mathf.Max(0.05f, entry.FlightDuration);
            var t = Mathf.Clamp01(entry.Elapsed / duration);
            var curved = _settings.FlightCurve != null ? _settings.FlightCurve.Evaluate(t) : t;

            // Летим не по прямой, а по дуге (квадратичная кривая Безье) - именно эта выгнутая
            // траектория и даёт узнаваемый "сбор опыта" вместо линейного перелёта, который на
            // коротких дистанциях глазу почти не за что зацепить.
            entry.View.RectTransform.anchoredPosition =
                EvaluateArc(entry.FlightStartPoint, targetPoint, entry.ArcSide * entry.ArcHeight, curved);

            if (t >= 1f)
                BeginArrival(ref entry);
        }

        /// <summary>
        /// Единая точка входа в фазу прилёта, откуда бы её ни начали (обычное завершение полёта
        /// или ранний выход при пропавшей цели) — гарантирует, что зачисление опыта происходит
        /// ровно один раз, синхронно с визуальным прилётом орба.
        /// </summary>
        private void BeginArrival(ref Entry entry)
        {
            entry.CurrentPhase = Phase.Arrival;
            entry.Elapsed = 0f;

            if (entry.CreditAmount > 0)
            {
                _signalBus.Fire(new TowerExperienceOrbArrivedSignal { amount = entry.CreditAmount });
                entry.CreditAmount = 0; // страховка от повторного зачисления при случайном повторном входе
            }

            // Точка подключения будущего звука/партикла прилёта XP в бар.
            if (entry.View.ArrivalGlow != null)
            {
                entry.View.ArrivalGlow.SetActive(true);
                if (entry.View.ArrivalGlowImage != null)
                    entry.View.ArrivalGlowImage.color = entry.View.ArrivalGlowBaseColor;
            }

            if (entry.View.Icon != null)
                entry.View.Icon.enabled = false;
        }

        /// <summary>
        /// Точка на квадратичной дуге Безье между start и end, выгнутой перпендикулярно линии
        /// полёта на arcOffset (со знаком - определяет сторону выгиба).
        /// </summary>
        private static Vector2 EvaluateArc(Vector2 start, Vector2 end, float arcOffset, float t)
        {
            var direction = end - start;
            var perpendicular = direction.sqrMagnitude > 0.0001f
                ? new Vector2(-direction.y, direction.x).normalized
                : Vector2.up;

            var control = Vector2.Lerp(start, end, 0.5f) + perpendicular * arcOffset;

            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * start
                   + 2f * oneMinusT * t * control
                   + t * t * end;
        }

        private bool TickArrival(ref Entry entry)
        {
            var duration = Mathf.Max(0.01f, _settings.ArrivalEffectDuration);
            var t = Mathf.Clamp01(entry.Elapsed / duration);

            // Короткий pop в первые ~35% (быстрый рост), затем вспышка плавно гаснет альфой -
            // орб не обрывается хлопком, а растворяется, пока идёт в пул на переиспользование.
            var growT = Mathf.Clamp01(t / 0.35f);
            var scale = Mathf.Lerp(1f, 1.35f, 1f - (1f - growT) * (1f - growT));
            entry.View.RectTransform.localScale = Vector3.one * scale;

            if (entry.View.ArrivalGlowImage != null)
            {
                var fadeT = Mathf.Clamp01((t - 0.2f) / 0.8f);
                var color = entry.View.ArrivalGlowBaseColor;
                color.a = Mathf.Lerp(entry.View.ArrivalGlowBaseColor.a, 0f, fadeT);
                entry.View.ArrivalGlowImage.color = color;
            }

            return t >= 1f;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private bool TryGetLocalPoint(Vector3 worldPosition, RectTransform parent, out Vector2 localPoint)
        {
            var screenPoint = _camera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z < 0f)
            {
                localPoint = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out localPoint);
        }

        private TowerExperienceOrbView Rent(RectTransform parent)
        {
            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled == null)
                    continue;

                pooled.transform.SetParent(parent, false);
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            return Object.Instantiate(_settings.OrbPrefab, parent);
        }

        private void Recycle(int index)
        {
            var view = _active[index].View;
            _active.RemoveAt(index);

            if (view == null)
                return;

            view.gameObject.SetActive(false);
            _pool.Push(view);
        }

        private void WarnMissingSetupOnce(string reason)
        {
            if (_warnedMissingSetup)
                return;

            _warnedMissingSetup = true;
            Debug.LogWarning($"[TowerExperienceOrbSystem] Визуальный дроп опыта отключён: {reason}. " +
                              "Начисление опыта не затронуто — это чисто косметический эффект.");
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<TowerExperienceDroppedSignal>(OnExperienceDropped);

            foreach (var entry in _active)
            {
                if (entry.View != null)
                    Object.Destroy(entry.View.gameObject);
            }

            _active.Clear();

            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled != null)
                    Object.Destroy(pooled.gameObject);
            }
        }

        private struct Entry
        {
            public TowerExperienceOrbView View;
            public Phase CurrentPhase;
            public float Elapsed;
            public Vector2 StartPoint;
            public Vector2 ScatterPoint;
            public Vector2 FlightStartPoint;
            public float FlightDuration;
            public float ArcSide;
            public float ArcHeight;
            public int CreditAmount;
        }
    }
}
