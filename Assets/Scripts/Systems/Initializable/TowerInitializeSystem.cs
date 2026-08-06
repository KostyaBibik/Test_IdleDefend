using Db;
using Enums;
using Services;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.Initializable
{
    public class TowerInitializeSystem : IInitializable
    {
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly ShopCatalogConfig _shopCatalogConfig;
        private readonly TowerView _towerView;
        private readonly ActiveBoostService _activeBoostService;

        public TowerInitializeSystem(
            TowerConfigSettings towerConfigSettings,
            ShopCatalogConfig shopCatalogConfig,
            TowerView towerView,
            ActiveBoostService activeBoostService
        )
        {
            _towerConfigSettings = towerConfigSettings;
            _shopCatalogConfig = shopCatalogConfig;
            _towerView = towerView;
            _activeBoostService = activeBoostService;
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

            ShopInventoryService.EnsureDefaultEquipped(_shopCatalogConfig, EShopTab.Projectiles);
            ApplyProjectile(ShopInventoryService.GetEquippedItem(_shopCatalogConfig, EShopTab.Projectiles)?.Projectile);

            ApplyActiveBoosts();
        }

        /// <summary>
        /// Бусты больше не вшиваются разово в базовые статы башни. Раньше "+10% урона" применялись
        /// к стартовым 30 единицам и давали +3 на весь бой: апгрейды за монеты аддитивные, поэтому
        /// вклад буста не рос вместе с башней и к концу уровня стремился к нулю. Теперь множители
        /// живут в рантайме и применяются в точке использования (TowerAttackSystem,
        /// TowerChangeRadiusSystem, CameraZoomSystem) — там же, где множители баффов, — и потому
        /// действуют на итоговый, уже прокачанный стат.
        /// </summary>
        private void ApplyActiveBoosts()
        {
            _activeBoostService.EnsureLoaded();
        }

        private void ApplyTowerBody(TowerBodyDefinition body)
        {
            // Нет ни одного Tower-товара с TowerBody в каталоге вообще — не должно происходить
            // в собранной игре, но не должно и ронять бой: обычная атака, ультимейт недоступен.
            if (body == null)
            {
                _towerView.attackType = EMainTowerAttackType.Default;
                _towerView.ultimateCooldown = 0f;
                _towerView.ultimateAuraPrefab = null;
                _towerView.ultimateBurstPrefab = null;
                _towerView.ultimateVfxScale = 1f;
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
            _towerView.ultimateAuraPrefab = body.UltimateAuraPrefab;
            _towerView.ultimateBurstPrefab = body.UltimateBurstPrefab;
            _towerView.ultimateVfxScale = body.UltimateVfxScale;

            _towerView.barrageAttackSpeedMultiplier = body.BarrageAttackSpeedMultiplier;
            _towerView.barrageDuration = body.BarrageDuration;
            _towerView.shatterDamagePercentOfMaxHealth = body.ShatterDamagePercentOfMaxHealth;
            _towerView.freezeDuration = body.FreezeDuration;
            _towerView.freezeWaveSpeed = body.FreezeWaveSpeed;
            _towerView.freezeWaveBurstEffectPrefab = body.FreezeWaveBurstEffectPrefab;
            _towerView.overloadSplashRadiusMultiplier = body.OverloadSplashRadiusMultiplier;
            _towerView.overloadDuration = body.OverloadDuration;

            // Визуал (цвет сферы, аура) больше не проставляется рантаймом — он уже запечён
            // в конкретном prefab-варианте TowerView, который выбрал GameInstaller при спавне
            // (см. body.TowerPrefabVariant и GameInstaller.BindAndCreateTowerView).
        }

        private void ApplyProjectile(ProjectileDefinition projectile)
        {
            if (projectile == null)
            {
                _towerView.projectilePrefabVariant = null;
                _towerView.projectileSpeedMultiplier = 1f;
                _towerView.projectileAppliesFrost = false;
                _towerView.projectileAppliesPoison = false;
                return;
            }

            _towerView.projectilePrefabVariant = projectile.BulletPrefabVariant;
            _towerView.projectileSpeedMultiplier = projectile.SpeedMultiplier;
            _towerView.attackDamage = Mathf.CeilToInt(_towerView.attackDamage * projectile.DamageMultiplier);

            _towerView.projectileAppliesFrost = projectile.AppliesFrost;
            _towerView.projectileFrostSlowPercent = projectile.FrostSlowPercent;
            _towerView.projectileFrostSlowDuration = projectile.FrostSlowDuration;

            _towerView.projectileAppliesPoison = projectile.AppliesPoison;
            _towerView.projectilePoisonDamagePercentPerTick = projectile.PoisonDamagePercentPerTick;
            _towerView.projectilePoisonTickInterval = projectile.PoisonTickInterval;
            _towerView.projectilePoisonDuration = projectile.PoisonDuration;
            _towerView.projectilePoisonVfxPrefab = projectile.PoisonVfxPrefab;
        }
    }
}
