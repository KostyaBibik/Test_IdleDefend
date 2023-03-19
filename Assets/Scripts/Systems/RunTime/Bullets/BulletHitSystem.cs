using Services.Impl;
using UnityEngine;
using Zenject;

namespace Systems.RunTime.Bullets
{
    public class BulletHitSystem : ITickable
    {
        private const float distanceCheckValue = 0.01f;
        
        private readonly BulletService _bulletService;
        
        public BulletHitSystem(
            BulletService bulletService
            )
        {
            _bulletService = bulletService;
        }
        
        public void Tick()
        {
            foreach (var bullet in _bulletService.Bullets)
            {
                if (bullet.target == null)
                    continue;
                
                
                if (Vector3.Distance(bullet.target.transform.position, bullet.transform.position) <= distanceCheckValue)
                {
                    _bulletService.RemoveEntityFromService(bullet);
                    bullet.target.healthComponent.ReduceHealth(bullet.damage);
                    
                    return;
                }
            }
        }
    }
}