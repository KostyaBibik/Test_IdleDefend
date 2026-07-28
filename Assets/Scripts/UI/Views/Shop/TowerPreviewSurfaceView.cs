using Db;
using Preview;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    /// <summary>
    /// UI-окно в живую витрину: показывает RenderTexture, который рисует TowerPreviewStage.
    /// Единственная точка, где UI встречается со сценой превью - и попап магазина, и главный
    /// экран меню используют её, а не работают со сценой напрямую.
    ///
    /// Для товаров без тела башни (бусты, эмеральды) откатывается на статичную иконку, поэтому
    /// компонент можно вешать в любое место магазина, не проверяя тип товара снаружи.
    /// </summary>
    public class TowerPreviewSurfaceView : MonoBehaviour
    {
        [SerializeField] private RawImage previewImage;
        [Tooltip("Показывается вместо живой витрины для товаров без 3D-модели")]
        [SerializeField] private Image fallbackIcon;
        [SerializeField] private TowerPreviewStage stagePrefab;

        private TowerPreviewStage _stage;

        /// <summary>
        /// Витрины разных мест (попап магазина и главный экран) живут в мировом пространстве на
        /// одном слое, поэтому разводим их по координатам - иначе они попадут друг другу в кадр.
        /// </summary>
        private static int _stageCounter;

        /// <summary>
        /// transparentBackground: витрина стоит прямо на фоне экрана (главное меню), а не в рамке
        /// попапа - фон кадра тогда не рисуется.
        /// </summary>
        public void Show(ShopItemDefinition item, bool transparentBackground = false)
        {
            EnsureStage();

            var shown = _stage != null && _stage.Show(item, transparentBackground);

            if (previewImage != null)
            {
                previewImage.gameObject.SetActive(shown);
                previewImage.texture = shown ? _stage.Texture : null;
            }

            if (fallbackIcon != null)
            {
                fallbackIcon.gameObject.SetActive(!shown);
                fallbackIcon.sprite = item != null ? item.Icon : null;
            }
        }

        public void Hide()
        {
            if (_stage != null)
                _stage.Clear();

            if (previewImage != null)
                previewImage.texture = null;
        }

        private void EnsureStage()
        {
            if (_stage != null || stagePrefab == null)
                return;

            var position = new Vector3(1000f + _stageCounter * 100f, -1000f, 0f);
            _stageCounter++;

            _stage = Instantiate(stagePrefab, position, Quaternion.identity);
            _stage.name = $"{stagePrefab.name}_{name}";
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            if (_stage != null)
                Destroy(_stage.gameObject);

            _stage = null;
        }
    }
}
