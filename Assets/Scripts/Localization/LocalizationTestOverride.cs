using UnityEngine;

namespace Game.Localization
{
    public static class LocalizationTestOverride
    {
        public const string PlayerPrefsKey = "IdleDefend.Localization.TestLocale";

        public static string LocaleCode
        {
            get => PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    Clear();
                else
                    PlayerPrefs.SetString(PlayerPrefsKey, value.Trim());

                PlayerPrefs.Save();
            }
        }

        public static bool HasLocaleCode => !string.IsNullOrWhiteSpace(LocaleCode);

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
