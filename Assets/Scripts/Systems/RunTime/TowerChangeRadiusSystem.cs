using Db;
using UnityEngine;
using Views;
using Zenject;

namespace Systems.RunTime
{
    public class TowerChangeRadiusSystem : IInitializable
    {
        private readonly TowerView _towerView;
        
        public TowerChangeRadiusSystem(
            TowerView towerView
        )
        {
            _towerView = towerView;
        }

        public void ChangeRadius(float newRadius)
        {
            _towerView.attackDistance = newRadius;
            _towerView.Sphere.localScale = new Vector3(newRadius, newRadius, newRadius);
        }

        public void UpRadius(float addValue)
        {
            _towerView.attackDistance += addValue;
            _towerView.Sphere.localScale += new Vector3(addValue, addValue, addValue);
        }
        
        public void DownRadius(float subValue)
        {
            _towerView.attackDistance -= subValue;
            _towerView.Sphere.localScale -= new Vector3(subValue, subValue, subValue);
        }

        public void Initialize()
        {
            ChangeRadius(_towerView.attackDistance);
        }
    }
}