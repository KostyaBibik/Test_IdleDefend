using System;
using System.Collections.Generic;
using Signals;
using Views;
using Views.Impl;
using Object = UnityEngine.Object;

namespace Services.Impl
{
    public class BulletService : IEntityService, IDisposable
    {
        private readonly List<BulletView> _bullets = new List<BulletView>();

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
                Object.Destroy(view.gameObject);
            }
        }
        
        private void RemoveAllEnemies()
        {
            foreach (var bulletView in _bullets)
            {
                Object.Destroy(bulletView.gameObject);
            }
            
            _bullets.Clear(); 
        }
        
        public void Dispose()
        {
            RemoveAllEnemies();
        }
    }
}