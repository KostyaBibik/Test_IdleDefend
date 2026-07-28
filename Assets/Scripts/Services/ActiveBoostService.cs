using System.Collections.Generic;
using Db;
using Enums;
using UnityEngine;

namespace Services
{
    public class ActiveBoostService
    {
        private readonly ShopCatalogConfig _catalog;
        private bool _loaded;
        private int _shieldHitsRemaining;

        public ActiveBoostService(ShopCatalogConfig catalog)
        {
            _catalog = catalog;
        }

        public float DamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;
        public float RangeMultiplier { get; private set; } = 1f;
        public float CoinRewardMultiplier { get; private set; } = 1f;
        public IReadOnlyList<ShopItemDefinition> ActiveBoosts { get; private set; } = new List<ShopItemDefinition>();

        public void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;
            ActiveBoosts = BoostInventoryService.ConsumeSelectedBoosts(_catalog);
            Recalculate();
        }

        public bool TryBlockTowerDamage()
        {
            EnsureLoaded();

            if (_shieldHitsRemaining <= 0)
                return false;

            _shieldHitsRemaining--;
            return true;
        }

        private void Recalculate()
        {
            DamageMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            RangeMultiplier = 1f;
            CoinRewardMultiplier = 1f;
            _shieldHitsRemaining = 0;

            foreach (var boost in ActiveBoosts)
            {
                switch (boost.BoostEffectType)
                {
                    case EBoostEffectType.TowerDamagePercent:
                        DamageMultiplier += Mathf.Max(0f, boost.BoostValue);
                        break;
                    case EBoostEffectType.TowerAttackSpeedPercent:
                        AttackSpeedMultiplier += Mathf.Max(0f, boost.BoostValue);
                        break;
                    case EBoostEffectType.TowerRangePercent:
                        RangeMultiplier += Mathf.Max(0f, boost.BoostValue);
                        break;
                    case EBoostEffectType.CoinRewardPercent:
                        CoinRewardMultiplier += Mathf.Max(0f, boost.BoostValue);
                        break;
                    case EBoostEffectType.ShieldHits:
                        _shieldHitsRemaining += Mathf.Max(0, Mathf.RoundToInt(boost.BoostValue));
                        break;
                }
            }
        }
    }
}
