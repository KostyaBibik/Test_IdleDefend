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
                _bullets.Remove(view);
                _entityPoolService.Return(view);
            }
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