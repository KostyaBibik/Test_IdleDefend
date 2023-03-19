using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(UpgradeTowerConfigSettings),
        fileName = nameof(UpgradeTowerConfigSettings))]
    public class UpgradeTowerConfigSettings : ScriptableObject
    {
        [Header("Upgrade values")] 
        [SerializeField] private float upgradeRangeAttack = .15f;
        [SerializeField] private float upgradeAttackSpeed = .1f;
        [SerializeField] private int upgradeAttackDamage = 10;
        
        [Space, Header("Start cost upgrade")]
        [SerializeField] private int startCostRadiusUpgrade = 50;
        [SerializeField] private int startCostAttackSpeedUpgrade = 50;
        [SerializeField] private int startCostAttackDamageUpgrade = 50;
        
        [Space, Header("Cost upgrade")]
        [SerializeField] private int costRangeUpgrade = 10;
        [SerializeField] private int costAttackSpeedUpgrade = 10;
        [SerializeField] private int costAttackDamageUpgrade = 10;
        
        public float UpgradeRangeAttack => upgradeRangeAttack;
        public float UpgradeAttackSpeed => upgradeAttackSpeed;
        public int UpgradeAttackDamage => upgradeAttackDamage;
        
        public int CostRangeUpgrade => costRangeUpgrade;
        public int CostAttackSpeedUpgrade => costAttackSpeedUpgrade;
        public int CostAttackDamageUpgrade => costAttackDamageUpgrade;
        
        public int StartCostRadiusUpgrade => startCostRadiusUpgrade;
        public int StartCostAttackSpeedUpgrade => startCostAttackSpeedUpgrade;
        public int StartCostAttackDamageUpgrade => startCostAttackDamageUpgrade;
    }
}