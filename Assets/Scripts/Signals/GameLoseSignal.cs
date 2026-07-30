using Services;

namespace Signals
{
    public class GameLoseSignal
    {
        /// <summary>Звёзды за прожитое время: проигранный забег тоже может их принести.</summary>
        public int stars;

        /// <summary>Разбивка начисленных гемов — заполняется LoseActionSystem.</summary>
        public LevelRewardResult reward;
    }
}
