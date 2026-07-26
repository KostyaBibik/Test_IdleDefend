using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace VYandexTools.Localization.Editor
{
    public static class LocalizationSetupCreator
    {
        private const string DataPath = "Assets/VYandexTools/Localization/Data";
        private const string LocalesPath = DataPath + "/Locales";
        private const string TableDataPath = DataPath + "/TableData";
        private const string SettingsPath = DataPath + "/LocalizationSettings.asset";
        private const string TableName = "LocalizationTable";

        private static readonly LocaleDefinition[] LocaleDefinitions =
        {
            new LocaleDefinition("ru", "Russian"),
            new LocaleDefinition("en", "English"),
            new LocaleDefinition("em", "Empty")
        };

        [MenuItem("Tools/Localization/Create Localization Setup")]
        public static void CreateLocalizationSetup()
        {
            EnsureFolder(DataPath);
            EnsureFolder(LocalesPath);
            EnsureFolder(TableDataPath);

            var settings = GetOrCreateSettings();
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            var locales = new List<Locale>(LocaleDefinitions.Length);
            var createdLocales = 0;

            foreach (var definition in LocaleDefinitions)
            {
                var locale = GetOrCreateLocale(definition, out var wasCreated);
                if (wasCreated)
                    createdLocales++;

                if (LocalizationEditorSettings.GetLocale(locale.Identifier) == null)
                    LocalizationEditorSettings.AddLocale(locale);

                locales.Add(locale);
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            var collectionCreated = collection == null;

            if (collectionCreated)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    TableName,
                    TableDataPath,
                    locales);
            }

            var createdTables = 0;
            foreach (var locale in locales)
            {
                if (collection.GetTable(locale.Identifier) != null)
                    continue;

                var tablePath = $"{TableDataPath}/{TableName}_{locale.Identifier.Code}.asset";
                collection.AddNewTable(locale.Identifier, tablePath);
                createdTables++;
            }

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = collection;
            Debug.Log(
                $"Localization setup is ready. " +
                $"Locales created: {createdLocales}, " +
                $"collection created: {collectionCreated}, " +
                $"tables added to existing collection: {createdTables}.",
                collection);
        }

        private static LocalizationSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings != null)
                return settings;

            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "LocalizationSettings";
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static Locale GetOrCreateLocale(LocaleDefinition definition, out bool wasCreated)
        {
            var identifier = new LocaleIdentifier(definition.Code);
            var locale = LocalizationEditorSettings.GetLocale(identifier);

            if (locale == null)
            {
                var expectedPath = $"{LocalesPath}/{definition.Code}.asset";
                locale = AssetDatabase.LoadAssetAtPath<Locale>(expectedPath);

                if (locale == null)
                    locale = FindLocaleAsset(identifier);

                if (locale == null)
                {
                    locale = Locale.CreateLocale(identifier);
                    locale.name = definition.Name;
                    locale.LocaleName = definition.Name;
                    AssetDatabase.CreateAsset(locale, expectedPath);
                    wasCreated = true;
                    return locale;
                }
            }

            wasCreated = false;
            return locale;
        }

        private static Locale FindLocaleAsset(LocaleIdentifier identifier)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Locale"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(path);

                if (locale != null && locale.Identifier == identifier)
                    return locale;
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var separatorIndex = path.LastIndexOf('/');
            var parentPath = path.Substring(0, separatorIndex);
            var folderName = path.Substring(separatorIndex + 1);

            EnsureFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private readonly struct LocaleDefinition
        {
            public LocaleDefinition(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public string Code { get; }
            public string Name { get; }
        }
    }
}
