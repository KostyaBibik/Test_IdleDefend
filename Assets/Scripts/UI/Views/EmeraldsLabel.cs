using Services;
using TMPro;
using UnityEngine;

namespace UI.Views
{
    /// <summary>
    /// Показывает баланс мета-валюты (эмеральдов). Работает без DI — используется в Menu.
    /// </summary>
    public class EmeraldsLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            Refresh();
            PlayerSaveData.OnEmeraldsChanged += Refresh;
        }

        private void OnDisable()
        {
            PlayerSaveData.OnEmeraldsChanged -= Refresh;
        }

        private void Refresh()
        {
            if (label != null)
                label.text = EmeraldWallet.Balance.ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (label == null)
                label = GetComponent<TMP_Text>();
        }
#endif
    }
}
