using System;
using System.Collections.Generic;
using Infrastructure.Impl;
using Signals;
using Views;
using Views.Impl;
using Zenject;

namespace Services.Impl
{
    public class BulletService : IEntityService, IDisposable
    {
        private readonly List<BulletView> _bullets = new List<BulletView>();

        [Inject] private IEntityPoolService _entityPoolService;

        public List<BulletView> Bullets => _bullets;

        public void AddEntityOnService(IEntityView entityView)
        {
            var view = (BulletView) entityView;
            _bullets.Add(view);
        }

        public void RemoveEntityFromService(DestroyEntitySignal signal) { }

        public void RemoveEntityFromService(IEntityView entityView)
        {
            var view = (BulletView) entityView;
            if (_bullets.Contains(view))
            {
                ReleaseReservation(view);
                _bullets.Remove(view);
                _entityPoolService.Return(view);
            }
        }

        /// <summary>
        /// Снаряд исчезает — если он резервировал урон на живой цели и так и не попал, резерв
        /// надо вернуть. Иначе враг с резервом больше своего HP навсегда пропадает из
        /// EnemyService.GetAssumedActiveEnemies: башни считают его уже убитым и не стреляют,
        /// а он живой доходит до базы. После состоявшегося попадания reservedDamage уже обнулён
        /// (см. BulletHitSystem.HandleHit), поэтому двойного возврата тут не будет.
        /// </summary>
        private static void ReleaseReservation(BulletView view)
        {
            if (view.reservedDamage <= 0)
                return;

            var target = view.target;
            if (target != null && !target.isDestroyed && target.poolVersion == view.targetPoolVersion)
                target.healthComponent.ReleaseAssumedHealth(view.reservedDamage);

            view.reservedDamage = 0;
        }

        private void RemoveAllEnemies()
        {
            foreach (var bulletView in _bullets)
            {
                _entityPoolService.Return(bulletView);
            }

            _bullets.Clear();
        }

        public void Dispose()
        {
            RemoveAllEnemies();
        }
    }
}