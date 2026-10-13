using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private TextAsset levelsJson;
    
    [Header("HUD")]
    [SerializeField] private LevelHud hud;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverImage;
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private float winPanelDelay = 1.2f;
    [SerializeField] private float losePanelDelay = 0.8f;
    
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

    public bool IsGameOver => isGameOver;
    
    private bool HasNextLevel => levels != null && currentLevelIndex + 1 < levels.Length;
    
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
        GridManager.Instance.RoomRemoved += HandleRoomRemoved;
    }

    private void OnDisable()
    {
        if (GridManager.Instance == null) return;
        GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
        GridManager.Instance.RoomRemoved -= HandleRoomRemoved;
    }

    private void Start()
    {
        levels = LevelLoader.Load(levelsJson);
        LoadLevel(ConsumeEditorStartLevel());
    }

    private void Update()
    {
        if (isGameOver || currentLevel == null || !currentLevel.HasTimeLimit) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            UpdateTimeHud();
            HandleLose();
            return;
        }

        UpdateTimeHud();
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
        return 0;
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
        timeRemaining = currentLevel.timeLimit;
        movesRemaining = currentLevel.moveLimit;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        isLoading = true;
        GridManager.Instance.BuildGrid(currentLevel);
        isLoading = false; 
        
        if (hud != null) hud.Bind(currentLevelIndex + 1);
        displayedSeconds = -1;
        UpdateLimitText();
        
        LevelLoaded?.Invoke();
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
            HandleWin();
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

    private void HandleWin()
    {
        if (isGameOver) return;
        isGameOver = true;

        ShowGameOverAfterDelay(true, winPanelDelay); 
    }

    private void HandleLose()
    {
        if (isGameOver) return;
        isGameOver = true;

        ShowGameOverAfterDelay(false, losePanelDelay);
    }
    
    private void ShowGameOverAfterDelay(bool win, float delay)
    {
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = StartCoroutine(ShowGameOverRoutine(win, delay));
    }

    private IEnumerator ShowGameOverRoutine(bool win, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        gameOverRoutine = null;

        bool hasNext = HasNextLevel;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverImage != null) gameOverImage.sprite = win ? winSprite : loseSprite;
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(win && hasNext);
        if (restartButton != null) restartButton.gameObject.SetActive(!win || !hasNext);
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
