using Db;
using Installers;
using UnityEngine;
using Views;

namespace Infrastructure.Impl
{
    public class EntityFactory : IFactory
    {
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        
        public EntityFactory(
            EnemyPrefabsConfig enemyPrefabsConfig
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
        }
        
        public void CreateEnemy(Vector3 posSpawn)
        {
            var enemyView = DiContainerRef.Container.InstantiatePrefabForComponent<EnemyView>(_enemyPrefabsConfig.EnemyView);
            var enemyTransform = enemyView.transform;
            enemyTransform.position = posSpawn;
            enemyTransform.rotation = Quaternion.identity;
        }
    }
}