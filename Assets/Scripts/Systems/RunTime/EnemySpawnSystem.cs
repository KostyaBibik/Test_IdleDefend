using System.Collections;
using Infrastructure.Impl;
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


            } while (true);
        }

        public void Initialize()
        {
            
        }
    }
}