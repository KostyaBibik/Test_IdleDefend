#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class YandexPortSceneSetup
{
    private const string BootScenePath = "Assets/VYandexTools/Scene/Boot.unity";
    private const string SceneIndexShiftMarker = "ProjectSettings/YandexPortSceneSetup.scene-index-shifted";
    private const string LegacyNoAdsPrefab = "Assets/VYandexTools/Advertisements/NoAds/NoAdsButton/Prefabs/NoAdButtonLegacyText.prefab";
    private const string TmpNoAdsPrefab = "Assets/VYandexTools/Advertisements/NoAds/NoAdsButton/Prefabs/NoAdButton .prefab";
    private const string FirstClickCheckerPrefab = "Assets/VYandexTools/Analytics/FirstClickChecker.prefab";
    private static readonly string[] OldSdkTokens =
    {
        "AppMetrica", "AppsFlyer", "Facebook", "Huawei", "GoogleMobileAds",
        "IronSource", "Adjust", "Appodeal", "Applovin", "MaxSdk", "Mintegral", "UnityAds",
        "YandexGame", "GamePush", "Firebase", "GooglePlayGames"
    };

    [MenuItem("Yandex/Prepare Port Scenes")]
    public static void PreparePortScenes()
    {
        ShiftSceneIndexReferencesOnce();
        EnsureBootScene();
        RemoveOldSdkObjectsFromBuildScenes();
        AddRequiredPrefabsToFirstGameplayScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[YandexPortSceneSetup] Done");
    }

    private static void ShiftSceneIndexReferencesOnce()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        var markerPath = Path.Combine(projectRoot, SceneIndexShiftMarker);
        if (File.Exists(markerPath))
        {
            Debug.Log("[YandexPortSceneSetup] Scene build-index references already shifted");
            return;
        }

        int changedFiles = 0;
        foreach (var file in Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories))
        {
            var relative = ToUnityPath(file, projectRoot);
            if (ShouldSkipSceneIndexFile(relative)) continue;

            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".cs")
            {
                if (ShiftCodeSceneIndexes(file)) changedFiles++;
            }
            else if (ext == ".unity" || ext == ".prefab" || ext == ".asset")
            {
                if (ShiftSerializedSceneIndexes(file)) changedFiles++;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        File.WriteAllText(markerPath, $"Shifted scene build-index references by +1 on {DateTime.UtcNow:O}\n");
        AssetDatabase.Refresh();
        Debug.Log($"[YandexPortSceneSetup] Scene build-index references shifted by +1 in {changedFiles} files");
    }

    private static bool ShouldSkipSceneIndexFile(string unityPath)
    {
        return unityPath.StartsWith("Assets/VYandexTools/", StringComparison.OrdinalIgnoreCase)
               || unityPath.StartsWith("Assets/Editor/Yandex", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShiftCodeSceneIndexes(string file)
    {
        var text = File.ReadAllText(file);
        var updated = text;

        updated = System.Text.RegularExpressions.Regex.Replace(
            updated,
            @"(?<call>\b(?:SceneManager\.)?(?:LoadScene|LoadSceneAsync|GetSceneByBuildIndex|GetScenePathByBuildIndex)\s*\(\s*)(?<num>\d+)",
            m => m.Groups["call"].Value + (int.Parse(m.Groups["num"].Value) + 1));

        updated = System.Text.RegularExpressions.Regex.Replace(
            updated,
            @"(?<name>\b(?:sceneBuildIndex|buildIndex|sceneIndex|sceneIndexToLoad)\s*:\s*)(?<num>\d+)",
            m => m.Groups["name"].Value + (int.Parse(m.Groups["num"].Value) + 1),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (updated == text) return false;
        File.WriteAllText(file, updated);
        return true;
    }

    private static bool ShiftSerializedSceneIndexes(string file)
    {
        var text = File.ReadAllText(file);
        var updated = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(?im)^(?<prefix>\s*(?:m_)?[A-Za-z0-9_]*(?:scene|build)[A-Za-z0-9_]*(?:index|id|idx)?\s*:\s*)(?<num>\d+)(?<suffix>\s*)$",
            m => m.Groups["prefix"].Value + (int.Parse(m.Groups["num"].Value) + 1) + m.Groups["suffix"].Value);

        if (updated == text) return false;
        File.WriteAllText(file, updated);
        return true;
    }

    private static string ToUnityPath(string path, string projectRoot)
    {
        return path.Substring(projectRoot.Length + 1).Replace("\\", "/");
    }

    private static void EnsureBootScene()
    {
        if (!File.Exists(BootScenePath))
        {
            Debug.LogError($"[YandexPortSceneSetup] Boot scene not found in VYandexTool package: {BootScenePath}");
            return;
        }

        var scenes = EditorBuildSettings.scenes
            .Where(s => !string.Equals(s.path, BootScenePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(BootScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[YandexPortSceneSetup] Boot scene is build index 0");
    }

    private static void RemoveOldSdkObjectsFromBuildScenes()
    {
        foreach (var scenePath in EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path))
        {
            if (string.Equals(scenePath, BootScenePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var toRemove = new HashSet<GameObject>();

            foreach (var root in scene.GetRootGameObjects())
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                var go = tr.gameObject;
                if (MatchesOldSdk(go.name))
                {
                    toRemove.Add(GetPrefabInstanceRoot(go));
                    continue;
                }

                foreach (var component in go.GetComponents<Component>())
                {
                    if (component == null) continue;
                    var type = component.GetType();
                    if (MatchesOldSdk(type.Name) || MatchesOldSdk(type.FullName ?? ""))
                    {
                        toRemove.Add(GetPrefabInstanceRoot(go));
                        break;
                    }
                }
            }

            foreach (var go in toRemove.Where(g => g != null).OrderByDescending(g => GetDepth(g.transform)))
                UnityEngine.Object.DestroyImmediate(go);

            if (toRemove.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[YandexPortSceneSetup] Removed old SDK scene objects in {scenePath}: {toRemove.Count}");
            }
        }
    }

    private static bool MatchesOldSdk(string value)
    {
        return OldSdkTokens.Any(t => value.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void AddRequiredPrefabsToFirstGameplayScene()
    {
        var gameplayScene = GetFirstGameplayScenePath();
        if (string.IsNullOrEmpty(gameplayScene))
        {
            Debug.LogWarning("[YandexPortSceneSetup] No gameplay scene found for package prefabs");
            return;
        }

        var scene = EditorSceneManager.OpenScene(gameplayScene, OpenSceneMode.Single);
        var changed = false;
        changed |= AddPrefabIfMissing(scene, FirstClickCheckerPrefab, "FirstClickChecker");

        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[YandexPortSceneSetup] Added package prefabs to {gameplayScene}");
    }

    private static bool AddPrefabIfMissing(Scene scene, string prefabPath, string objectName)
    {
        if (FindObjectInScene(scene, objectName) != null)
            return false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[YandexPortSceneSetup] Required prefab not found: {prefabPath}");
            return false;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = objectName;
        EditorUtility.SetDirty(instance);
        return true;
    }

    private static GameObject GetPrefabInstanceRoot(GameObject go)
    {
        var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        return root != null ? root : go;
    }

    private static int GetDepth(Transform tr)
    {
        int depth = 0;
        while (tr.parent != null)
        {
            depth++;
            tr = tr.parent;
        }
        return depth;
    }

    private static void AddNoAdsButtonToFirstGameplayScene()
    {
        if (HasPaymentsExplicitlyDisabled())
        {
            Debug.Log("[YandexPortSceneSetup] hasPayments=false, NoAds button skipped");
            return;
        }

        var gameplayScene = GetFirstGameplayScenePath();
        if (string.IsNullOrEmpty(gameplayScene))
        {
            Debug.LogWarning("[YandexPortSceneSetup] No gameplay scene found for NoAds button");
            return;
        }

        var scene = EditorSceneManager.OpenScene(gameplayScene, OpenSceneMode.Single);
        if (FindObjectInScene(scene, "NoAd") != null || FindObjectInScene(scene, "NoAds") != null)
        {
            Debug.Log("[YandexPortSceneSetup] NoAds button already exists, skipped");
            return;
        }

        var usesTmp = SceneUsesTmp(scene);
        var prefabPath = usesTmp && File.Exists(TmpNoAdsPrefab) ? TmpNoAdsPrefab : LegacyNoAdsPrefab;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[YandexPortSceneSetup] NoAds prefab not found: {prefabPath}");
            return;
        }

        var canvas = FindOrCreateCanvas(scene);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "NoAdsButton";
        instance.transform.SetParent(canvas.transform, false);

        PositionNoAds(instance.GetComponent<RectTransform>(), canvas);
        CopyProjectFont(instance, scene, usesTmp);
        instance.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[YandexPortSceneSetup] Added NoAds button to {gameplayScene}: {prefabPath}");
    }

    private static string GetFirstGameplayScenePath()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.Equals(s.path, BootScenePath, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.path)
            .FirstOrDefault();
    }

    private static bool HasPaymentsExplicitlyDisabled()
    {
        var configPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "game.config.json");
        if (!File.Exists(configPath)) return false;
        var text = File.ReadAllText(configPath);
        return System.Text.RegularExpressions.Regex.IsMatch(text, @"""hasPayments""\s*:\s*false", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool SceneUsesTmp(Scene scene)
    {
        var tmpType = FindType("TMPro.TMP_Text");
        if (tmpType == null) return false;
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Component>(true))
            .Any(c => c != null && tmpType.IsAssignableFrom(c.GetType()));
    }

    private static Canvas FindOrCreateCanvas(Scene scene)
    {
        var canvas = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay) ??
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();

        if (canvas != null) return canvas;

        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(go, scene);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return canvas;
    }

    private static void PositionNoAds(RectTransform rect, Canvas canvas)
    {
        if (rect == null) return;

        bool topRightOccupied = canvas.GetComponentsInChildren<RectTransform>(true)
            .Where(r => r != rect)
            .Any(IsTopRight);

        if (topRightOccupied)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
        }
    }

    private static bool IsTopRight(RectTransform rect)
    {
        var nearTopRight = rect.anchorMax.x > 0.75f && rect.anchorMax.y > 0.75f;
        var nearCorner = rect.anchoredPosition.x > -260f && rect.anchoredPosition.y > -220f;
        return nearTopRight && nearCorner && rect.gameObject.activeInHierarchy;
    }

    private static void CopyProjectFont(GameObject noAdsButton, Scene scene, bool useTmp)
    {
        if (useTmp)
        {
            var tmpType = FindType("TMPro.TMP_Text");
            if (tmpType == null) return;

            var source = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .FirstOrDefault(c => c != null && tmpType.IsAssignableFrom(c.GetType()) && !c.transform.IsChildOf(noAdsButton.transform));
            if (source == null) return;

            var font = tmpType.GetProperty("font")?.GetValue(source);
            foreach (var target in noAdsButton.GetComponentsInChildren<Component>(true).Where(c => c != null && tmpType.IsAssignableFrom(c.GetType())))
            {
                tmpType.GetProperty("font")?.SetValue(target, font);
                EditorUtility.SetDirty(target);
            }
            return;
        }

        var sourceText = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Text>(true))
            .FirstOrDefault(t => !t.transform.IsChildOf(noAdsButton.transform) && t.font != null);
        if (sourceText == null) return;

        foreach (var target in noAdsButton.GetComponentsInChildren<Text>(true))
        {
            target.font = sourceText.font;
            EditorUtility.SetDirty(target);
        }
    }

    private static GameObject FindObjectInScene(Scene scene, string namePart)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .FirstOrDefault(go => go.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullName))
            .FirstOrDefault(t => t != null);
    }
}
#endif
