using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    public bool UnlimitedTime { get; set; }
    public bool UnlimitedMoves { get; set; }
    public int ForcedStars { get; set; }
    
    public int LevelCount => levels != null ? levels.Length : 0;
    public int CurrentLevelNumber => currentLevelIndex + 1;
    public float TimeRemaining => timeRemaining;
    public int MovesRemaining => movesRemaining;
    public bool HasTimeLimit => currentLevel != null && currentLevel.HasTimeLimit;
    public bool HasMoveLimit => currentLevel != null && currentLevel.HasMoveLimit;
    public bool IsEditorStart => useEditorStart;
    public bool TimerPaused { get; set; }
    
    [SerializeField] private TextAsset levelsJson;
    
    [Header("Input")]
    [SerializeField] private RoomBuildController buildController;
    
    [Header("HUD")]
    [SerializeField] private LevelHud hud;
    
    [Header("Win FX")]
    [SerializeField] private float roomAnimationTimeout = 3f;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;

    [Header("Game Over Animation")]
    [SerializeField] private WinAnimController winAnim;
    
    [Header("Reward")]
    [SerializeField] private CollectButton collectButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private int[] starRewards = { 5, 10, 20 }; 
    [SerializeField] private int minWinStars = 1;        
    [SerializeField] private CoinHud coinHud;

    [Header("Revive")]
    [SerializeField] private ReviveTimeOutPanel revivePanel;   
    [SerializeField] private GameObject reviveRoot;    
    [SerializeField] private LosePanel losePanel;        
    [SerializeField] private int revivePrice = 900;
    [SerializeField] private int reviveBonusSeconds = 20; 
    [SerializeField] private int reviveBonusMoves = 5; 
    [SerializeField] private int maxRevivesPerLevel = 1;
    
    [Header("Freeze Booster")]
    [SerializeField] private float freezeDuration = 10f;
    
    [Header("Freeze FX")]
    [SerializeField] private FreezeOverlayFX freezeFx;
    [SerializeField] private float freezeWarningTime = 3f;
    
    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorial;
    
    public bool IsTutorial { get; set; }
    public bool HasLevelLoaded => currentLevel != null;
    public bool LevelStarted => timerRunning;   // Người chơi đã chạm vào một ô của lưới (đồng hồ/booster bắt đầu hoạt động)

    public const string EditorStartLevelKey = "Shikacat.StartLevelIndex";
    
    public event Action LevelLoaded;
    public event Action<float, float> FreezeProgress;
    public event Action FreezeEnded;
    
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
    private int revivesUsed;
    private float freezeTimeLeft;
    private bool freezeIntro;
    private bool freezeWarned;

    public bool IsFrozen => freezeTimeLeft > 0f;
    public float FreezeRemaining => freezeTimeLeft;
    public float FreezeDuration => freezeDuration;

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
        if (revivePanel != null)
        {
            revivePanel.ReviveRequested += HandleReviveRequested;
            revivePanel.Declined += HandleReviveDeclined;
        }
        if (losePanel != null)
        {
            losePanel.RetryRequested += HandleLoseRetry;
            losePanel.Closed += HandleLoseClosed;
        }
    }

    private void OnDisable()
    {
        if (revivePanel != null)
        {
            revivePanel.ReviveRequested -= HandleReviveRequested;
            revivePanel.Declined -= HandleReviveDeclined;
        }
        if (losePanel != null)
        {
            losePanel.RetryRequested -= HandleLoseRetry;
            losePanel.Closed -= HandleLoseClosed;
        }
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
        int startIndex = useEditorStart ? editorIndex : PlayerProgress.LoadLevelIndex();
        if (!useEditorStart && tutorial != null && PlayerProgress.ShouldShowTutorial)
            tutorial.Begin(() => LoadLevel(startIndex));
        else
            LoadLevel(startIndex);
        
        if (BoosterManager.Instance != null)
            BoosterManager.Instance.SetHandler(BoosterType.Freeze, TryFreeze);
    }

    private void Update()
    {
        if (IsTutorial) return;

        TickFreeze();
        
        if (isGameOver || currentLevel == null || !currentLevel.HasTimeLimit || !timerRunning) return;
        if (TimerPaused) return;
        if (UnlimitedTime || IsFrozen) return;
        
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

    public void CheatWin()
    {
        if (isGameOver || currentLevel == null) return;
        
        HandleWin(null);
        winRoomAnimated = true;  
    }

    public void CheatLose(bool byTimeOut)
    {
        if (isGameOver || currentLevel == null) return;
        HandleLose(byTimeOut);
    }
    
    public void CheatAddTime(float seconds)
    {
        if (!HasTimeLimit) return;

        timeRemaining += seconds;
        displayedSeconds = -1;
        UpdateTimeHud();
    }

    public void CheatAddMoves(int moves)
    {
        if (!HasMoveLimit) return;

        movesRemaining += moves;
        UpdateMovesHud();
    }
    
    public void CheatEndFreeze() => EndFreeze(true);
    
    public void CheatGoToLevel(int levelNumber) => LoadLevel(levelNumber - 1);
    
    public void CheatRestart() => LoadLevel(currentLevelIndex);

    public void CheatReplayTutorial()
    {
        if (tutorial == null || IsTutorial || isGameOver) return;

        PlayerProgress.ClearTutorial();
        int index = currentLevelIndex;
        tutorial.Begin(() => LoadLevel(index));
    }

    public void CheatSaveCurrentLevel() => PlayerProgress.SaveLevelIndex(currentLevelIndex);

    public void CheatClearSave()
    {
        PlayerProgress.Clear();
        BoosterTutorial.ResetSeen();
        MechanicIntro.ResetSeen();
        BoosterInventory.Clear();
        CoinWallet.Clear();
        LoadLevel(0);
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
        
        EndFreeze(true);

        isGameOver = false;
        timerRunning = false;
        timeRemaining = currentLevel.timeLimit;
        movesRemaining = currentLevel.moveLimit;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        revivesUsed = 0;
        ClosePopups();
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (collectButton != null) collectButton.Hide();
        if (coinHud != null) coinHud.Release(false);
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
        if (IsTutorial || isLoading || isGameOver) return;
        
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
        if (IsTutorial || isLoading || isGameOver || GridManager.Instance.IsReplacingRooms) return;

        ConsumeMove();
        CheckMoveLimitLose();
    }

    private void ConsumeMove()
    {
        if (!currentLevel.HasMoveLimit || UnlimitedMoves || IsFrozen || GridManager.Instance.IsRevealing) return;

        movesRemaining--;
        UpdateMovesHud();
    }

    private void CheckMoveLimitLose()
    {
        if (UnlimitedMoves) return;
        
        if (currentLevel.HasMoveLimit && movesRemaining <= 0)
            HandleLose();
    }

    private void HandleWin(Room lastRoom)
    {
        if (isGameOver) return;
        isGameOver = true;

        SaveProgress();
        winStars = ForcedStars > 0
            ? Mathf.Clamp(ForcedStars, 1, 3)
            : Mathf.Clamp(hud != null ? hud.LitStars : 3, Mathf.Clamp(minWinStars, 1, 3), 3);
        winReward = starRewards != null && starRewards.Length >= winStars ? starRewards[winStars - 1] : 0;
        GrantWinReward();

        winRoom = lastRoom;
        winRoomAnimated = false;

        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = StartCoroutine(WinSequenceRoutine());
    }
    
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

        if (revivePanel != null && revivesUsed < maxRevivesPerLevel) ShowRevive(); 
        else ShowGameOverPanel(false);
    }

    private void ShowRevive()
    {
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (reviveRoot != null) reviveRoot.SetActive(true);

        revivePanel.Show(lostByTimeOut ? LoseReason.TimeOut : LoseReason.OutOfMoves,
            revivePrice, lostByTimeOut ? reviveBonusSeconds : reviveBonusMoves);
    }

    private void HandleReviveRequested()
    {
        if (!CoinWallet.TrySpend(revivePrice))
        {
            revivePanel.ShowNotEnoughCoins();
            return;
        }

        revivesUsed++;

        if (lostByTimeOut) timeRemaining += reviveBonusSeconds;
        else movesRemaining += reviveBonusMoves;

        isGameOver = false;
        ClosePopups();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        displayedSeconds = -1;
        if (currentLevel.HasTimeLimit) UpdateTimeHud();
        if (currentLevel.HasMoveLimit) UpdateMovesHud();

        timerRunning = true;
    }

    private void HandleReviveDeclined()
    {
        ClosePopups();
        ShowGameOverPanel(false);
    }

    private void ClosePopups()
    {
        if (revivePanel != null) revivePanel.Hide();
        if (losePanel != null) losePanel.Hide();
        if (reviveRoot != null) reviveRoot.SetActive(false);
    }

    private void ShowLose()
    {
        if (winAnim != null) winAnim.gameObject.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (reviveRoot != null) reviveRoot.SetActive(true);

        losePanel.Show(currentLevelIndex + 1, 0);
    }

    private void HandleLoseRetry()
    {
        LoadLevel(currentLevelIndex);
    }
    
    private void HandleLoseClosed()
    {
        ClosePopups();
        if (restartButton != null) restartButton.gameObject.SetActive(true);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
    }

    private IEnumerator WinSequenceRoutine()
    {
        float waited = 0f;
        while (!winRoomAnimated && waited < roomAnimationTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        gameOverRoutine = null;
        ShowGameOverPanel(true);
    }
    
    private void ShowGameOverPanel(bool win)
    {
        ClosePopups();

        if (!win)
        {
            if (losePanel != null) ShowLose();
            else
            {
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                if (restartButton != null) restartButton.gameObject.SetActive(true);
            }
            return;
        }

        if (winAnim != null) winAnim.gameObject.SetActive(false);
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
    }
    
    private void HandleRewardCollected()
    {
        bool hasNext = HasNextLevel;

        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(hasNext);
        if (restartButton != null) restartButton.gameObject.SetActive(!hasNext);
    }

    private bool TryFreeze()
    {
        if (isGameOver || currentLevel == null || IsFrozen) return false;
        
        freezeTimeLeft = freezeDuration;
        freezeWarned = false;
        freezeIntro = freezeFx != null;
        FreezeProgress?.Invoke(freezeTimeLeft, freezeDuration);

        if (freezeFx != null) freezeFx.PlayIntro(HandleFreezeBordersShown);
        else if (hud != null) hud.SetFrozen(true);

        return true;
    }
    
    private void HandleFreezeBordersShown()
    {
        freezeIntro = false;
        if (hud != null && IsFrozen) hud.SetFrozen(true);
    }

    
    private void TickFreeze()
    {
        if (freezeTimeLeft <= 0f) return;

        if (isGameOver)
        {
            EndFreeze();
            return;
        }
        if (freezeIntro || TimerPaused || !timerRunning) return;

        freezeTimeLeft -= Time.deltaTime;
        if (freezeTimeLeft <= 0f)
        {
            EndFreeze();
            return;
        }

        FreezeProgress?.Invoke(freezeTimeLeft, freezeDuration);

        if (!freezeWarned && freezeTimeLeft <= freezeWarningTime)
        {
            freezeWarned = true;
            if (freezeFx != null) freezeFx.PlayWarning();
        }
    }

    private void EndFreeze(bool immediate = false)
    {
        freezeTimeLeft = 0f;
        freezeIntro = false;
        if (hud != null) hud.SetFrozen(false);
        if (freezeFx != null) freezeFx.Stop(immediate);
        FreezeEnded?.Invoke();
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
