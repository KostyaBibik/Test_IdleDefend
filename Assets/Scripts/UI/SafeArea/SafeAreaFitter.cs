using UnityEngine;

namespace UI.SafeArea
{
    /// <summary>
    /// Ужимает свой RectTransform до <see cref="Screen.safeArea"/> плюс ручные отступы.
    ///
    /// Нужен на веб-платформах (Яндекс.Игры и т.п.): на мобильных снизу висит закреплённый
    /// рекламный/промо-баннер платформы, который НЕ попадает в safeArea, поэтому под него
    /// резервируется место через <see cref="bottomInset"/>. Вырезы/скругления экрана при этом
    /// закрывает сам safeArea.
    ///
    /// RectTransform должен быть растянут на весь родитель. Компонент переписывает
    /// anchorMin/anchorMax в нормированные координаты области и обнуляет offset'ы —
    /// задавать их вручную на этом объекте не нужно.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Доп. отступы, px референс-разрешения канваса")]
        [SerializeField, Min(0f)] private float leftInset;
        [SerializeField, Min(0f)] private float rightInset;
        [SerializeField, Min(0f)] private float topInset;

        [Tooltip("Высота нижнего баннера платформы. Для Яндекс.Игр на мобильных вебе ~90-120.")]
        [SerializeField, Min(0f)] private float bottomInset = 110f;

        [Header("Референс-разрешение CanvasScaler (Match = Width)")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);

        private RectTransform _rect;
        private Rect _lastSafeArea = new Rect(0f, 0f, 0f, 0f);
        private Vector2Int _lastScreen = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (_rect == null)
                _rect = GetComponent<RectTransform>();

            Apply();
        }

        private void Update()
        {
            // Ориентация/ресайз окна на вебе и на устройстве — единственные события, после которых
            // safeArea реально меняется. Пересчитываем только тогда, а не каждый кадр.
            if (Screen.safeArea != _lastSafeArea
                || Screen.width != _lastScreen.x
                || Screen.height != _lastScreen.y
                || Screen.orientation != _lastOrientation)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null)
                return;

            int w = Mathf.Max(1, Screen.width);
            int h = Mathf.Max(1, Screen.height);
            Rect safe = Screen.safeArea;

            // px референса -> px экрана. Скейлер в режиме Match = Width масштабирует по ширине.
            float pxScale = w / Mathf.Max(1f, referenceResolution.x);

            float left = safe.xMin + leftInset * pxScale;
            float right = safe.xMax - rightInset * pxScale;
            float bottom = safe.yMin + bottomInset * pxScale;
            float top = safe.yMax - topInset * pxScale;

            left = Mathf.Clamp(left, 0f, w);
            right = Mathf.Clamp(right, left + 1f, w);
            bottom = Mathf.Clamp(bottom, 0f, h);
            top = Mathf.Clamp(top, bottom + 1f, h);

            _rect.anchorMin = new Vector2(left / w, bottom / h);
            _rect.anchorMax = new Vector2(right / w, top / h);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(w, h);
            _lastOrientation = Screen.orientation;
        }
    }
}
