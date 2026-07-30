using Db;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// Результат начисления награды за забег. Экран победы показывает это построчно,
    /// поэтому части награды разделены, а не свёрнуты в одно число.
    /// </summary>
    public readonly struct LevelRewardResult
    {
        /// <summary>Звёзд заработано в этом забеге (0..3).</summary>
        public readonly int Stars;

        /// <summary>Сколько из них новые — только за них платят полную ставку.</summary>
        public readonly int NewStars;

        /// <summary>Ставка за одну звезду на этом уровне.</summary>
        public readonly int PerStar;

        /// <summary>Гемы за новые звёзды.</summary>
        public readonly int StarEmeralds;

        /// <summary>Утешительный бонус за уровень, где новых звёзд не заработано.</summary>
        public readonly int ReplayEmeralds;

        public int Total => StarEmeralds + ReplayEmeralds;
        public bool HasNewStars => NewStars > 0;

        public LevelRewardResult(int stars, int newStars, int perStar, int starEmeralds, int replayEmeralds)
        {
            Stars = stars;
            NewStars = newStars;
            PerStar = perStar;
            StarEmeralds = starEmeralds;
            ReplayEmeralds = replayEmeralds;
        }
    }

    /// <summary>
    /// Считает и начисляет гемы за забег.
    ///
    /// Платим за прирост звёзд, а не за факт прохождения: иначе выгоднее было бы фармить первый
    /// уровень, чем идти дальше. За повтор уровня, где всё уже выбито, даётся символический
    /// бонус — чтобы застрявший игрок мог накопить на расходники, но фарм оставался медленнее
    /// продвижения вперёд.
    /// </summary>
    public static class LevelRewardService
    {
        /// <summary>Доля от полной награды уровня, которая достаётся за повтор без новых звёзд.</summary>
        private const float ReplayRewardShare = 0.15f;

        /// <summary>
        /// Начисляет награду за забег и возвращает её разбивку. Сохранение звёзд — на стороне
        /// вызывающего: победа и поражение по-разному двигают прогресс по карте.
        /// </summary>
        public static LevelRewardResult GrantForRun(LevelDefinition level, int starsEarned)
        {
            if (level == null)
                return default;

            var stars = Mathf.Clamp(starsEarned, 0, LevelDefinition.MaxStars);
            var perStar = Mathf.Max(0, level.RewardEmeraldsPerStar);
            var bestStars = SaveSystem.GetLevelStars(level.LevelId);
            var newStars = Mathf.Max(0, stars - bestStars);

            var starEmeralds = newStars * perStar;

            // Бонус за повтор положен только за реально пройденный забег: если игрок не дожил
            // даже до первой звезды, платить не за что.
            var replayEmeralds = newStars == 0 && stars > 0
                ? Mathf.RoundToInt(perStar * LevelDefinition.MaxStars * ReplayRewardShare)
                : 0;

            var total = starEmeralds + replayEmeralds;
            if (total > 0)
                EmeraldWallet.Add(total);

            return new LevelRewardResult(stars, newStars, perStar, starEmeralds, replayEmeralds);
        }

        /// <summary>
        /// Выдаёт подарочный предмет уровня (башню или снаряд) и сразу его надевает, чтобы игрок
        /// увидел обновку в бою, а не искал её в магазине.
        ///
        /// Возвращает предмет, только если он действительно выдан сейчас: повторное прохождение
        /// и случай «игрок уже купил его сам» дают null, и экран победы про подарок молчит.
        /// </summary>
        public static ShopItemDefinition GrantUnlockItem(LevelDefinition level)
        {
            var item = level != null ? level.UnlockRewardItem : null;
            if (item == null)
                return null;

            if (!ShopInventoryService.TryGrantFree(item))
                return null;

            ShopInventoryService.TryEquip(item);
            return item;
        }
    }
}
