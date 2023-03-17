using UI.Systems;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Installers
{
    public class UiInstaller : MonoInstaller
    {
        [SerializeField] private Button upRadiusBtn;
        [SerializeField] private Button downRadiusBtn;
        
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<ChangeRadiusInputSystem>()
                .AsSingle()
                .WithArguments(upRadiusBtn, downRadiusBtn)
                .NonLazy();
        }
    }
}