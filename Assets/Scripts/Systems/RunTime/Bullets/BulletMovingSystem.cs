using Db;
using Services;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Bullets
{
    public class BulletMovingSystem : ITickable
    {
        private readonly BulletConfigSettings _bulletConfigSettings;
        private readonly BulletService _bulletService;
        private readonly IGameTimeProvider _gameTimeProvider;

        public BulletMovingSystem(
            BulletConfigSettings bulletConfigSettings,
            BulletService bulletService,
            IGameTimeProvider gameTimeProvider
            )
        {
            _bulletConfigSettings = bulletConfigSettings;
            _bulletService = bulletService;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            for (var i = _bulletService.Bullets.Count - 1; i >= 0; i--)
            {
                var bulletView = _bulletService.Bullets[i];
                if (bulletView.target == null || bulletView.target.isDestroyed)
                {
                    if (!MoveFreeFlight(bulletView))
                        _bulletService.RemoveEntityFromService(bulletView);

                    continue;
                }

                MoveToTarget(bulletView, bulletView.target.transform);
            }
        }

        private void MoveToTarget(BulletView bulletView, Transform target)
        {
             var bullet = bulletView.transform;
             var targetPos = target.position;
             var bulletPos = bullet.position;
             var direction = targetPos - bulletPos;
             var speedMoving = _bulletConfigSettings.SpeedMoving * bulletView.speedMultiplier;
             var step = _gameTimeProvider.DeltaTime * speedMoving;

             bulletView.previousPosition = bulletPos;
             if (direction.sqrMagnitude > 0.0001f)
                 bulletView.freeFlightDirection = direction.normalized;
 
             bullet.transform.position = Vector3.MoveTowards(
                 bulletPos,
                 targetPos,
                 step);
             if (direction.sqrMagnitude > 0.0001f)
                 bullet.transform.LookAt(bullet.position + direction);
        }

        private bool MoveFreeFlight(BulletView bulletView)
        {
            if (!bulletView.continueOnTargetLost
                || (bulletView.freeFlightRemainingDistance <= 0f && bulletView.freeFlightRemainingSeconds <= 0f))
                return false;

            var direction = bulletView.freeFlightDirection.sqrMagnitude > 0.0001f
                ? bulletView.freeFlightDirection.normalized
                : bulletView.launchDirection.normalized;

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            var speedMoving = _bulletConfigSettings.SpeedMoving * bulletView.speedMultiplier;
            var rawStep = _gameTimeProvider.DeltaTime * speedMoving;
            var step = bulletView.freeFlightRemainingDistance > 0f
                ? Mathf.Min(rawStep, bulletView.freeFlightRemainingDistance)
                : rawStep;
            var bulletTransform = bulletView.transform;

            bulletView.previousPosition = bulletTransform.position;
            bulletTransform.position += direction * step;
            if (bulletView.freeFlightRemainingDistance > 0f)
                bulletView.freeFlightRemainingDistance -= step;
            if (bulletView.freeFlightRemainingSeconds > 0f)
                bulletView.freeFlightRemainingSeconds -= _gameTimeProvider.DeltaTime;
            bulletTransform.LookAt(bulletTransform.position + direction);

            return bulletView.freeFlightRemainingDistance > 0f || bulletView.freeFlightRemainingSeconds > 0f;
        }
    }
}
