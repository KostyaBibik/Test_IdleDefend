#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agava.WebUtility;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Конвейерная сборка под Яндекс.Игры. Выставляет все WebGL-настройки программно,
/// заменяет EventSystem -> WebEventSystem во всех сценах билда и собирает WebGL.
/// Запуск (Unity закрыт):
///   Unity.exe -batchmode -quit -projectPath "путь" -executeMethod YandexBuild.BuildWebGL -logFile "лог.txt"
/// </summary>
public static class YandexBuild
{
    // --- per-game конфиг (для конвейера выносится в game.config.json) ---
    private const string Template = "PROJECT:AT2.1.1";
    private const int CanvasWidth = 540;   // портрет; для горизонтальной игры поменять местами
    private const int CanvasHeight = 960;
    private const string CanvasWidthEnv = "YANDEX_PORT_CANVAS_WIDTH";
    private const string CanvasHeightEnv = "YANDEX_PORT_CANVAS_HEIGHT";
    private const string OutputDirName = "Build_WebGL";
    private const string OutputDirEnv = "YANDEX_PORT_BUILD_OUTPUT"; // задаёт build.ps1 (типизированная папка)
    private const long YandexUncompressedLimitBytes = 100L * 1024 * 1024; // лимит Яндекса — 100 МБ несжатых

    [MenuItem("Yandex/Build WebGL")]
    public static void BuildWebGL()
    {
        YandexPortSceneSetup.PreparePortScenes();
        ApplyPlayerSettings();
        ReplaceEventSystems();
        PatchKimicuJslib();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        // Папку вывода задаёт конвейер (build.ps1) через env: <проект>\builds\<дата>\<Игра>_v<N>.
        // Фолбэк (ручной запуск из редактора) — старая папка <проект>\Build_WebGL.
        var outDir = System.Environment.GetEnvironmentVariable(OutputDirEnv);
        if (string.IsNullOrWhiteSpace(outDir))
            outDir = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, OutputDirName);
        Directory.CreateDirectory(outDir);

        Debug.Log($"[YandexBuild] Building {scenes.Length} scenes -> {outDir}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        });

        var s = report.summary;
        Debug.Log($"[YandexBuild] Result={s.result} Errors={s.totalErrors} Warnings={s.totalWarnings} " +
                  $"Size={s.totalSize} Time={s.totalTime} Output={s.outputPath}");

        if (s.result == BuildResult.Succeeded)
            ReportBuildSize(report);

        if (Application.isBatchMode)
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>
    /// Unity 6: 4 jslib-пакета Kimicu объявляют `const library` в общей области видимости emscripten →
    /// SyntaxError: Identifier 'library' has already been declared. Переименовываем локальный `library`
    /// в каждом .jslib в уникальное имя (LibraryManager.library НЕ трогаем). Идемпотентно, durable
    /// (переживает вайп Library — переприменяется на каждой сборке).
    /// </summary>
    private static void PatchKimicuJslib()
    {
#if UNITY_6000_0_OR_NEWER
        var pkgCache = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Library", "PackageCache");
        if (!Directory.Exists(pkgCache)) return;

        var files = Directory.GetDirectories(pkgCache, "com.blackbox.kimicuyandexgames@*")
            .SelectMany(d => Directory.GetFiles(d, "*.jslib", SearchOption.AllDirectories));

        int patched = 0;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"(?<!LibraryManager\.)\blibrary\b"))
                continue; // уже пропатчен

            var unique = "_kimLib_" + Path.GetFileNameWithoutExtension(file).Replace("-", "_").Replace(".", "_");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!LibraryManager\.)\blibrary\b", unique);
            File.WriteAllText(file, text);
            patched++;
            Debug.Log($"[YandexBuild] jslib пропатчен ({unique}): {Path.GetFileName(file)}");
        }
        Debug.Log($"[YandexBuild] Kimicu jslib patch (Unity 6 library-коллизия): пропатчено {patched}");
