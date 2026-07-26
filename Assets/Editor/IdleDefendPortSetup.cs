#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UI.Views;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VYandexTools.Localization.Scripts;

/// <summary>
/// Per-game доводка сцен Test_IdleDefend под Яндекс.Игры (идемпотентно).
/// Запуск: Unity.exe -batchmode -quit -projectPath ... -executeMethod IdleDefendPortSetup.Prepare
/// или меню Yandex/IdleDefend/Prepare Game Content.
/// </summary>
public static class IdleDefendPortSetup
{
    private const string TableName = "LocalizationTable";
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string AdTimerPrefab = "Assets/VYandexTools/Advertisements/AdTimer/Prefabs/AdTimer.prefab";
    private const string BestTimeObjectName = "BestTimeLabel";
    private const string AdTimerObjectName = "AdTimer";

    /// <summary>Текст в сцене -> ключ локализации. Сопоставление по исходной английской строке.</summary>
    private static readonly Dictionary<string, string> TextToKey = new Dictionary<string, string>
    {
        { "Play", "menu_play" },
        { "You lose!", "lose_title" },
        { "Restart", "lose_restart" },
        { "Exit", "lose_menu" },
        { "Up distance", "upgrade_range" },
        { "Up damage", "upgrade_damage" },
        { "Up Attack speed", "upgrade_attack_speed" },
        { "Buy Health", "upgrade_health" },
        { "Claim", "upgrade_buy" },
    };

    /// <summary>ключ -> (ru, en). 'em' (Empty) заполняем английским.</summary>
    private static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
    {
        { "loading", new[] { "Загрузка", "Loading" } },
        { "menu_play", new[] { "Играть", "Play" } },
        { "upgrade_range", new[] { "Дальность", "Range" } },
        { "upgrade_damage", new[] { "Урон", "Damage" } },
        { "upgrade_attack_speed", new[] { "Скор. атаки", "Atk. speed" } },
        { "upgrade_health", new[] { "Здоровье", "Health" } },
        { "upgrade_buy", new[] { "Купить", "Buy" } },
        { "lose_title", new[] { "Вы проиграли!", "You lose!" } },
        { "lose_restart", new[] { "Заново", "Restart" } },
        { "lose_menu", new[] { "В меню", "Menu" } },
        { "best_time", new[] { "Рекорд", "Record" } },
        { "run_time", new[] { "Результат", "Result" } },
    };

    [MenuItem("Yandex/IdleDefend/Prepare Game Content")]
    public static void Prepare()
    {
        EnsureLocalization();
        PrepareScene(MenuScenePath, isGameScene: false);
        PrepareScene(GameScenePath, isGameScene: true);
        AssetDatabase.SaveAssets();
        BuildAddressables();
        Debug.Log("[IdleDefendPortSetup] Done");
    }

