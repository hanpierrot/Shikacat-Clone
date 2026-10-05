using System;
using System.Collections;
using System.Collections.Generic;
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
    
    private LevelData[] levels;
    private LevelData currentLevel;
    private int currentLevelIndex;
    private float timeRemaining;
    private int movesRemaining;
    private bool isGameOver;
    private bool isLoading;
    
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
        LoadLevel(0);
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

    private void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0) return;
        
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
        
        UpdateTimerText();
        UpdateMoveText();
    }
    
    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
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
        
        if (GridManager.Instance.IsGridFull())
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
        if (gameOverImage != null && gameOverPanel != null && nextLevelButton != null)
        {
            gameOverImage.sprite = winSprite;
            gameOverPanel.SetActive(true);
            nextLevelButton.gameObject.SetActive(true);
        }
    }

    private void HandleLose()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (gameOverImage != null && gameOverPanel != null && restartButton != null)
        {
            gameOverImage.sprite = loseSprite;
            gameOverPanel.SetActive(true);
            restartButton.gameObject.SetActive(true);
        }
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
