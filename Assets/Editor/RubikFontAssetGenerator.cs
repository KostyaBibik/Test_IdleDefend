using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class RubikFontAssetGenerator
{
    private const string OutputFolder = "Assets/InternalAssets/Fonts/Rubik/TMP";
    private const int AtlasSize = 2048;
    private const string FallbackPath =
        "Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static readonly FontDefinition[] Fonts =
    {
        new("Rubik Black", "Assets/InternalAssets/Fonts/Rubik/static/Rubik-Black.ttf"),
        new("Rubik Medium", "Assets/InternalAssets/Fonts/Rubik/static/Rubik-Medium.ttf"),
        new("Rubik Regular", "Assets/InternalAssets/Fonts/Rubik/static/Rubik-Regular.ttf")
    };

    [InitializeOnLoadMethod]
    private static void GenerateOnceAfterImport()
    {
        EditorApplication.delayCall += () => Generate(replaceExisting: NeedsRegeneration());
    }

    [MenuItem("Tools/UI/Regenerate Rubik TMP Assets")]
    private static void RegenerateFromMenu()
    {
        Generate(replaceExisting: true);
    }

    public static void GenerateFromCommandLine()
    {
        Generate(replaceExisting: true);
    }

    private static void Generate(bool replaceExisting)
    {
        EnsureOutputFolder();

        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        if (fallback == null)
        {
            Debug.LogError($"[Rubik Fonts] Fallback font was not found at {FallbackPath}.");
            return;
        }

        var requiredCharacters = BuildRequiredCharacters();
        var changed = false;

        foreach (var definition in Fonts)
            changed |= GenerateFont(definition, fallback, requiredCharacters, replaceExisting);

        changed |= EnsureExistingPresetSettings();

        if (!changed)
            return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Rubik Fonts] Static RU/EN TMP font assets, presets and fallbacks are ready.");
    }

    private static bool GenerateFont(
        FontDefinition definition,
        TMP_FontAsset fallback,
        string requiredCharacters,
        bool replaceExisting)
    {
        var assetPath = $"{OutputFolder}/{definition.Name} SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
        {
            if (!replaceExisting)
                return false;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.DeleteAsset($"{OutputFolder}/{definition.Name} SDF - Outline.mat");
            AssetDatabase.DeleteAsset($"{OutputFolder}/{definition.Name} SDF - Outline Shadow.mat");
        }

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(definition.SourcePath);
        if (sourceFont == null)
        {
            Debug.LogError($"[Rubik Fonts] Source font was not found at {definition.SourcePath}.");
            return false;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            72,
            9,
            GlyphRenderMode.SDFAA,
            AtlasSize,
            AtlasSize,
            AtlasPopulationMode.Dynamic,
            false);

        if (fontAsset == null)
        {
            Debug.LogError($"[Rubik Fonts] Could not create {definition.Name}.");
            return false;
        }

        fontAsset.name = $"{definition.Name} SDF";
        fontAsset.atlasTextures[0].name = $"{definition.Name} SDF Atlas";
        fontAsset.material.name = $"{definition.Name} SDF Atlas Material";

        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        var allAdded = fontAsset.TryAddCharacters(requiredCharacters, out var missingCharacters, true);
        if (!allAdded || !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogError(
                $"[Rubik Fonts] {definition.Name} is missing required characters: {missingCharacters}",
                fontAsset);
        }

        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        fontAsset.isMultiAtlasTexturesEnabled = false;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.ReadFontAssetDefinition();

        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(fontAsset.atlasTextures[0]);
        EditorUtility.SetDirty(fontAsset.material);

        CreatePreset(fontAsset, definition.Name, withShadow: false);
        CreatePreset(fontAsset, definition.Name, withShadow: true);
        return true;
    }

    private static void CreatePreset(TMP_FontAsset fontAsset, string fontName, bool withShadow)
    {
        var suffix = withShadow ? "Outline Shadow" : "Outline";
        var material = new Material(fontAsset.material)
        {
            name = $"{fontName} SDF - {suffix}"
        };

        material.SetColor("_FaceColor", Color.white);
        material.SetColor("_OutlineColor", new Color(0.025f, 0.035f, 0.07f, 1f));
        material.SetFloat("_OutlineWidth", GetOutlineWidth(fontName));
        material.SetFloat("_OutlineSoftness", 0.02f);
        material.EnableKeyword("OUTLINE_ON");

        if (withShadow)
        {
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.72f));
            material.SetFloat("_UnderlayOffsetX", 1f);
            material.SetFloat("_UnderlayOffsetY", -1f);
            material.SetFloat("_UnderlayDilate", 0.08f);
            material.SetFloat("_UnderlaySoftness", 0.12f);
            material.EnableKeyword("UNDERLAY_ON");
        }

        AssetDatabase.CreateAsset(material, $"{OutputFolder}/{material.name}.mat");
    }

    private static bool EnsureExistingPresetSettings()
    {
        var changed = false;
        foreach (var definition in Fonts)
        {
            var expectedWidth = GetOutlineWidth(definition.Name);
            foreach (var suffix in new[] { "Outline", "Outline Shadow" })
            {
                var path = $"{OutputFolder}/{definition.Name} SDF - {suffix}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || Mathf.Approximately(material.GetFloat("_OutlineWidth"), expectedWidth))
                    continue;

                material.SetFloat("_OutlineWidth", expectedWidth);
                EditorUtility.SetDirty(material);
                changed = true;
            }
        }

        return changed;
    }

    private static float GetOutlineWidth(string fontName)
    {
        if (fontName.Contains("Black", StringComparison.Ordinal))
            return 0.18f;
        if (fontName.Contains("Medium", StringComparison.Ordinal))
            return 0.12f;
        return 0.07f;
    }

    private static string BuildRequiredCharacters()
    {
        var characters = new HashSet<char>();

        AddRange(characters, 0x0020, 0x007E); // English, digits and basic punctuation.
        AddRange(characters, 0x00A0, 0x00FF); // NBSP, guillemets and common currency marks.
        AddRange(characters, 0x0410, 0x044F); // Russian alphabet.

        foreach (var character in "Ёё–—‘’“”„…•‰₽№")
            characters.Add(character);

        return new string(characters.OrderBy(character => character).ToArray());
    }

    private static void AddRange(ISet<char> characters, int first, int last)
    {
        for (var codePoint = first; codePoint <= last; codePoint++)
            characters.Add((char) codePoint);
    }

    private static void EnsureOutputFolder()
    {
        const string parent = "Assets/InternalAssets/Fonts/Rubik";
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder(parent, "TMP");
    }

    private static bool NeedsRegeneration()
    {
        foreach (var definition in Fonts)
        {
            var path = $"{OutputFolder}/{definition.Name} SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fontAsset == null || fontAsset.atlasWidth != AtlasSize ||
                fontAsset.atlasHeight != AtlasSize ||
                fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
                return true;
        }

        return false;
    }

    private readonly struct FontDefinition
    {
        public FontDefinition(string name, string sourcePath)
        {
            Name = name;
            SourcePath = sourcePath;
        }

        public string Name { get; }
        public string SourcePath { get; }
    }
}
