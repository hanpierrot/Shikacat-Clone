using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BootStart
{
    public const string FromSceneKey = "Shikacat.PlayFromScene";

    private const string EnabledKey = "Shikacat.StartFromBoot";
    private const string MenuPath = "Shikacat/Start From Boot";
    private const string BootPath = "Assets/Design/Scenes/Boot.unity";

    static BootStart()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, true);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle() => Enabled = !Enabled;

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            SessionState.SetString(FromSceneKey, string.Empty);

            var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath);
            string active = EditorSceneManager.GetActiveScene().path;
            if (!Enabled || boot == null || active == BootPath || string.IsNullOrEmpty(active))
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SessionState.SetString(FromSceneKey, Path.GetFileNameWithoutExtension(active));
            EditorSceneManager.playModeStartScene = boot;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
