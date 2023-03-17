using System.Collections;
using Infrastructure.Impl;
using UniRx;
using UnityEngine;
using Zenject;

namespace Systems.RunTime
{
    public class EnemySpawnSystem : IInitializable
    {
        private readonly EntityFactory _entityFactory;
        
        public EnemySpawnSystem(
            EntityFactory entityFactory
        )
        {
            _entityFactory = entityFactory;
        }

        private IEnumerator SpawnEnemyWithDelay()
        {
            do
            {
                yield return new WaitForSeconds(1f);

                _entityFactory.CreateEnemy(new Vector3(15f, 15f, 0f));
            } while (true);
        }

        public void Initialize()
        {
            Observable.FromCoroutine(SpawnEnemyWithDelay)
                .Subscribe();
        }
    }
}