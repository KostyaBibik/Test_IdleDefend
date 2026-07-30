using Db;
using TMPro;
using UnityEngine;

namespace UI.Views.Menu
{
    /// <summary>
    /// Прогресс игрока на главном экране: сколько уровней пройдено и сколько звёзд собрано.
    ///
    /// Показывает только числа рядом с иконками ("6/10", "18/30") — намеренно без подписей,
    /// чтобы не заводить новые ключи локализации в трёх таблицах ради двух строк.
    ///
    /// Пройденным считается уровень, за который есть хотя бы одна звезда: SaveSystem хранит
    /// именно звёзды, отдельного флага "пройден" в сейве нет.
    /// </summary>
    public class MenuProgressLabel : MonoBehaviour
    {
        [SerializeField] private LevelsConfig levelsConfig;
        [Tooltip("Формат: {0} — пройдено, {1} — всего уровней")]
        [SerializeField] private TMP_Text levelsLabel;
        [Tooltip("Формат: {0} — собрано звёзд, {1} — максимум")]
        [SerializeField] private TMP_Text starsLabel;

        public int CompletedLevels { get; private set; }
        public int TotalLevels { get; private set; }
        public int CollectedStars { get; private set; }
        public int TotalStars { get; private set; }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (levelsConfig == null)
                return;

            SaveSystem.EnsureRuntimeCollections();

            CompletedLevels = 0;
            CollectedStars = 0;
            TotalLevels = levelsConfig.Count;
            TotalStars = levelsConfig.Count * 3;

            foreach (var level in levelsConfig.Levels)
            {
                if (level == null)
                    continue;

                var stars = SaveSystem.GetLevelStars(level.LevelId);
                CollectedStars += stars;

                if (stars > 0)
                    CompletedLevels++;
            }

            if (levelsLabel != null)
                levelsLabel.text = $"{CompletedLevels}/{TotalLevels}";

            if (starsLabel != null)
                starsLabel.text = $"{CollectedStars}/{TotalStars}";
        }
    }
}
