namespace Services
{
    public struct TowerBuffRuntimeStats
    {
        public float DamageMultiplier;
        public float AttackSpeedMultiplier;
        public float RangeMultiplier;
        public float CoinRewardMultiplier;
        public float CritChance;
        public float CritDamageMultiplier;
        public int AdditionalForwardShots;
        public int BackShots;
        public int RicochetCount;
        public float RicochetRadius;
        public float RicochetFalloff;
        public int PiercingLineCount;
        public float PiercingLineWidth;
        public float PiercingLineFalloff;
        public float ExplosiveShotRadius;
        public float ExplosiveShotFalloff;
        public float OverloadAttackSpeedMultiplier;
        public float OverloadDuration;
        public int MultishotRepeats;

        public static TowerBuffRuntimeStats Default => new TowerBuffRuntimeStats
        {
            DamageMultiplier = 1f,
            AttackSpeedMultiplier = 1f,
            RangeMultiplier = 1f,
            CoinRewardMultiplier = 1f,
            CritChance = 0f,
            CritDamageMultiplier = 1f,
            AdditionalForwardShots = 0,
            BackShots = 0,
            RicochetCount = 0,
            RicochetRadius = 2.5f,
            RicochetFalloff = 0.7f,
            PiercingLineCount = 0,
            PiercingLineWidth = 0.45f,
            PiercingLineFalloff = 0.75f,
            ExplosiveShotRadius = 0f,
            ExplosiveShotFalloff = 0.45f,
            OverloadAttackSpeedMultiplier = 1f,
            OverloadDuration = 0f,
            MultishotRepeats = 0
        };
    }
}