#endif
    }

    /// <summary>
    /// Отчёт по несжатому размеру билда (лимит Яндекса — 100 МБ) + топ тяжёлых ассетов.
    /// По умолчанию НИЧЕГО не сжимаем — только измеряем; при превышении лимита советуем YandexOptimize.
    /// </summary>
    private static void ReportBuildSize(BuildReport report)
    {
        // summary.totalSize — несжатый размер билда, заполнен всегда (в т.ч. на кэшированной сборке).
        long total = (long)report.summary.totalSize;
        double totalMb = total / (1024.0 * 1024.0);
        Debug.Log($"[YandexBuild] Несжатый размер билда ~{totalMb:F1} МБ (лимит Яндекса 100 МБ)");

        if (total > YandexUncompressedLimitBytes)
            Debug.LogWarning($"[YandexBuild] ПРЕВЫШЕН ЛИМИТ: ~{totalMb:F1} МБ > 100 МБ — Яндекс отклонит. " +
                             "Запусти 'Yandex/Optimize Assets (size)' и пересобери.");
        else if (total > YandexUncompressedLimitBytes * 8 / 10)
            Debug.LogWarning($"[YandexBuild] Близко к лимиту: ~{totalMb:F1} МБ (>80% от 100 МБ).");

        // Пер-ассетный разбор доступен только на полной (не кэшированной) сборке.
        var bySource = new System.Collections.Generic.Dictionary<string, ulong>();
        foreach (var packed in report.packedAssets)
        foreach (var info in packed.contents)
        {
            var path = string.IsNullOrEmpty(info.sourceAssetPath) ? "(engine)" : info.sourceAssetPath;
            bySource.TryGetValue(path, out var cur);
            bySource[path] = cur + info.packedSize;
        }

        if (bySource.Count == 0)
        {
            Debug.Log("[YandexBuild] Пер-ассетный разбор недоступен (кэшированная сборка). " +
                      "Для топ-ассетов удали папку Library/Bee и пересобери.");
            return;
        }

        Debug.Log("[YandexBuild] Топ-20 тяжёлых ассетов:");
        foreach (var kv in bySource.OrderByDescending(k => k.Value).Take(20))
            Debug.Log($"[YandexBuild]   {kv.Value / (1024.0 * 1024.0):F2} МБ  {kv.Key}");
    }

    private static void ApplyPlayerSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        PlayerSettings.WebGL.template = Template;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        var canvasWidth = GetCanvasSize(CanvasWidthEnv, CanvasWidth);
        var canvasHeight = GetCanvasSize(CanvasHeightEnv, CanvasHeight);
        PlayerSettings.defaultWebScreenWidth = canvasWidth;
        PlayerSettings.defaultWebScreenHeight = canvasHeight;

        AssetDatabase.SaveAssets();
        Debug.Log($"[YandexBuild] PlayerSettings applied: template={Template}, Brotli, dataCaching, " +
                  $"decompressionFallback, runInBackground, canvas {canvasWidth}x{canvasHeight}");
    }

    private static int GetCanvasSize(string envName, int fallback)
    {
        var raw = System.Environment.GetEnvironmentVariable(envName);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    private static void ReplaceEventSystems()
    {
        int replaced = 0;
        var prefabPaths = new HashSet<string>(); // префабы с EventSystem внутри — правим сам ассет
        foreach (var scenePath in EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path))
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool dirty = false;

            foreach (var es in Resources.FindObjectsOfTypeAll<EventSystem>())
            {
                if (es is WebEventSystem) continue;
                var go = es.gameObject;
                if (!go.scene.IsValid()) continue; // пропускаем префаб-ассеты

                // компонент префаб-инстанса удалить из сцены нельзя — правим исходный префаб после обхода сцен
                if (PrefabUtility.IsPartOfPrefabInstance(es))
                {
                    var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(es);
                    if (!string.IsNullOrEmpty(assetPath)) prefabPaths.Add(assetPath);
                    continue;
                }

                ReplaceOnGameObject(go);
                dirty = true;
                replaced++;
            }

            if (dirty) EditorSceneManager.SaveScene(scene);
        }

        foreach (var assetPath in prefabPaths)
        {
            var root = PrefabUtility.LoadPrefabContents(assetPath);
            bool dirty = false;
            foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
            {
                if (es is WebEventSystem) continue;
                ReplaceOnGameObject(es.gameObject);
                dirty = true;
                replaced++;
            }
            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                Debug.Log($"[YandexBuild] EventSystem заменён внутри префаба: {assetPath}");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[YandexBuild] EventSystem -> WebEventSystem replaced: {replaced}");
    }

    private static void ReplaceOnGameObject(GameObject go)
    {
        // BaseInputModule держит EventSystem через RequireComponent — снять модули ДО удаления EventSystem,
        // иначе DestroyImmediate молча фейлится ("Can't remove EventSystem ... depends on it")
        foreach (var module in go.GetComponents<BaseInputModule>()) Object.DestroyImmediate(module);
        foreach (var es in go.GetComponents<EventSystem>())
            if (!(es is WebEventSystem)) Object.DestroyImmediate(es);
        if (!go.TryGetComponent(out WebEventSystem _)) go.AddComponent<WebEventSystem>();
        if (!go.TryGetComponent(out StandaloneInputModule _)) go.AddComponent<StandaloneInputModule>();
        EditorUtility.SetDirty(go);
    }
}
#endif
