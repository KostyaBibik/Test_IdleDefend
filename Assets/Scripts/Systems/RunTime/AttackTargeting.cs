using System.Collections.Generic;
using UnityEngine;
using Views.Impl;

namespace Systems.RunTime
{
    public static class AttackTargeting
    {
        public static EnemyView FindNearestEnemy(Vector3 fromPosition, IList<EnemyView> enemies)
        {
            var nearest = enemies[0];
            var nearestDistance = Vector3.Distance(fromPosition, nearest.transform.position);

            for (var i = 1; i < enemies.Count; i++)
            {
                var distance = Vector3.Distance(fromPosition, enemies[i].transform.position);
                if (distance < nearestDistance)
                {
                    nearest = enemies[i];
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Ищет ближайшего к <paramref name="fromPosition"/> врага в радиусе <paramref name="radius"/>,
        /// исключая уже задетых <paramref name="alreadyHit"/>. Общая логика "прыжка" — используется
        /// и ChainLightning у доп-башен, и Pierce у главной башни (обе прыгают по ближайшим врагам).
        /// </summary>
        public static EnemyView FindNearestUnhit(
            Vector3 fromPosition,
            IList<EnemyView> allEnemies,
            ICollection<EnemyView> alreadyHit,
            float radius)
        {
            EnemyView nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var candidate in allEnemies)
            {
                if (alreadyHit.Contains(candidate))
                    continue;

                var distance = Vector3.Distance(fromPosition, candidate.transform.position);
                if (distance > radius)
                    continue;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }
}