    /// <summary>
    /// Unity Localization хранит локали и таблицы в Addressables. Без собранного контента
    /// в плеере не будет ни локалей, ни строк — гоняем сборку перед билдом плеера.
    /// </summary>
    [MenuItem("Yandex/IdleDefend/Build Addressables")]
    public static void BuildAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[IdleDefendPortSetup] AddressableAssetSettings не найдены");
            return;
        }

        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out var result);

        if (!string.IsNullOrEmpty(result.Error))
            Debug.LogError($"[IdleDefendPortSetup] Addressables build error: {result.Error}");
        else
            Debug.Log($"[IdleDefendPortSetup] Addressables собраны: {result.OutputPath} ({result.Duration:F1}s)");
    }

    // ------------------------------------------------------------------ localization

    private static void EnsureLocalization()
    {
        VYandexTools.Localization.Editor.LocalizationSetupCreator.CreateLocalizationSetup();

        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            Debug.LogError($"[IdleDefendPortSetup] Не найдена таблица {TableName}");
            return;
        }

        foreach (var table in collection.StringTables)
        {
            var code = table.LocaleIdentifier.Code;
            var index = code == "ru" ? 0 : 1; // ru | en/em

            foreach (var pair in Strings)
            {
                var entry = table.GetEntry(pair.Key);
                if (entry == null)
                    table.AddEntry(pair.Key, pair.Value[index]);
                else if (string.IsNullOrEmpty(entry.Value))
                    entry.Value = pair.Value[index];
            }

            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        VYandexTools.Localization.Editor.LocalizationEnumGenerator.Generate();
        Debug.Log($"[IdleDefendPortSetup] Локализация: {Strings.Count} ключей x {collection.StringTables.Count} таблиц");
    }

    // ------------------------------------------------------------------ scenes

    private static void PrepareScene(string scenePath, bool isGameScene)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        LogTextInventory(scene);
        var localized = ApplyLocalizedTexts(scene);
        var canvas = FindOverlayCanvas(scene);

        if (canvas == null)
        {
            Debug.LogWarning($"[IdleDefendPortSetup] {scenePath}: Canvas не найден");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // В GameScene подпись кладём на LosePanel сразу под заголовком «You lose!»,
        // в Menu — прижимаем к низу канваса, чтобы гарантированно не наехать на кнопку Play.
        bool bestTimeAdded;
        if (isGameScene)
        {
            var losePanel = FindByName(scene, "LosePanel");
            var title = losePanel == null
                ? null
                : losePanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t.text != null && t.text.Trim() == "You lose!");

            var anchor = title != null ? (RectTransform) title.transform : null;
            var pos = anchor != null
                ? anchor.anchoredPosition + new Vector2(0f, -220f)
                : new Vector2(0f, -160f);

            bestTimeAdded = EnsureBestTimeLabel(canvas, losePanel, showLastRun: true, anchor, pos);
        }
        else
        {
            bestTimeAdded = EnsureBestTimeLabel(canvas, canvas.transform, showLastRun: false, null,
                new Vector2(0f, 160f), bottomAnchored: true);
        }

        var adTimerAdded = isGameScene && EnsureAdTimer(scene, canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[IdleDefendPortSetup] {scenePath}: localized={localized}, bestTime={bestTimeAdded}, adTimer={adTimerAdded}");
    }

    private static void LogTextInventory(Scene scene)
    {
        foreach (var text in AllComponents<TMP_Text>(scene))
            Debug.Log($"[IdleDefendPortSetup] TMP '{text.text}' @ {Path(text.transform)}");
    }

    private static int ApplyLocalizedTexts(Scene scene)
    {
        var count = 0;

        foreach (var text in AllComponents<TMP_Text>(scene))
        {
            var raw = text.text?.Trim();
            if (string.IsNullOrEmpty(raw) || !TextToKey.TryGetValue(raw, out var key))
                continue;

            var go = text.gameObject;
            var localized = go.GetComponent<LocalizedTextTMP>();
            if (localized == null)
                localized = go.AddComponent<LocalizedTextTMP>();

            SetPrivateField(localized, "textComponent", text);
            SetPrivateField(localized, "localizedString", new LocalizedString(TableName, key));
            EditorUtility.SetDirty(localized);
            count++;
        }

        return count;
    }

    private static bool EnsureBestTimeLabel(Canvas canvas, Transform parent, bool showLastRun,
        RectTransform anchorSample, Vector2 position, bool bottomAnchored = false)
    {
        if (parent == null)
            parent = canvas.transform;

        if (FindChildByName(parent, BestTimeObjectName) != null)
            return false;

        var sample = canvas.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null);

        var go = new GameObject(BestTimeObjectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform) go.transform;
        if (anchorSample != null)
        {
            rect.anchorMin = anchorSample.anchorMin;
            rect.anchorMax = anchorSample.anchorMax;
            rect.pivot = anchorSample.pivot;
        }
        else if (bottomAnchored)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        }

        rect.sizeDelta = new Vector2(760f, 200f);
        rect.anchoredPosition = position;

        var label = go.AddComponent<TextMeshProUGUI>();
        if (sample != null)
        {
            label.font = sample.font;
            label.fontSharedMaterial = sample.fontSharedMaterial;
            label.color = sample.color;
        }

        label.fontSize = 48f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.text = "Record: 00:00";

        var view = go.AddComponent<BestTimeLabel>();
        SetPrivateField(view, "label", label);
        SetPrivateField(view, "showLastRun", showLastRun);

        EditorUtility.SetDirty(go);
        return true;
    }

    private static bool EnsureAdTimer(Scene scene, Canvas canvas)
    {
        if (FindByName(scene, AdTimerObjectName) != null)
            return false;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdTimerPrefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[IdleDefendPortSetup] Не найден префаб {AdTimerPrefab}");
            return false;
        }

        var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = AdTimerObjectName;
        instance.transform.SetParent(canvas.transform, false);

        var rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        instance.transform.SetAsLastSibling();
        EditorUtility.SetDirty(instance);
        return true;
    }

    // ------------------------------------------------------------------ helpers

    private static Canvas FindOverlayCanvas(Scene scene)
    {
        var canvases = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .ToList();

        return canvases.FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay)
               ?? canvases.FirstOrDefault();
    }

    private static IEnumerable<T> AllComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    }

    private static Transform FindByName(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name);
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }

    private static string Path(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (field == null)
            throw new InvalidOperationException($"Поле {fieldName} не найдено в {target.GetType().Name}");

        field.SetValue(target, value);
    }
}
#endif
