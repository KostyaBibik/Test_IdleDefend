using System.Collections.Generic;
using UnityEngine;
using Views;
using Views.Impl;

namespace Services.Impl
{
    public class BulletService : IEntityService
    {
        private readonly List<BulletView> _bullets = new List<BulletView>();

        public List<BulletView> Bullets => _bullets;
        
        public void AddEntityOnService(IEntityView entityView)
        {
            var view = (BulletView) entityView;
            _bullets.Add(view);
        }

        public void RemoveEntityFromService(IEntityView entityView)
        {
            var view = (BulletView) entityView;
            if (_bullets.Contains(view))
            {
                _bullets.Remove(view);
                Object.Destroy(view.gameObject);
            }
        }
    }
}