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
    [SerializeField] private float roomAnimationTimeout = 3f;
    [SerializeField] private float panelDelayAfterShot = 0.3f;   // Bật GameOverPanel sau khi bật ShotFX khoảng này
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;

    [Header("Game Over Animation")]
    [SerializeField] private WinAnimController winAnim;
    [SerializeField] private LoseAnimController loseAnim;
    
    [Header("Reward")]
    [SerializeField] private CollectButton collectButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private int[] starRewards = { 5, 10, 20 };   // Thưởng coin theo 1, 2, 3 sao
    [SerializeField] private int minWinStars = 1;                 // Thắng luôn được ít nhất ngần này sao
    [SerializeField] private CoinHud coinHud;

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
    private int winStars;
    private int winReward;
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
        if (collectButton != null) collectButton.Collected += HandleRewardCollected;
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
        if (collectButton != null) collectButton.Collected -= HandleRewardCollected;
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
        if (shotFx != null) shotFx.SetActive(false);   // Tắt để lần thắng sau bật lại sẽ phát lại từ đầu
        if (collectButton != null) collectButton.Hide();
        if (coinHud != null) coinHud.Release(false);   // Phòng trường hợp chưa bấm Collect mà sang màn mới
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
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
        winStars = Mathf.Clamp(hud != null ? hud.LitStars : 3, Mathf.Clamp(minWinStars, 1, 3), 3);
        winReward = starRewards != null && starRewards.Length >= winStars ? starRewards[winStars - 1] : 0;
        GrantWinReward();

        winRoom = lastRoom;
        winRoomAnimated = false;

        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = StartCoroutine(WinSequenceRoutine());
    }
    
    // Cộng và lưu coin ngay khi thắng; nút Collect chỉ chạy animation, HUD giữ số cũ tới lúc đó
    private void GrantWinReward()
    {
        if (useEditorStart || winReward <= 0) return;

        if (coinHud != null) coinHud.HoldDisplay();
        CoinWallet.Add(winReward);
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
            if (panelDelayAfterShot > 0f) yield return new WaitForSeconds(panelDelayAfterShot);   // Không chờ ShotFX chạy hết, panel mở sau ~0.3s
        }

        gameOverRoutine = null;
        ShowGameOverPanel(true);
    }
    
    private void ShowGameOverPanel(bool win)
    {
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (loseAnim != null) loseAnim.gameObject.SetActive(false);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(!win);

        if (win)
        {
            if (winAnim != null) winAnim.Play(winReward, winStars);
            else
            {
                if (coinHud != null) coinHud.Release(false);
                HandleRewardCollected();
            }
        }
        else if (loseAnim != null)
        {
            loseAnim.Play(lostByTimeOut);
        }
    }
    
    private void HandleRewardCollected()
    {
        bool hasNext = HasNextLevel;

        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(hasNext);
        if (restartButton != null) restartButton.gameObject.SetActive(!hasNext);
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
