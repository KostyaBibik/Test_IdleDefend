using Db;
using UnityEngine;
using Zenject;

namespace Systems.RunTime.Enemies
{
    public class IncreasingEnemyParametersSystem : ITickable
    {
        private readonly IncreaseEnemiesSettings _increaseEnemiesSettings;

        public int additiveHealth { get; private set; }
        public float additiveSpeed { get; private set; }
        private float _expensiveTime;
        
        public IncreasingEnemyParametersSystem(
            IncreaseEnemiesSettings increaseEnemiesSettings
            )
        {
            _increaseEnemiesSettings = increaseEnemiesSettings;
        }
        
        public void Tick()
        {
            _expensiveTime += Time.deltaTime;

            if (_expensiveTime >= _increaseEnemiesSettings.TimeDelayForBoost)
            {
                _expensiveTime = 0;
                additiveHealth += _increaseEnemiesSettings.IncreaseHealthValue;
                additiveSpeed += _increaseEnemiesSettings.IncreaseSpeedValue;
            }
        }
    }
}