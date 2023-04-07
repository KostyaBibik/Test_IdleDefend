using System;
using System.Collections.Generic;
using Signals;
using Views;
using Views.Impl;
using Zenject;
using Object = UnityEngine.Object;

namespace Services.Impl
{
    public class BulletService : IEntityService, IInitializable, IDisposable
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

        public void Initialize()
        {
           
        }

        public void Dispose()
        {
           
        }
    }
}