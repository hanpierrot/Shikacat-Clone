using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private TextAsset levelsJson;
    
    [Header("HUD")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text moveLimitText;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverImage;
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    
    public const string EditorStartLevelKey = "Shikacat.StartLevelIndex";
    
    private LevelData[] levels;
    private LevelData currentLevel;
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
            UpdateTimerText();
            HandleLose();
            return;
        }
        
        UpdateTimerText();
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

        isGameOver = false;
        timeRemaining = currentLevel.timeLimit;
        movesRemaining = currentLevel.moveLimit;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        isLoading = true;
        GridManager.Instance.BuildGrid(currentLevel);
        isLoading = false; 
        
        if (timerText != null) timerText.gameObject.SetActive(currentLevel.HasTimeLimit);
        if (moveLimitText != null) moveLimitText.gameObject.SetActive(currentLevel.HasMoveLimit);
        
        displayedSeconds = -1;
        UpdateTimerText();
        UpdateMoveText();
    }
    
    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        if (totalSeconds == displayedSeconds) return;
        displayedSeconds = totalSeconds;
        timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
    
    private void UpdateMoveText()
    {
        if (moveLimitText == null) return;
        moveLimitText.text = movesRemaining.ToString();
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
        UpdateMoveText();
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

        bool hasNext = HasNextLevel;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverImage != null) gameOverImage.sprite = winSprite;
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(hasNext);
        if (restartButton != null) restartButton.gameObject.SetActive(!hasNext); 
    }

    private void HandleLose()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverImage != null) gameOverImage.sprite = loseSprite;
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(true);
    }
    
    public void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }
    
    public void RestartLevel()
    {
        if (!HasNextLevel) return;
        LoadLevel(currentLevelIndex);
    }
}
