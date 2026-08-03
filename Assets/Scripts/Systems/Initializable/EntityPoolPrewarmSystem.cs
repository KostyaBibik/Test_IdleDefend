using Db;
using Infrastructure.Impl;
using Zenject;

namespace Systems.Initializable
{
    /// <summary>
    /// Прогревает EntityPoolService по значениям PoolPrewarmCount из EnemyDefinition/BulletConfigSettings
    /// - выполняется один раз при старте сцены, чтобы первые Instantiate под волну/выстрелы не
    /// случались прямо во время геймплея.
    /// </summary>
    public class EntityPoolPrewarmSystem : IInitializable
    {
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly BulletConfigSettings _bulletConfigSettings;
        private readonly IEntityPoolService _entityPoolService;

        public EntityPoolPrewarmSystem(
            EnemyPrefabsConfig enemyPrefabsConfig,
            BulletConfigSettings bulletConfigSettings,
            IEntityPoolService entityPoolService
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _bulletConfigSettings = bulletConfigSettings;
            _entityPoolService = entityPoolService;
        }

        public void Initialize()
        {
            foreach (var definition in _enemyPrefabsConfig.Definitions)
                _entityPoolService.Prewarm(definition.ViewPrefab, definition.PoolPrewarmCount);

            _entityPoolService.Prewarm(_bulletConfigSettings.PrefabViewBullet, _bulletConfigSettings.PoolPrewarmCount);
        }
    }
}
