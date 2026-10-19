using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private TextAsset levelsJson;
    
    [Header("Input")]
    [SerializeField] private RoomBuildController buildController;
    
    [Header("HUD")]
    [SerializeField] private LevelHud hud;
    
    [Header("Win FX")]
    [SerializeField] private GameObject shotFx;
    [SerializeField] private GameObject dropFx;
    [SerializeField] private float roomAnimationTimeout = 3f;
    [SerializeField] private float fxTimeout = 5f;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;

    [Header("Game Over Animation")]
    [SerializeField] private WinAnimController winAnim;    // Spine thắng (WellDone, mèo, coin), nằm trong GameOverPanel
    [SerializeField] private LoseAnimController loseAnim;  // Spine thua (LevelFail, TimeOut), nằm trong GameOverPanel

    public const string EditorStartLevelKey = "Shikacat.StartLevelIndex";
    
    public event Action LevelLoaded;
    
    private LevelData[] levels;
    private LevelData currentLevel;
    private Coroutine gameOverRoutine;
    private int currentLevelIndex;
    private float timeRemaining;
    private int displayedSeconds = -1;
    private int movesRemaining;
    private bool isGameOver;
    private bool isLoading;
    private bool timerRunning;
    private Room winRoom;
    private bool winRoomAnimated;
    private bool useEditorStart;
    private bool lostByTimeOut;

    public bool IsGameOver => isGameOver;
    
    private bool HasNextLevel => levels != null && currentLevelIndex + 1 < levels.Length;
    
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
        GridManager.Instance.RoomRemoved += HandleRoomRemoved;
        if (buildController != null)
        {
            buildController.CellPressed += HandleCellPressed;
            buildController.RoomAnimationFinished += HandleRoomAnimationFinished;
        }
    }

    private void OnDisable()
    {
        if (buildController != null)
        {
            buildController.CellPressed -= HandleCellPressed;
            buildController.RoomAnimationFinished -= HandleRoomAnimationFinished;
        }
        if (GridManager.Instance == null) return;
        GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
        GridManager.Instance.RoomRemoved -= HandleRoomRemoved;
    }

    protected override void Awake()
    {
        base.Awake();
        DOTween.SetTweensCapacity(200, 50);
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        if (buildController == null)
            Debug.LogWarning("GameManager: RoomBuildController NOT FOUND.");

        levels = LevelLoader.Load(levelsJson);

        int editorIndex = ConsumeEditorStartLevel();
        useEditorStart = editorIndex >= 0;
        LoadLevel(useEditorStart ? editorIndex : PlayerProgress.LoadLevelIndex());
    }

    private void Update()
    {
        if (isGameOver || currentLevel == null || !currentLevel.HasTimeLimit || !timerRunning) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            UpdateTimeHud();
            HandleLose(true);
            return;
        }

        UpdateTimeHud();
    }
    
    private void HandleRoomAnimationFinished(Room room)
    {
        if (room == winRoom) winRoomAnimated = true;
    }

    private static int ConsumeEditorStartLevel()
    {
#if UNITY_EDITOR
        int index = UnityEditor.EditorPrefs.GetInt(EditorStartLevelKey, -1);
        if (index >= 0)
        {
            UnityEditor.EditorPrefs.DeleteKey(EditorStartLevelKey);
            return index;
        }
#endif
        return -1;
    }

    public void LoadLevelAt(int index) => LoadLevel(index);
    
    private void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: không có level nào để chơi (kiểm tra levelsJson).");
            return;
        }
        
        currentLevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        currentLevel = levels[currentLevelIndex];

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        isGameOver = false;
        timerRunning = false;
        timeRemaining = currentLevel.timeLimit;
        movesRemaining = currentLevel.moveLimit;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (loseAnim != null) loseAnim.gameObject.SetActive(false);
        if (shotFx != null) shotFx.SetActive(false);
        if (dropFx != null) dropFx.SetActive(false);
        winRoom = null;
        winRoomAnimated = false;
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        isLoading = true;
        GridManager.Instance.BuildGrid(currentLevel);
        isLoading = false; 
        
        if (hud != null) hud.Bind(currentLevelIndex + 1, currentLevel.HasMoveLimit);
        displayedSeconds = -1;
        UpdateLimitText();
        
        LevelLoaded?.Invoke();
    }
    
    private void HandleCellPressed()
    {
        if (timerRunning || isGameOver || isLoading || currentLevel == null) return;
        timerRunning = true;
    }
    
    private void UpdateTimeHud()
    {
        if (hud == null) return;

        hud.SetProgress(timeRemaining / currentLevel.timeLimit);
        if (!currentLevel.HasMoveLimit) UpdateLimitText();
    }

    private void UpdateMovesHud()
    {
        if (hud == null) return;

        if (!currentLevel.HasTimeLimit)
            hud.SetProgress(movesRemaining / (float)currentLevel.moveLimit);
        UpdateLimitText();
    }
    
    private void UpdateLimitText()
    {
        if (hud == null || currentLevel == null) return;

        if (currentLevel.HasMoveLimit)
        {
            hud.SetLimitText(movesRemaining.ToString());
            return;
        }

        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        if (totalSeconds == displayedSeconds) return;
        displayedSeconds = totalSeconds;
        hud.SetLimitText($"{totalSeconds / 60:00}:{totalSeconds % 60:00}");
    }

    private void HandleRoomCommitted(Room room)
    {
        if (isLoading || isGameOver) return;
        
        ConsumeMove();
        
        if (GridManager.Instance.IsGridFull() && GridManager.Instance.AreAllRoomsCorrect())
        {
            HandleWin(room);
            return;
        }

        CheckMoveLimitLose();
    }
    
    private void HandleRoomRemoved(Room room)
    {
        if (isLoading || isGameOver || GridManager.Instance.IsReplacingRooms) return;

        ConsumeMove();
        CheckMoveLimitLose();
    }

    private void ConsumeMove()
    {
        if (!currentLevel.HasMoveLimit) return;

        movesRemaining--;
        UpdateMovesHud();
    }

    private void CheckMoveLimitLose()
    {
        if (currentLevel.HasMoveLimit && movesRemaining <= 0)
            HandleLose();
    }

    private void HandleWin(Room lastRoom)
    {
        if (isGameOver) return;
        isGameOver = true;

        SaveProgress();

        winRoom = lastRoom;
        winRoomAnimated = false;

        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = StartCoroutine(WinSequenceRoutine());
    }
    
    private void SaveProgress()
    {
        if (useEditorStart) return;

        int next = HasNextLevel ? currentLevelIndex + 1 : currentLevelIndex;
        if (next > PlayerProgress.LoadLevelIndex())
            PlayerProgress.SaveLevelIndex(next);
    }

    private void HandleLose(bool byTimeOut = false)
    {
        if (isGameOver) return;
        isGameOver = true;

        lostByTimeOut = byTimeOut;
        ShowGameOverPanel(false);
    }

    private IEnumerator WinSequenceRoutine()
    {
        float waited = 0f;
        while (!winRoomAnimated && waited < roomAnimationTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (shotFx != null)
        {
            shotFx.SetActive(true);
            yield return null;
            yield return WaitForParticles(shotFx, fxTimeout);
        }

        gameOverRoutine = null;
        ShowGameOverPanel(true);
    }
    
    private void ShowGameOverPanel(bool win)
    {
        bool hasNext = HasNextLevel;

        if (dropFx != null) dropFx.SetActive(win);
        
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (loseAnim != null) loseAnim.gameObject.SetActive(false);
        
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (win) { if (winAnim != null) winAnim.Play(); }
        else if (loseAnim != null) loseAnim.Play(lostByTimeOut);

        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(win && hasNext);
        if (restartButton != null) restartButton.gameObject.SetActive(!win || !hasNext);
    }
    
    private static IEnumerator WaitForParticles(GameObject fx, float timeout)
    {
        ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        float elapsed = 0f;

        while (elapsed < timeout && AnyAlive(systems))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private static bool AnyAlive(ParticleSystem[] systems)
    {
        foreach (ParticleSystem s in systems)
            if (s != null && s.IsAlive(false)) return true;
        return false;
    }
    
    public void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }
    
    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }
}
