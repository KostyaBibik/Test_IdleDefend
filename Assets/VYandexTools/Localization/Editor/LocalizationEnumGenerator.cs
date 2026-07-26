using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;

namespace VYandexTools.Localization.Editor
{
    public static class LocalizationEnumGenerator
    {
        private const string TABLE_NAME = "LocalizationTable";
        private const string SAVE_PATH = "Assets/VYandexTools/Localization/Data/LocalizationKey.cs";

        [MenuItem("Tools/Localization/Generate Enum")]
        public static void Generate()
        {
            var table = LocalizationEditorSettings.GetStringTableCollection(TABLE_NAME);

            if (table == null)
            {
                UnityEngine.Debug.LogError($"Не удалось найти таблицу {TABLE_NAME}");
                return;
            }

            var builder = new StringBuilder();

            builder.AppendLine("public enum LocalizationKey");
            builder.AppendLine("{");

            foreach (var key in table.SharedData.Entries)
            {
                var keyName = RemoveInvalidCharacters(key.Key);

                builder.AppendLine($"    {keyName},");
            }

            builder.AppendLine("}");

            Directory.CreateDirectory(Path.GetDirectoryName(SAVE_PATH));

            File.WriteAllText(SAVE_PATH, builder.ToString());

            AssetDatabase.Refresh();

            UnityEngine.Debug.Log("Localization enum generated!");
        }

        private static string RemoveInvalidCharacters(string value)
        {
            value = value.Replace(" ", "_");
            value = value.Replace("-", "_");

            return value;
        }
    }
}