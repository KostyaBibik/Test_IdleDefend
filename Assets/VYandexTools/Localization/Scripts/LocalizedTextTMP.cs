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
        private const string TableName = "LocalizationTable";

        [SerializeField] private TMP_Text textComponent;
        [SerializeField] private bool useEnumKey = true;
        [SerializeField] private LocalizationKey key;
        [SerializeField] private LocalizedString localizedString;

        private int _requestVersion;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            _requestVersion++;
        }

        public void SetLocale(LocalizationKey key)
        {
            useEnumKey = true;
            this.key = key;
            Refresh();
        }

        public void Refresh()
        {
            if (!textComponent)
                return;

            if (ClearForEmptyLocale())
                return;

            var operation = useEnumKey
                ? LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TableName, key.ToString())
                : localizedString.GetLocalizedStringAsync();

            LoadString(operation);
        }

        public void SetValue(object[] values)
        {
            if (ClearForEmptyLocale())
                return;

            LoadString(localizedString.GetLocalizedStringAsync(values));
        }

        public void SetValue(LocalizationKey key, object[] values)
        {
            if (ClearForEmptyLocale())
                return;

            var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                TableName, key.ToString(), values);

            LoadString(operation);
        }

        private bool ClearForEmptyLocale()
        {
            if (!LocalizationSettings.HasSettings ||
                LocalizationSettings.SelectedLocale == null ||
                !string.Equals(
                    LocalizationSettings.SelectedLocale.Identifier.Code,
                    "em",
                    System.StringComparison.OrdinalIgnoreCase))
                return false;

            _requestVersion++;
            if (textComponent)
                textComponent.text = string.Empty;

            return true;
        }

        private void LoadString(AsyncOperationHandle<string> operation)
        {
            var version = ++_requestVersion;
            operation.Completed += handle => OnStringLoaded(handle, version);
        }

        private void OnStringLoaded(AsyncOperationHandle<string> operation, int version)
        {
            if (version != _requestVersion || !this || !textComponent)
                return;

            if (operation.Status == AsyncOperationStatus.Succeeded)
                textComponent.text = operation.Result;
            else
                Debug.LogError("Failed to load localized string.", this);
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            Refresh();
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
