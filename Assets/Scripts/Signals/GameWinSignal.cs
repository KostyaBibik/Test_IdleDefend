using Db;
using Services;

namespace Signals
{
    public class GameWinSignal
    {
        public int stars;

        /// <summary>Разбивка начисленных гемов — экран победы показывает её построчно.</summary>
        public LevelRewardResult reward;

        /// <summary>Подарок за первое прохождение уровня. null — подарка на этот раз нет.</summary>
        public ShopItemDefinition unlockedItem;
    }
}
