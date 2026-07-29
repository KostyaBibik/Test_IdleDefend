using Systems.Actions;
using Systems.Initializable;
using Systems.RunTime;
using Systems.RunTime.Bullets;
using Systems.RunTime.Camera;
using Systems.RunTime.Enemies;
using Systems.RunTime.SideTower;
using Systems.RunTime.Tower;
using Systems.RunTime.UI;
using Components.Tower;
using Db;
using Enums;
using Helpers;
using Infrastructure.Impl;
using Services;
using Services.Impl;
using Signals;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private TowerConfigSettings towerConfigSettings;
        [SerializeField] private ShopCatalogConfig shopCatalogConfig;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SceneHandler sceneHandler;
        
        public override void InstallBindings()
        {
            InitializeSignals();
            
            Container.Bind<Camera>().FromInstance(mainCamera).AsSingle();

            Container.Rebind<SceneHandler>().FromInstance(sceneHandler).AsTransient();

            Container.Bind<IGameTimeProvider>().To<GameTimeProvider>().AsSingle().NonLazy();
            Container.Bind<ActiveBoostService>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<LevelService>().AsSingle().NonLazy();

            InstallGameSystems();

            BindAndCreateTowerView();

            Container.BindInterfacesAndSelfTo<EntityFactory>().AsSingle().NonLazy();

            BindEnemyComponents();

            BindBulletComponents();

            BindSideTowerComponents();

            Container.BindInterfacesAndSelfTo<CameraZoomSystem>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<LevelTimeProgressBarSystem>().AsSingle().NonLazy();

            BindServices();

            Container.Bind<GameInstaller>().FromInstance(this).AsSingle();
        }

        private void InitializeSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<InitializeTowerSignal>();
            Container.DeclareSignal<GameLoseSignal>();
            Container.DeclareSignal<TowerLostHealthSignal>();
            Container.DeclareSignal<TowerAddHealthSignal>();
            Container.DeclareSignal<DestroyEntitySignal>();
            Container.DeclareSignal<ShowRewardSignal>();
            Container.DeclareSignal<LevelWavesFinishedSignal>();
            Container.DeclareSignal<GameWinSignal>();
            Container.DeclareSignal<TowerLevelUpSignal>();
            Container.DeclareSignal<TowerBuffSelectedSignal>();
            Container.DeclareSignal<TowerDamageDealtSignal>();
            Container.DeclareSignal<TowerUltimateActivatedSignal>();
            Container.DeclareSignal<TowerExperienceChangedSignal>();
            Container.DeclareSignal<TowerExperienceDroppedSignal>();
            Container.DeclareSignal<TowerExperienceOrbArrivedSignal>();
        }
        
        private void InstallGameSystems()
        {
            Container.BindInterfacesAndSelfTo<GameInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LoseActionSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<WinActionSystem>().AsSingle().NonLazy();
            // Всплывающие монеты за смерть врага отключены: их место заняли числа урона.
            // Начисление монет живёт в CoinService и не затронуто; ShowRewardSystem оставлен
            // невключённым, чтобы эффект можно было вернуть под другие награды.
            Container.BindInterfacesAndSelfTo<ShowDamageNumbersSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerExperienceOrbSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ShakeCamOnDamageSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SurvivalTimeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerLevelUpSystem>().AsSingle().NonLazy();
        }

        private void BindAndCreateTowerView()
        {
            // Игрок мог ни разу не открыть магазин — EquippedShopItemIds тогда пуст. ShopWindowView
            // сам вызывает EnsureDefaultEquipped при показе вкладки; здесь делаем то же самое, чтобы
            // выбор prefab-варианта не зависел от того, заходил ли игрок в магазин. TowerInitializeSystem
            // ниже независимо повторяет этот же лукап для чисел атаки/ультимейта — оба обращения
            // дешёвые и идемпотентные, ветвиться не из-за чего.
            ShopInventoryService.EnsureDefaultEquipped(shopCatalogConfig, EShopTab.Tower);
            var equippedBody = ShopInventoryService.GetEquippedItem(shopCatalogConfig, EShopTab.Tower)?.TowerBody;

            var prefab = equippedBody != null && equippedBody.TowerPrefabVariant != null
                ? equippedBody.TowerPrefabVariant.gameObject
                : towerConfigSettings.PrefabViewTower.gameObject;

            var towerView = Container.InstantiatePrefabForComponent<TowerView>(
                prefab,
                sceneHandler.TowerPos.position,
                Quaternion.identity,
                null
            );

            Container.BindInterfacesAndSelfTo<TowerView>().FromInstance(towerView).AsCached().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerAttackSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<FreezeWaveSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerUltimateSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerUltimateVfxSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeRadiusSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeHealthSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeAttackSpeedSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeAttackDamageSystem>().AsSingle().NonLazy();
            var healthComponent = Container.InstantiateComponent<TowerHealthComponent>(towerView.gameObject);
            Container.BindInterfacesAndSelfTo<TowerHealthComponent>().FromInstance(healthComponent).AsSingle().NonLazy();
        }
        
        private void BindEnemyComponents()
        {
            Container.BindInterfacesAndSelfTo<EnemySpawnInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyMovingSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyCheckToHitSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyPoisonSystem>().AsSingle().NonLazy();
        }
        
        private void BindBulletComponents()
        {
            Container.BindInterfacesAndSelfTo<BulletService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<BulletMovingSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<BulletHitSystem>().AsSingle().NonLazy();
        }
        
        private void BindSideTowerComponents()
        {
            Container.BindInterfacesAndSelfTo<SideTowerService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SideTowerAttackSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SideTowerBeamSystem>().AsSingle().NonLazy();
            // Комбинирует ауру SlowAura с тайм-замедлением от главной башни (Frost) — см. EnemySpeedModifierSystem.
            Container.BindInterfacesAndSelfTo<EnemySpeedModifierSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SideTowerSlotService>().AsSingle().NonLazy();
        }

        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<UpgradeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<CoinService>().AsSingle();
            Container.BindInterfacesAndSelfTo<TowerExperienceService>().AsSingle();
            Container.Bind<TowerBuffRuntimeService>().AsSingle();
            Container.Bind<TowerBuffRollService>().AsSingle();
            Container.Bind<TowerBuffSelectionService>().AsSingle();
            Container.Bind<TowerLevelUpUiService>().AsSingle();
        }
    }
}
