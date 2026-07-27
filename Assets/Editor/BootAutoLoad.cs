#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BootAutoLoad
{
    private const string BootScenePath = "Assets/VYandexTools/Scene/Boot.unity";

    static BootAutoLoad()
    {
        var bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
        if (bootScene == null)
            return;

        if (EditorSceneManager.playModeStartScene != bootScene)
            EditorSceneManager.playModeStartScene = bootScene;
    }
}
#endif
