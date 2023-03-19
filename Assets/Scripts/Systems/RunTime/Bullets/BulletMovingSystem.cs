using Db;
using Services.Impl;
using UnityEngine;
using Zenject;

namespace Systems.RunTime.Bullets
{
    public class BulletMovingSystem : ITickable
    {
        private readonly BulletConfigSettings _bulletConfigSettings;
        private readonly BulletService _bulletService;

        public BulletMovingSystem(
            BulletConfigSettings bulletConfigSettings,
            BulletService bulletService
            )
        {
            _bulletConfigSettings = bulletConfigSettings;
            _bulletService = bulletService;
        }

        public void Tick()
        {
            foreach (var bulletView in _bulletService.Bullets)
            {
                if (bulletView.target == null)
                {
                    _bulletService.RemoveEntityFromService(bulletView);
                    Debug.Log("RemoveEntityFromService");
                    return;
                }
                
                MoveToTarget(bulletView.transform, bulletView.target.transform);    
            }
        }
        
        private void MoveToTarget(Transform bullet, Transform target)
        {
             var targetPos = target.position;
             var bulletPos = bullet.position;
             var direction = targetPos - bulletPos;
             var speedMoving = _bulletConfigSettings.SpeedMoving;
                        
             bullet.transform.position = Vector3.MoveTowards(
                 bulletPos, 
                 targetPos,
                 Time.deltaTime * speedMoving);
             bullet.transform.LookAt(direction);
        }
    }
}