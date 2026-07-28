using UnityEngine;
using Views.Impl;

namespace Signals
{
    public class TowerDamageDealtSignal
    {
        public EnemyView target;
        public Vector3 worldPos;
        public int damage;
        public bool isCritical;
        public bool isPrimaryHit;
    }
}
