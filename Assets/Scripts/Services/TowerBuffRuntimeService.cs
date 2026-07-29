using System;
using System.Collections.Generic;
using Db;
using Enums;
using UnityEngine;

namespace Services
{
    public class TowerBuffRuntimeService
    {
        private readonly Dictionary<string, int> _stacks = new();

        public event Action OnChanged;

        public TowerBuffRuntimeStats Stats { get; private set; } = TowerBuffRuntimeStats.Default;

        public int GetStackCount(TowerBuffDefinition buff)
        {
            if (buff == null || string.IsNullOrEmpty(buff.Id))
                return 0;

            return _stacks.TryGetValue(buff.Id, out var count) ? count : 0;
        }

        public bool CanApply(TowerBuffDefinition buff)
        {
            return buff != null
                   && !string.IsNullOrEmpty(buff.Id)
                   && GetStackCount(buff) < buff.MaxStacks;
        }

        public bool ApplyBuff(TowerBuffDefinition buff)
        {
            if (!CanApply(buff))
                return false;

            _stacks[buff.Id] = GetStackCount(buff) + 1;
            RecalculateStats();
            OnChanged?.Invoke();
            return true;
        }

        private void RecalculateStats()
        {
            var stats = TowerBuffRuntimeStats.Default;

            foreach (var pair in _stacks)
            {
                var buff = TowerBuffRegistry.Get(pair.Key);
                if (buff == null)
                    continue;

                var value = Math.Max(0f, buff.Value) * pair.Value;
                switch (buff.EffectType)
                {
                    case ETowerBuffEffectType.DamageMultiplier:
                        stats.DamageMultiplier += value;
                        break;
                    case ETowerBuffEffectType.AttackSpeedMultiplier:
                        stats.AttackSpeedMultiplier += value;
                        break;
                    case ETowerBuffEffectType.RangeMultiplier:
                        stats.RangeMultiplier += value;
                        break;
                    case ETowerBuffEffectType.CoinRewardMultiplier:
                        stats.CoinRewardMultiplier += value;
                        break;
                    case ETowerBuffEffectType.CritChance:
                        stats.CritChance += value;
                        break;
                    case ETowerBuffEffectType.CritDamageMultiplier:
                        stats.CritDamageMultiplier += value;
                        break;
                    case ETowerBuffEffectType.DoubleShot:
                        stats.ParallelForwardShots += Mathf.Max(0, Mathf.RoundToInt(value));
                        break;
                    case ETowerBuffEffectType.TripleShot:
                        stats.ParallelForwardShots += Mathf.Max(0, Mathf.RoundToInt(value) * 2);
                        break;
                    case ETowerBuffEffectType.BackShot:
                        stats.BackShots += Mathf.Max(0, Mathf.RoundToInt(value));
                        break;
                    case ETowerBuffEffectType.Ricochet:
                        stats.RicochetCount += Mathf.Max(0, Mathf.RoundToInt(value));
                        break;
                    case ETowerBuffEffectType.PiercingShot:
                        stats.PiercingLineCount += Mathf.Max(0, Mathf.RoundToInt(value));
                        break;
                    case ETowerBuffEffectType.ExplosiveShot:
                        stats.ExplosiveShotRadius += value;
                        break;
                    case ETowerBuffEffectType.Overload:
                        stats.OverloadAttackSpeedMultiplier += value;
                        stats.OverloadDuration = Mathf.Max(stats.OverloadDuration, 3f);
                        break;
                    case ETowerBuffEffectType.Multishot:
                        stats.MultishotRepeats += Mathf.Max(0, Mathf.RoundToInt(value));
                        break;
                }
            }

            Stats = stats;
        }

        public void RegisterCatalog(TowerBuffCatalogConfig catalog)
        {
            TowerBuffRegistry.Register(catalog);
        }

        private static class TowerBuffRegistry
        {
            private static readonly Dictionary<string, TowerBuffDefinition> Definitions = new();

            public static void Register(TowerBuffCatalogConfig catalog)
            {
                Definitions.Clear();

                if (catalog == null)
                    return;

                foreach (var buff in catalog.Buffs)
                {
                    if (buff == null || string.IsNullOrEmpty(buff.Id))
                        continue;

                    Definitions[buff.Id] = buff;
                }
            }

            public static TowerBuffDefinition Get(string id)
            {
                return !string.IsNullOrEmpty(id) && Definitions.TryGetValue(id, out var buff) ? buff : null;
            }
        }
    }
}
