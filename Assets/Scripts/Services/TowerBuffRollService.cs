using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using UI.Views;
using UnityEngine;

namespace Services
{
    public class TowerBuffRollService
    {
        private readonly TowerBuffCatalogConfig _catalog;
        private readonly TowerBuffRuntimeService _runtime;
        private readonly TowerHealthHandler _towerHealthHandler;

        public TowerBuffRollService(
            TowerBuffCatalogConfig catalog,
            TowerBuffRuntimeService runtime,
            TowerHealthHandler towerHealthHandler)
        {
            _catalog = catalog;
            _runtime = runtime;
            _towerHealthHandler = towerHealthHandler;
            _runtime.RegisterCatalog(catalog);
        }

        public IReadOnlyList<TowerBuffDefinition> RollChoices(int level)
        {
            if (_catalog == null)
                return new List<TowerBuffDefinition>();

            // Редкость роллится один раз на весь выбор, чтобы все 3 карточки были одной
            // редкости (либо все Common, либо все Rare, либо все Legendary) - вероятности
            // редкости при этом не меняются, меняется только то, что она общая для сета.
            var rarity = RollRarity(level);
            var result = new List<TowerBuffDefinition>(_catalog.ChoicesCount);
            var excluded = new HashSet<string>();

            for (var i = 0; i < _catalog.ChoicesCount; i++)
            {
                var buff = RollByRarity(rarity, excluded) ?? RollFallback(excluded);
                if (buff == null)
                    break;

                result.Add(buff);
                excluded.Add(buff.Id);
            }

            return result;
        }

        private TowerBuffDefinition RollFallback(HashSet<string> excluded)
        {
            var fallback = _catalog.GetEnabledBuffs()
                .Where(candidate => IsAvailable(candidate, excluded))
                .ToList();

            return PickWeighted(fallback);
        }

        private TowerBuffDefinition RollByRarity(ETowerBuffRarity rarity, HashSet<string> excluded)
        {
            var candidates = _catalog.GetEnabledBuffs(rarity)
                .Where(candidate => IsAvailable(candidate, excluded))
                .ToList();

            return PickWeighted(candidates);
        }

        private bool IsAvailable(TowerBuffDefinition buff, HashSet<string> excluded)
        {
            return buff != null
                   && !string.IsNullOrEmpty(buff.Id)
                   && !excluded.Contains(buff.Id)
                   && _runtime.CanApply(buff)
                   && CanRollByCurrentState(buff);
        }

        private bool CanRollByCurrentState(TowerBuffDefinition buff)
        {
            if (buff.EffectType == ETowerBuffEffectType.HealTower)
                return _towerHealthHandler == null || _towerHealthHandler.CanUpHealth();

            return true;
        }

        private ETowerBuffRarity RollRarity(int level)
        {
            if (level <= _catalog.GuaranteedCommonUntilLevel)
                return ETowerBuffRarity.Common;

            var total = _catalog.CommonChance + _catalog.RareChance + _catalog.LegendaryChance;
            if (total <= 0f)
                return ETowerBuffRarity.Common;

            var roll = Random.value * total;
            if (roll < _catalog.LegendaryChance)
                return ETowerBuffRarity.Legendary;

            roll -= _catalog.LegendaryChance;
            if (roll < _catalog.RareChance)
                return ETowerBuffRarity.Rare;

            return ETowerBuffRarity.Common;
        }

        private static TowerBuffDefinition PickWeighted(IReadOnlyList<TowerBuffDefinition> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            var total = candidates.Sum(candidate => Mathf.Max(0f, candidate.Weight));
            if (total <= 0f)
                return candidates[Random.Range(0, candidates.Count)];

            var roll = Random.value * total;
            foreach (var candidate in candidates)
            {
                roll -= Mathf.Max(0f, candidate.Weight);
                if (roll <= 0f)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }
    }
}
