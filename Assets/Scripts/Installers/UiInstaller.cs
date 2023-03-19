﻿using UI.Systems;
using UI.Views;
using UI.Views.Upgradable;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class UiInstaller : MonoInstaller
    {
        [SerializeField] private UpgradeViewsHandler upgradeViewsHandler;
        [SerializeField] private CoinsUiView coinsUiView;
        
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<UpgradeViewsHandler>()
                .FromInstance(upgradeViewsHandler)
                .AsSingle()
                .NonLazy();
            
            Container
                .BindInterfacesAndSelfTo<UpgradeInputSystem>()
                .AsSingle()
                .NonLazy();
            
            Container
                .BindInterfacesAndSelfTo<CoinsUiView>()
                .FromInstance(coinsUiView)
                .AsSingle()
                .NonLazy();
        }
    }
}