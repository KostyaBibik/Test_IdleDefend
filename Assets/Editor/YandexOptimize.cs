#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// On-demand оптимизатор размера билда. НЕ запускается автоматически — только когда YandexBuild
/// сообщил, что несжатый размер превысил 100 МБ (лимит Яндекса). По умолчанию оригиналы не трогаем.
/// Запуск: меню 'Yandex/Optimize Assets (size)' или -executeMethod YandexOptimize.OptimizeAssets.
/// Изменения обратимы (ручной Reimport вернёт исходные настройки импорта).
/// </summary>
public static class YandexOptimize
{
    private const int MaxTextureSize = 2048;   // потолок размера текстур; ужесточать при необходимости
    private const float VorbisQuality = 0.6f;  // качество Vorbis для аудио

    [MenuItem("Yandex/Optimize Assets (size)")]
    public static void OptimizeAssets()
    {
        int tex = OptimizeTextures();
        int aud = OptimizeAudio();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[YandexOptimize] Готово. Текстур изменено: {tex}, аудио: {aud}. Пересобери билд.");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static int OptimizeTextures()
    {
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;

            bool dirty = false;
            if (ti.maxTextureSize > MaxTextureSize) { ti.maxTextureSize = MaxTextureSize; dirty = true; }
            if (ti.textureCompression == TextureImporterCompression.Uncompressed)
            {
                ti.textureCompression = TextureImporterCompression.Compressed;
                dirty = true;
            }

            if (dirty) { ti.SaveAndReimport(); changed++; }
        }
        return changed;
    }

    private static int OptimizeAudio()
    {
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;

            var settings = ai.defaultSampleSettings;
            bool dirty = false;
            if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                dirty = true;
            }
            if (settings.quality > VorbisQuality)
            {
                settings.quality = VorbisQuality;
                dirty = true;
            }

            if (dirty) { ai.defaultSampleSettings = settings; ai.SaveAndReimport(); changed++; }
        }
        return changed;
    }
}
#endif
