using UnityEngine;
using UnityEngine.Rendering;

namespace Db
{
    /// <summary>
    /// Настройки живой витрины башни (магазин + главный экран меню): кадрирование, темп спавна
    /// манекенов и источники боевых чисел. Числа атаки/скорости снаряда берутся из тех же
    /// конфигов, что и бой, - витрина ничего не выдумывает, поэтому правки баланса и визуала
    /// подхватываются ей автоматически.
    /// </summary>
    [CreateAssetMenu(menuName = "Settings/" + nameof(TowerPreviewSettings), fileName = nameof(TowerPreviewSettings))]
    public class TowerPreviewSettings : ScriptableObject
    {
        [Header("Источники боевых чисел (те же, что в бою)")]
        [SerializeField] private TowerConfigSettings towerConfigSettings;
        [SerializeField] private BulletConfigSettings bulletConfigSettings;

        [Header("Кадр")]
        [Tooltip("Какую долю кадра занимает ТЕЛО башни, когда врагов в кадре нет. Кадр считается от " +
                 "фактических габаритов префаба (партиклы ауры не учитываются), поэтому вручную ничего " +
                 "подгонять не надо")]
        [SerializeField, Range(0.3f, 1f)] private float towerFillRatio = 0.8f;

        [Tooltip("Во сколько раз кадр отъезжает, пока в нём есть манекен. Тело башни физически меньше " +
                 "врага, поэтому при 80% кадра под башню врагу просто некуда влетать: в покое камера " +
                 "держит крупный план, на время боя плавно отъезжает. 1 = никогда не отъезжать")]
        [SerializeField, Min(1f)] private float combatFrameMultiplier = 2.6f;

        [Tooltip("Скорость плавного перехода между крупным планом и боевым кадром")]
        [SerializeField, Min(0.1f)] private float frameLerpSpeed = 2f;

        [Tooltip("Страховка на случай, если у префаба башни не нашлось ни одного меша")]
        [SerializeField, Min(0.1f)] private float fallbackTowerRadius = 0.5f;

        [Tooltip("Разрешение RenderTexture витрины (квадрат). 512 достаточно, больше бьёт по WebGL")]
        [SerializeField, Min(128)] private int textureResolution = 512;

        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.03f, 0.14f, 1f);

        [Header("Пост-обработка")]
        [Tooltip("Тот же Volume-профиль, что стоит в бою (Bloom + ColorAdjustments) - именно он даёт " +
                 "боевую 'сочность'. Ссылка на боевой ассет, а не копия: правки профиля видны и в витрине")]
        [SerializeField] private VolumeProfile postProcessingProfile;

        [Tooltip("Выключить, если пост-обработка окажется дорогой на слабых мобильных браузерах: " +
                 "витрина станет как раньше, всё остальное продолжит работать")]
        [SerializeField] private bool usePostProcessing = true;

        [Tooltip("HDR нужен, чтобы Bloom получил яркости выше единицы - без него свечение почти не видно")]
        [SerializeField] private bool useHdr = true;

        [Header("Манекены")]
        [Tooltip("Какой враг подлетает к башне. Берём самого простого - витрина про башню, а не про врагов")]
        [SerializeField] private EnemyDefinition enemyDefinition;

        [Tooltip("Пауза перед первым врагом после открытия витрины")]
        [SerializeField, Min(0f)] private float firstSpawnDelay = 1.5f;

        [Tooltip("Разброс паузы между спавнами: большие значения дают время просто рассмотреть башню")]
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(2.5f, 5f);

        [Tooltip("Сколько манекенов может быть в кадре одновременно (2+ нужно, чтобы читались Pierce и Splash)")]
        [SerializeField, Min(1)] private int maxAliveEnemies = 2;

        [Tooltip("Доля боевого радиуса кадра, на которой появляется враг. Больше 1 - враг влетает " +
                 "из-за края кадра, а не проявляется внутри него")]
        [SerializeField, Range(0.5f, 1.5f)] private float spawnRadiusRatio = 1.1f;

        [Tooltip("Ближе этого расстояния к башне манекен исчезает без взрыва - витрина не показывает урон по башне")]
        [SerializeField, Min(0.05f)] private float enemyDespawnDistance = 0.7f;

        [Tooltip("Множитель скорости манекена относительно боевой. Меньше 1 - враг успевает погибнуть " +
                 "в кадре, а не дойти до башни: витрина должна показывать, как башня работает, а не как она пропускает")]
        [SerializeField, Range(0.1f, 2f)] private float enemySpeedMultiplier = 0.55f;

        [Header("Бой")]
        [Tooltip("На каком расстоянии снаряд засчитывается попавшим (как distanceCheckValue в BulletHitSystem)")]
        [SerializeField, Min(0.001f)] private float bulletHitDistance = 0.05f;

        [Tooltip("Сколько секунд живёт партикл смерти манекена")]
        [SerializeField, Min(0f)] private float deathParticleLifetime = 2f;

        public TowerConfigSettings TowerConfigSettings => towerConfigSettings;
        public BulletConfigSettings BulletConfigSettings => bulletConfigSettings;

        public float TowerFillRatio => towerFillRatio;
        public float CombatFrameMultiplier => combatFrameMultiplier;
        public float FrameLerpSpeed => frameLerpSpeed;
        public float FallbackTowerRadius => fallbackTowerRadius;
        public int TextureResolution => textureResolution;
        public Color BackgroundColor => backgroundColor;
        public VolumeProfile PostProcessingProfile => postProcessingProfile;
        public bool UsePostProcessing => usePostProcessing;
        public bool UseHdr => useHdr;

        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public float FirstSpawnDelay => firstSpawnDelay;
        public Vector2 SpawnDelayRange => spawnDelayRange;
        public int MaxAliveEnemies => maxAliveEnemies;
        public float SpawnRadiusRatio => spawnRadiusRatio;
        public float EnemyDespawnDistance => enemyDespawnDistance;
        public float EnemySpeedMultiplier => enemySpeedMultiplier;

        public float BulletHitDistance => bulletHitDistance;
        public float DeathParticleLifetime => deathParticleLifetime;
    }
}
