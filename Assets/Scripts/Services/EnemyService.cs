using System.Collections.Generic;
using Views;

namespace Services
{
    public class EnemyService
    {
        private readonly List<EnemyView> enemies = new List<EnemyView>();

        public List<EnemyView> Enemies => enemies;
        
        public void AddEnemyOnService(EnemyView enemyView)
        {
            enemies.Add(enemyView);
        }

        public void RemoveEnemyFromService(EnemyView enemyView)
        {
            if (enemies.Contains(enemyView))
            {
                enemies.Remove(enemyView);
            }
        }
    }
}