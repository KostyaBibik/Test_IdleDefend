using UnityEngine;

namespace UI.Views.Game
{
    /// <summary>
    /// Настройки визуального дропа опыта: сколько орбов спавнить на один
    /// TowerExperienceDroppedSignal, как они разлетаются и летят к бару, и куда именно лететь.
    /// Сам по себе компонент ничего не анимирует — это просто сцен-носитель конфигурации
    /// (в том числе ссылки на RectTransform бара, которую ScriptableObject дать не может),
    /// вся логика полёта — в TowerExperienceOrbSystem. См. UiInstaller за биндингом.
    /// </summary>
    public class TowerExperienceOrbSpawnerView : MonoBehaviour
    {
        [Header("Префаб орба")]
        [SerializeField] private TowerExperienceOrbView orbPrefab;

        [Header("Количество орбов на один дроп опыта")]
        [SerializeField, Min(1)] private int minOrbCount = 4;
        [SerializeField, Min(1)] private int maxOrbCount = 9;

        [Header("Фаза разлёта")]
        [Tooltip("Радиус, в пределах которого орбы разлетаются от точки смерти врага, прежде чем полететь к бару")]
        [SerializeField, Min(0f)] private float scatterRadius = 85f;
        [SerializeField, Min(0.01f)] private float scatterDuration = 0.26f;

        [Header("Пауза перед полётом")]
        [Tooltip("Сколько орб держится в точке разлёта, прежде чем начать лететь к бару — " +
                 "даёт игроку время это прочитать, а не увидеть смазанный рывок")]
        [SerializeField, Min(0f)] private float delayBeforeFlight = 0.32f;

        [Header("Полёт к бару")]
        [Tooltip("Полёт заметно длиннее разлёта — иначе орб долетает быстрее, чем глаз успевает его поймать")]
        [SerializeField, Min(0.1f)] private float flightDurationMin = 0.55f;
        [SerializeField, Min(0.1f)] private float flightDurationMax = 0.9f;
        [Tooltip("Форма скорости полёта. По умолчанию — медленный старт и ускорение к концу " +
                 "(как будто бар притягивает орб), а не равномерный или резкий рывок")]
        [SerializeField] private AnimationCurve flightCurve = new(
            new Keyframe(0f, 0f, 0f, 0.35f),
            new Keyframe(1f, 1f, 2.4f, 0f));
        [Tooltip("Насколько сильно траектория выгибается в сторону (дуга-\"свуп\", как у сбора опыта в Archero) " +
                 "вместо прямой линии к бару. 0 — лететь строго по прямой")]
        [SerializeField, Min(0f)] private float flightArcHeightMin = 40f;
        [SerializeField, Min(0f)] private float flightArcHeightMax = 110f;

        [Header("Прилёт")]
        [Tooltip("Длительность pop-вспышки и её плавного затухания в момент, когда орб долетает до бара")]
        [SerializeField, Min(0.05f)] private float arrivalEffectDuration = 0.22f;

        [Header("Цель полёта")]
        [Tooltip("RectTransform полосы опыта в HUD. Если не назначен — визуальный дроп просто " +
                 "не спавнится (начисление опыта при этом не затронуто, см. TowerExperienceService)")]
        [SerializeField] private RectTransform targetBarTransform;

        [Header("Безопасность")]
        [Tooltip("Потолок одновременно летящих орбов — защита от лагов при массовой смерти врагов")]
        [SerializeField, Min(1)] private int maxActiveOrbs = 40;

        public TowerExperienceOrbView OrbPrefab => orbPrefab;
        public int MinOrbCount => minOrbCount;
        public int MaxOrbCount => maxOrbCount;
        public float ScatterRadius => scatterRadius;
        public float ScatterDuration => scatterDuration;
        public float DelayBeforeFlight => delayBeforeFlight;
        public float FlightDurationMin => flightDurationMin;
        public float FlightDurationMax => flightDurationMax;
        public AnimationCurve FlightCurve => flightCurve;
        public float FlightArcHeightMin => flightArcHeightMin;
        public float FlightArcHeightMax => flightArcHeightMax;
        public float ArrivalEffectDuration => arrivalEffectDuration;
        public RectTransform TargetBarTransform => targetBarTransform;
        public int MaxActiveOrbs => maxActiveOrbs;
    }
}
