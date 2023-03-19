using Services.Impl;
using Zenject;

namespace Components.Impl
{
    public class EnemyHealthComponent : HealthComponent
    {
        private EnemyService _enemyService;
        
        [Inject]
        public void Construct(EnemyService enemyService)
        {
            _enemyService = enemyService;
        }

        protected override void Die()
        {
            _enemyService.RemoveEntityFromService(_entityView);
            
            base.Die();
        }
    }
}