using Systems.RunTime;
using UnityEngine.UI;
using Zenject;

namespace UI.Systems
{
    public class ChangeRadiusInputSystem : IInitializable
    {
        private TowerChangeRadiusSystem _towerChangeRadiusSystem;
        private readonly Button _upRadiusBtn;
        private readonly Button _downRadiusBtn;

        public ChangeRadiusInputSystem(
            Button upRadiusBtn,
            Button downRadiusBtn
        )
        {
            _upRadiusBtn = upRadiusBtn;
            _downRadiusBtn = downRadiusBtn;
        }

        [Inject]
        public void Construct(TowerChangeRadiusSystem towerChangeRadiusSystem)
        {
            _towerChangeRadiusSystem = towerChangeRadiusSystem;
        }

        public void Initialize()
        {
            _upRadiusBtn.onClick.AddListener(delegate
            {
                _towerChangeRadiusSystem.UpRadius(.1f);
            });
            
            _downRadiusBtn.onClick.AddListener(delegate
            {
                _towerChangeRadiusSystem.DownRadius(.1f);
            });
        }
    }
}