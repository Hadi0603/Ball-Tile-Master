using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Game Win")]
    [SerializeField] private GameObject winCeleb;
    [SerializeField] private CanvasGroup levelWonUI;
    [SerializeField] private GameObject winPanel;
    
    [Header("Game Pause")]
    [SerializeField] private CanvasGroup pauseUI;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseBtn;
    
    [Header("Level Lost")]
    [SerializeField] private CanvasGroup levelLostUI;
    [SerializeField] private GameObject lostPanel;
    
    [Header("Gameplay")]
    [SerializeField] private Text timerText;
    [SerializeField] Text movesLeftText;
    [SerializeField] GameObject tutorialText;
    [SerializeField] private GameObject tutorialText1;
    [SerializeField] public float gameTime = 60f;
    [SerializeField] AudioSource winSound;
    [SerializeField] AudioSource gameOverSound;
    
    [SerializeField] GameObject discMover;

    
    private bool isBlinking = false;
    private bool isPaused = false;
    
    

    
    public void Awake()
    {
        levelWonUI.alpha = 0f;
        winPanel.transform.localPosition = new Vector2(0, +Screen.height);
        levelLostUI.alpha = 0f;
        lostPanel.transform.localPosition = new Vector2(0, +Screen.height);
        pauseUI.alpha = 0f;
        pausePanel.transform.localPosition = new Vector2(0, +Screen.height);
        StartCoroutine(TimerCountdown());
        //slowTimeButton.onClick.AddListener(ActivateSlowTime);
        
    }

    
    
    public void UpdateMovesLeft(int moves)
    {
        if (movesLeftText != null)
        {
            movesLeftText.text = "Moves: " + moves.ToString();
        }
        else
        {
            Debug.LogError("movesLeftText is not assigned in the Inspector!");
        }
    }
    
    public void TriggerGameWon()
    {
        Debug.Log("Game Won! All discs are destroyed.");
        winSound.Play();
        levelWonUI.gameObject.SetActive(true);
        if (tutorialText != null && tutorialText1 != null)
        {
            tutorialText.SetActive(false);
            tutorialText1.SetActive(false);
        }
        levelWonUI.LeanAlpha(1, 0.5f);
        pauseBtn.SetActive(false);
        StopCoroutine(TimerCountdown());
        Destroy(timerText);
        winPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        winCeleb.SetActive(true);
        if (GameManager.levelToLoad < 11)
        {
            PlayerPrefs.SetInt("levelToLoad", ++GameManager.levelToLoad);
        }
        PlayerPrefs.Save();
    }

    public void OpenPauseMenu()
    {
        pauseUI.gameObject.SetActive(true);
        pauseUI.LeanAlpha(1, 0.5f);
        pauseBtn.SetActive(false);
        pausePanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        isPaused = true;
        discMover.SetActive(false);
    }

    public void ClosePauseMenu()
    {
        pauseUI.LeanAlpha(0, 0.5f);
        pausePanel.LeanMoveLocalY(+Screen.height, 0.5f).setEaseInExpo();
        pauseBtn.SetActive(true);
        isPaused = false;
        discMover.SetActive(true);
        Invoke(nameof(DisablePauseUI), 0.5f);
    }

    private void DisablePauseUI()
    {
        pauseUI.gameObject.SetActive(false);
    }

    private IEnumerator TimerCountdown()
    {
        while (gameTime > 0)
        {
            if (!isPaused) // Only update the timer if the game is not paused
            {
                gameTime -= Time.deltaTime;
                UpdateTimerDisplay();

                if (gameTime <= 15f && !isBlinking)
                {
                    StartCoroutine(BlinkTimer());
                }
            }
            yield return null;
        }

        GameOver();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private IEnumerator BlinkTimer()
    {
        isBlinking = true;
        Color originalColor = timerText.color;
    
        while (gameTime <= 15f && gameTime > 0)
        {
            timerText.color = (timerText.color == Color.red) ? Color.white : Color.red;
            yield return new WaitForSeconds(0.5f);
        }

        timerText.color = originalColor;
        isBlinking = false;
    }

    public void GameOver()
    {
        Debug.Log("Time's up! Game Over.");
        gameOverSound.Play();
        pauseBtn.SetActive(false);
        if (tutorialText != null && tutorialText1 != null)
        {
            tutorialText.SetActive(false);
            tutorialText1.SetActive(false);
        }
        levelLostUI.gameObject.SetActive(true);
        timerText.enabled = false;
        levelLostUI.LeanAlpha(1, 0.5f);
        lostPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
    }
}
