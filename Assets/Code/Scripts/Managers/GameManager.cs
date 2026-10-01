using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private GridConfig[] levels;
    [SerializeField] private TMP_Text timerText;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverImage;
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    
    private int currentLevelIndex = 0;
    private float timeRemaining;
    private bool isGameOver;
    
    private void OnEnable()
    {
        GridManager.Instance.RoomCommitted += HandleRoomCommitted;
    }

    private void OnDisable()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.RoomCommitted -= HandleRoomCommitted;
    }

    private void Start()
    {
        LoadLevel(0);
    }

    private void Update()
    {
        if (isGameOver) return;
        
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
        GridConfig level = levels[currentLevelIndex];

        isGameOver = false;
        timeRemaining = level.timeLimit;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        GridManager.Instance.BuildGrid(level);
        UpdateTimerText();
    }
    
    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void HandleRoomCommitted(Room room)
    {
        if (isGameOver) return;
        if (GridManager.Instance.IsGridFull())
            HandleWin();
    }

    private void HandleWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (gameOverImage != null)
        {
            gameOverImage.sprite = winSprite;
            gameOverPanel.SetActive(true);
            nextLevelButton.gameObject.SetActive(true);
        }
    }

    private void HandleLose()
    {
        isGameOver = true;
        if (gameOverImage != null)
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
