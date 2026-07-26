using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VYandexTools.Localization.Scripts
{
    public class LocalizedTextTMP : MonoBehaviour
    {
        [SerializeField] private TMP_Text textComponent;
        [SerializeField] private LocalizedString localizedString;

        private void OnEnable() =>
            LoadString(localizedString.GetLocalizedStringAsync());

        public void SetLocale(LocalizationKey key)
        {
            var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                "LocalizationTable", key.ToString());

            LoadString(operation);
        }

        public void SetValue(object[] values) =>
            LoadString(localizedString.GetLocalizedStringAsync(values));

        public void SetValue(LocalizationKey key, object[] values)
        {
            var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                "LocalizationTable", key.ToString(), values);

            LoadString(operation);
        }

        private void LoadString(AsyncOperationHandle<string> operation) =>
            operation.Completed += OnStringLoaded;

        private void OnStringLoaded(AsyncOperationHandle<string> operation)
        {
            if (!this || !textComponent)
                return;

            if (operation.Status == AsyncOperationStatus.Succeeded)
                textComponent.text = operation.Result;
            else
                Debug.LogError("Failed to load localized string.", this);
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (textComponent)
                return;
            textComponent = GetComponent<TMP_Text>();
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
