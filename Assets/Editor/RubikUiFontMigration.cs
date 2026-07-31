using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RubikUiFontMigration
{
    private const string FontFolder = "Assets/InternalAssets/Fonts/Rubik/TMP";
    private const string SessionKey = "IdleDefend.RubikUiFontMigration.v2";

    [InitializeOnLoadMethod]
    private static void MigrateOnceAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            if (!FontSet.TryLoad(out var fonts))
                return;

            SessionState.SetBool(SessionKey, true);
            Migrate(fonts);
        };
    }

    [MenuItem("Tools/UI/Migrate All UI Text To Rubik")]
    public static void MigrateFromMenu()
    {
        if (!FontSet.TryLoad(out var fonts))
        {
            Debug.LogError("[Rubik UI] Font assets are missing. Generate Rubik TMP assets first.");
            return;
        }

        Migrate(fonts);
    }

    private static void Migrate(FontSet fonts)
    {
        var report = new MigrationReport();
        MigratePrefabs(fonts, report);
        MigrateScenes(fonts, report);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[Rubik UI] Migration complete. " +
            $"Prefabs: {report.ChangedPrefabs}, scenes: {report.ChangedScenes}, " +
            $"texts: {report.ChangedTexts} " +
            $"(Black {report.Black}, Medium {report.Medium}, Regular {report.Regular}), " +
            $"serialized font fields: {report.ChangedSerializedFontFields}, " +
            $"reverted scene overrides: {report.RevertedOverrides}, skipped dirty scenes: {report.SkippedDirtyScenes}.");
    }

    private static void MigratePrefabs(FontSet fonts, MigrationReport report)
    {
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/VYandexTools" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Concat(new[]
            {
                "Assets/ExternalAssets/GUI PRO Kit - Casual Game/Prefabs/Prefabs_Component_Buttons/Btn_MainButton_Orange.prefab",
                "Assets/ExternalAssets/GUI PRO Kit - Casual Game/Prefabs/Prefabs_Component_Buttons/Btn_MainButton_Yellow.prefab"
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var prefabPath in prefabPaths)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var changed = false;
            try
            {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (IsNestedPrefabText(text, root))
                        continue;

                    changed |= MigrateText(text, fonts, report);
                }

                if (!changed)
                    continue;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                report.ChangedPrefabs++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static bool IsNestedPrefabText(TMP_Text text, GameObject prefabRoot)
    {
        var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(text.gameObject);
        return instanceRoot != null && instanceRoot != prefabRoot;
    }

    private static void MigrateScenes(FontSet fonts, MigrationReport report)
    {
        var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes", "Assets/VYandexTools/Scene" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var scenePath in scenePaths)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            var wasLoaded = scene.IsValid() && scene.isLoaded;

            if (wasLoaded && scene.isDirty)
            {
                report.SkippedDirtyScenes++;
                Debug.LogWarning($"[Rubik UI] Skipped dirty scene to preserve unsaved work: {scenePath}");
                continue;
            }

            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var changed = false;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    changed |= MigrateSceneText(text, fonts, report);

                    changed |= MigrateSerializedFontFields(root, fonts, report);
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.ChangedScenes++;
                }
            }
            finally
            {
                if (!wasLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static bool MigrateSceneText(TMP_Text text, FontSet fonts, MigrationReport report)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(text.gameObject))
            return MigrateText(text, fonts, report);

        var sourceText = PrefabUtility.GetCorrespondingObjectFromSource(text);
        if (sourceText == null || !fonts.Contains(sourceText.font))
            return MigrateText(text, fonts, report);

        var oldFont = text.font;
        var oldMaterial = text.fontSharedMaterial;
        var serializedText = new SerializedObject(text);
        RevertProperty(serializedText.FindProperty("m_fontAsset"));
        RevertProperty(serializedText.FindProperty("m_sharedMaterial"));
        serializedText.UpdateIfRequiredOrScript();
        if (text.font == oldFont && text.fontSharedMaterial == oldMaterial)
            return false;

        report.RevertedOverrides++;
        report.ChangedTexts++;
        CountRole(ChooseRole(text), report);
        EditorUtility.SetDirty(text);
        return true;
    }

    private static void RevertProperty(SerializedProperty property)
    {
        if (property != null && property.prefabOverride)
            PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
    }

    private static bool MigrateText(TMP_Text text, FontSet fonts, MigrationReport report)
    {
        var role = ChooseRole(text);
        fonts.Get(role, out var font, out var material);
        var changed = false;

        if (text.font != font || text.fontSharedMaterial != material)
        {
            text.font = font;
            text.fontSharedMaterial = material;
            changed = true;
        }

        if (!changed)
            return false;

        EditorUtility.SetDirty(text);

        report.ChangedTexts++;
        CountRole(role, report);
        return true;
    }

    private static bool MigrateSerializedFontFields(GameObject root, FontSet fonts, MigrationReport report)
    {
        var changed = false;
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null || component is TMP_Text)
                continue;

            var serializedComponent = new SerializedObject(component);
            var iterator = serializedComponent.GetIterator();
            var componentChanged = false;

            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                    iterator.objectReferenceValue is not TMP_FontAsset)
                    continue;

                var role = ChooseSerializedFieldRole(iterator.name);
                fonts.Get(role, out var font, out var material);
                var fieldChanged = false;
                if (iterator.objectReferenceValue != font)
                {
                    iterator.objectReferenceValue = font;
                    componentChanged = true;
                    fieldChanged = true;
                }

                var materialProperty = serializedComponent.FindProperty(iterator.name + "Material");
                if (materialProperty != null && materialProperty.objectReferenceValue != material)
                {
                    materialProperty.objectReferenceValue = material;
                    componentChanged = true;
                    fieldChanged = true;
                }

                if (fieldChanged)
                {
                    CountRole(role, report);
                    report.ChangedSerializedFontFields++;
                }
            }

            if (!componentChanged)
                continue;

            serializedComponent.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            changed = true;
        }

        return changed;
    }

    private static FontRole ChooseSerializedFieldRole(string propertyName)
    {
        var value = propertyName.ToLowerInvariant();
        if (ContainsAny(value, "body", "description", "instruction", "hint"))
            return FontRole.Regular;
        if (ContainsAny(value, "title", "header"))
            return FontRole.Black;
        return FontRole.Medium;
    }

    private static FontRole ChooseRole(TMP_Text text)
    {
        var objectName = text.gameObject.name.ToLowerInvariant();
        var parentName = text.transform.parent != null
            ? text.transform.parent.name.ToLowerInvariant()
            : string.Empty;
        var identity = objectName + " " + parentName;

        if (ContainsAny(identity,
                "description", "body", "subtitle", "hint", "requirement", "message", "details", "info"))
            return FontRole.Regular;

        if (text.GetComponentInParent<Button>(true) != null ||
            ContainsAny(identity,
                "title", "header", "level", "stage", "amount", "price", "count", "value", "number", "score") ||
            text.fontSize >= 34f)
            return FontRole.Black;

        var contentLength = string.IsNullOrEmpty(text.text) ? 0 : text.text.Length;
        if (text.fontSize <= 26f && (text.enableWordWrapping || contentLength > 34))
            return FontRole.Regular;

        return FontRole.Medium;
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
        foreach (var fragment in fragments)
            if (value.Contains(fragment))
                return true;

        return false;
    }

    private static void CountRole(FontRole role, MigrationReport report)
    {
        switch (role)
        {
            case FontRole.Black:
                report.Black++;
                break;
            case FontRole.Medium:
                report.Medium++;
                break;
            case FontRole.Regular:
                report.Regular++;
                break;
        }
    }

    private enum FontRole
    {
        Black,
        Medium,
        Regular
    }

    private sealed class FontSet
    {
        private TMP_FontAsset Black { get; set; }
        private TMP_FontAsset Medium { get; set; }
        private TMP_FontAsset Regular { get; set; }
        private Material BlackMaterial { get; set; }
        private Material MediumMaterial { get; set; }
        private Material RegularMaterial { get; set; }

        public static bool TryLoad(out FontSet fonts)
        {
            fonts = new FontSet
            {
                Black = Load<TMP_FontAsset>("Rubik Black SDF.asset"),
                Medium = Load<TMP_FontAsset>("Rubik Medium SDF.asset"),
                Regular = Load<TMP_FontAsset>("Rubik Regular SDF.asset"),
                BlackMaterial = Load<Material>("Rubik Black SDF - Outline Shadow.mat"),
                MediumMaterial = Load<Material>("Rubik Medium SDF - Outline Shadow.mat"),
                RegularMaterial = Load<Material>("Rubik Regular SDF - Outline.mat")
            };

            return fonts.Black != null && fonts.Medium != null && fonts.Regular != null &&
                   fonts.BlackMaterial != null && fonts.MediumMaterial != null && fonts.RegularMaterial != null;
        }

        public bool Contains(TMP_FontAsset font)
        {
            return font == Black || font == Medium || font == Regular;
        }

        public void Get(FontRole role, out TMP_FontAsset font, out Material material)
        {
            switch (role)
            {
                case FontRole.Black:
                    font = Black;
                    material = BlackMaterial;
                    return;
                case FontRole.Regular:
                    font = Regular;
                    material = RegularMaterial;
                    return;
                default:
                    font = Medium;
                    material = MediumMaterial;
                    return;
            }
        }

        private static T Load<T>(string fileName) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{FontFolder}/{fileName}");
        }
    }

    private sealed class MigrationReport
    {
        public int ChangedPrefabs;
        public int ChangedScenes;
        public int ChangedTexts;
        public int RevertedOverrides;
        public int SkippedDirtyScenes;
        public int ChangedSerializedFontFields;
        public int Black;
        public int Medium;
        public int Regular;
    }
}
