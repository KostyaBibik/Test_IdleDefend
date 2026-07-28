using System;
using Enums;
using UnityEngine;

namespace Components
{
    [Serializable]
    public struct UpgradeContainer
    {
        [Header("Основные параметры")]
        public EUpgradeType upgradeType;
        public float upgradeValue;
        public int startCost;
        public int costUpgrade;

        [Header("Цикл прокачки")]
        [Tooltip("Сколько покупок нужно для заполнения прогресс-бара. После этого бар сбрасывается, а ранг растет на 1.")]
        public int maxLevel;
        [Tooltip("Множитель цены за каждый закрытый цикл. 1.35 = каждый следующий ранг примерно на 35% дороже.")]
        public float costRankMultiplier;
        [Tooltip("Множитель силы прироста за каждый закрытый цикл. 0.85 = каждый следующий ранг дает 85% от прироста прошлого.")]
        public float upgradeValueRankMultiplier;

        public int CycleLength => maxLevel > 0 ? maxLevel : 10;
        public float CostMultiplier => costRankMultiplier > 0f ? costRankMultiplier : 1.35f;
        public float ValueMultiplier => upgradeValueRankMultiplier > 0f ? upgradeValueRankMultiplier : 0.85f;
    }
}
