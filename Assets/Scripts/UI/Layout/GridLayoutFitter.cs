using UnityEngine;
using UnityEngine.UI;

namespace UI.Layout
{
    /// <summary>
    /// Пересчитывает <see cref="GridLayoutGroup.cellSize"/> так, чтобы ВСЕ активные ячейки
    /// (по числу реально показанных товаров) влезали в текущий размер контейнера.
    ///
    /// Нужно на вебе: когда платформа (Яндекс.Игры) ужимает вьюпорт под нижний баннер,
    /// сетка с фиксированным cellSize вылезает за свои границы и наезжает на подписи снизу —
    /// текст становится нечитаемым. Здесь карточки вместо этого пропорционально уменьшаются
    /// под доступное место. ScrollRect не требуется.
    ///
    /// Ставится на тот же объект, что и <see cref="GridLayoutGroup"/>.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    [DisallowMultipleComponent]
    public class GridLayoutFitter : MonoBehaviour
    {
        [Tooltip("Базовый (максимальный) размер ячейки. Пропорции карточки берутся отсюда, " +
                 "больше этого ячейка не станет.")]
        [SerializeField] private Vector2 maxCellSize = new Vector2(488f, 630f);

        [Tooltip("Нижняя граница масштаба ячейки относительно базового размера.")]
        [SerializeField, Range(0.1f, 1f)] private float minScale = 0.5f;

        [Tooltip("Кол-во колонок, если у GridLayoutGroup не задан FixedColumnCount.")]
        [SerializeField, Min(1)] private int fallbackColumns = 2;

        private GridLayoutGroup _grid;
        private RectTransform _rect;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();

        // Набор видимых карточек и высота контейнера могут меняться без ресайза самого rect
        // (переключение вкладки магазина, появление баннера) — поэтому пересчитываем в LateUpdate.
        // Присваивание того же cellSize внутри GridLayoutGroup — no-op, лишних rebuild'ов нет.
        private void LateUpdate() => Fit();

        private void Fit()
        {
            if (_grid == null)
                _grid = GetComponent<GridLayoutGroup>();
            if (_rect == null)
                _rect = GetComponent<RectTransform>();
            if (_grid == null || _rect == null)
                return;

            int columns = _grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, _grid.constraintCount)
                : Mathf.Max(1, fallbackColumns);

            int itemCount = 0;
            for (int i = 0; i < _rect.childCount; i++)
            {
                if (_rect.GetChild(i).gameObject.activeSelf)
                    itemCount++;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)columns));

            float availW = _rect.rect.width
                           - _grid.padding.left - _grid.padding.right
                           - _grid.spacing.x * (columns - 1);
            float availH = _rect.rect.height
                           - _grid.padding.top - _grid.padding.bottom
                           - _grid.spacing.y * (rows - 1);

            if (availW <= 1f || availH <= 1f || maxCellSize.x <= 0f || maxCellSize.y <= 0f)
                return;

            float scale = Mathf.Min(
                availW / columns / maxCellSize.x,
                availH / rows / maxCellSize.y,
                1f);
            scale = Mathf.Max(scale, minScale);

            _grid.cellSize = new Vector2(maxCellSize.x * scale, maxCellSize.y * scale);
        }
    }
}
