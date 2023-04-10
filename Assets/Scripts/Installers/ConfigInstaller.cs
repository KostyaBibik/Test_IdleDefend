using Db;
using UnityEngine;
using Zenject;

namespace Installers
{
    [CreateAssetMenu(fileName = nameof(ConfigInstaller),
        menuName = "Installers/" + nameof(ConfigInstaller))]
    public class ConfigInstaller : ScriptableObjectInstaller<ConfigInstaller>
    {
        [SerializeField] private TowerConfigSettings towerConfigSettings;
        [SerializeField] private EnemyPrefabsConfig enemyPrefabsConfig;
        [SerializeField] private BulletConfigSettings bulletConfigSettings;
        [SerializeField] private UpgradeTowerConfigSettings upgradeTowerConfigSettings;
        [SerializeField] private LevelConfigSettings levelConfigSettings;
        [SerializeField] private VisualEffectsSettings visualEffectsSettings;
        
        public override void InstallBindings()
        {
            Container.BindInstance(towerConfigSettings);
            Container.BindInstance(enemyPrefabsConfig);
            Container.BindInstance(bulletConfigSettings);
            Container.BindInstance(upgradeTowerConfigSettings);
            Container.BindInstance(visualEffectsSettings);
        }
    }
}