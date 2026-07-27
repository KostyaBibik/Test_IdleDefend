using Db;
using Enums;
using Services;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime
{
    public class EnemyMovingSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;

        private const float speedRotating = 2f;

        public EnemyMovingSystem(
            EnemyService enemyService,
            TowerView towerView,
            IGameTimeProvider gameTimeProvider
        )
        {
            _enemyService = enemyService;
            _towerView = towerView;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            foreach (var enemy in _enemyService.Enemies)
            {
                MoveToTower(enemy);
            }
        }

        private void MoveToTower(EnemyView enemy)
        {
            var towerPos = _towerView.transform.position;
            var deltaTime = _gameTimeProvider.DeltaTime;
            var newPos = enemy.definition != null && enemy.definition.MovementType == EEnemyMovementType.Orbit
                ? MoveOrbit(enemy, towerPos, deltaTime)
                : MoveLinear(enemy, towerPos, deltaTime);

            var enemyPos = enemy.transform.position;
            enemy.transform.position = newPos;

            var angle = Mathf.Atan2(towerPos.y - enemyPos.y, towerPos.x - enemyPos.x ) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
            enemy.Mesh.rotation = Quaternion.RotateTowards(enemy.Mesh.rotation, targetRotation, speedRotating * deltaTime);
        }

        private static Vector3 MoveLinear(EnemyView enemy, Vector3 towerPos, float deltaTime)
        {
            return Vector3.MoveTowards(
                enemy.transform.position,
                towerPos,
                deltaTime * enemy.speedMoving * enemy.speedMultiplier);
        }

        private static Vector3 MoveOrbit(EnemyView enemy, Vector3 towerPos, float deltaTime)
        {
            var definition = enemy.definition;

            if (float.IsNaN(enemy.orbitAngleDeg))
            {
                var offset = enemy.transform.position - towerPos;
                enemy.orbitAngleDeg = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                enemy.orbitRadius = offset.magnitude;
            }

            enemy.orbitAngleDeg += definition.OrbitAngularSpeedDegPerSec * deltaTime * enemy.speedMultiplier;
            enemy.orbitRadius = Mathf.Max(0f, enemy.orbitRadius - definition.OrbitRadiusShrinkSpeed * deltaTime);

            var rad = enemy.orbitAngleDeg * Mathf.Deg2Rad;
            var offsetFromTower = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * enemy.orbitRadius;

            return towerPos + offsetFromTower;
        }
    }
}
