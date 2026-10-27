using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoSingleton<SceneLoader>
{
    [SerializeField] private LoadingUI loadingUI;
    [SerializeField] private string firstScene = "MainMenu";
    [SerializeField] private float bootDuration = 3f;
    [SerializeField] private float coverDuration = 0.6f;
    [SerializeField] private float coverFadeIn = 0.2f;
    [SerializeField] private TextAsset levelsJson;

    private bool covering;
    private bool busy;

    public static bool IsCovering => Instance != null && Instance.covering;

    public static void Load(string scene, float? duration = null)
    {
        if (Instance == null) SceneManager.LoadScene(scene);
        else Instance.BeginLoad(scene, duration);
    }

    public static void Cover(Action work, float? duration = null)
    {
        if (Instance == null || Instance.busy) work?.Invoke();
        else Instance.StartCoroutine(Instance.RunCover(work, duration ?? Instance.coverDuration));
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        if (loadingUI == null) loadingUI = GetComponentInChildren<LoadingUI>(true);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (Instance != this) return;
        if (levelsJson != null) LevelRepository.Get(levelsJson);
        BeginLoad(ResolveFirstScene(), bootDuration);
    }

    private string ResolveFirstScene()
    {
#if UNITY_EDITOR
        string from = UnityEditor.SessionState.GetString("Shikacat.PlayFromScene", string.Empty);
        UnityEditor.SessionState.SetString("Shikacat.PlayFromScene", string.Empty);
        if (!string.IsNullOrEmpty(from) && from != gameObject.scene.name) return from;
#endif
        return firstScene;
    }

    private void BeginLoad(string scene, float? duration)
    {
        if (busy) return;
        StartCoroutine(RunLoad(scene, duration ?? loadingUI.LoadDuration));
    }

    private IEnumerator RunLoad(string scene, float seconds)
    {
        busy = true;
        covering = true;
        loadingUI.Begin();

        AsyncOperation op = SceneManager.LoadSceneAsync(scene);
        op.allowSceneActivation = false;

        float t = 0f;
        while (t < seconds || op.progress < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Min(t / seconds, op.progress / 0.9f);
            loadingUI.SetProgress(Mathf.Min(p, 0.999f));
            yield return null;
        }

        loadingUI.SetProgress(1f);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        yield return null;
        yield return null;

        covering = false;
        loadingUI.Hide(() => busy = false);
    }

    private IEnumerator RunCover(Action work, float seconds)
    {
        busy = true;
        covering = true;
        loadingUI.Begin(coverFadeIn);
        yield return new WaitForSecondsRealtime(coverFadeIn);

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            loadingUI.SetProgress(Mathf.Min(t / seconds, 0.999f));
            yield return null;
        }

        work?.Invoke();
        loadingUI.SetProgress(1f);
        yield return null;
        yield return null;

        covering = false;
        loadingUI.Hide(() => busy = false);
    }
}
