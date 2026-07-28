using Db;
using Enums;
using Services;
using Views.Impl;
using Zenject;

namespace Systems.Initializable
{
    public class TowerInitializeSystem : IInitializable
    {
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly ShopCatalogConfig _shopCatalogConfig;
        private readonly TowerView _towerView;

        public TowerInitializeSystem(
            TowerConfigSettings towerConfigSettings,
            ShopCatalogConfig shopCatalogConfig,
            TowerView towerView
        )
        {
            _towerConfigSettings = towerConfigSettings;
            _shopCatalogConfig = shopCatalogConfig;
            _towerView = towerView;
        }

        public void Initialize()
        {
            _towerView.attackDistance = _towerConfigSettings.RangeAttack;
            _towerView.attackSpeed = _towerConfigSettings.AttackSpeed;
            _towerView.attackDamage = _towerConfigSettings.AttackDamage;

            // Игрок мог ни разу не открыть магазин — EquippedShopItemIds тогда пуст. ShopWindowView
            // в этом случае сам вызывает EnsureDefaultEquipped при показе вкладки; здесь делаем то же
            // самое, чтобы бой не зависел от того, заходил ли игрок в магазин.
            ShopInventoryService.EnsureDefaultEquipped(_shopCatalogConfig, EShopTab.Tower);

            ApplyTowerBody(ShopInventoryService.GetEquippedItem(_shopCatalogConfig, EShopTab.Tower)?.TowerBody);
        }

        private void ApplyTowerBody(TowerBodyDefinition body)
        {
            // Нет ни одного Tower-товара с TowerBody в каталоге вообще — не должно происходить
            // в собранной игре, но не должно и ронять бой: обычная атака, ультимейт недоступен.
            if (body == null)
            {
                _towerView.attackType = EMainTowerAttackType.Default;
                _towerView.ultimateCooldown = 0f;
                return;
            }

            _towerView.attackType = body.AttackType;

            _towerView.pierceCount = body.PierceCount;
            _towerView.pierceJumpRadius = body.PierceJumpRadius;
            _towerView.pierceFalloff = body.PierceFalloff;
            _towerView.pierceLineDuration = body.PierceLineDuration;
            _towerView.pierceLineRemaining = 0f;

            _towerView.frostSlowPercent = body.FrostSlowPercent;
            _towerView.frostSlowDuration = body.FrostSlowDuration;

            _towerView.splashRadius = body.SplashRadius;
            _towerView.splashFalloff = body.SplashFalloff;
            _towerView.splashImpactEffectPrefab = body.SplashImpactEffectPrefab;
            _towerView.splashImpactEffectReferenceRadius = body.SplashImpactEffectReferenceRadius;

            _towerView.ultimateName = body.UltimateName;
            _towerView.ultimateIcon = body.UltimateIcon;
            _towerView.ultimateCooldown = body.UltimateCooldown;
            _towerView.ultimateCooldownRemaining = 0f;

            _towerView.barrageAttackSpeedMultiplier = body.BarrageAttackSpeedMultiplier;
            _towerView.barrageDuration = body.BarrageDuration;
            _towerView.shatterDamagePercentOfMaxHealth = body.ShatterDamagePercentOfMaxHealth;
            _towerView.freezeDuration = body.FreezeDuration;
            _towerView.overloadSplashRadiusMultiplier = body.OverloadSplashRadiusMultiplier;
            _towerView.overloadDuration = body.OverloadDuration;

            // Визуал (цвет сферы, аура) больше не проставляется рантаймом — он уже запечён
            // в конкретном prefab-варианте TowerView, который выбрал GameInstaller при спавне
            // (см. body.TowerPrefabVariant и GameInstaller.BindAndCreateTowerView).
        }
    }
}
