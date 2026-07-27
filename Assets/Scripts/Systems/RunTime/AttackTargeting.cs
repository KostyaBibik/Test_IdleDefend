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
    }
}
