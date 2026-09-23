using System.Collections;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreMultiplyerManager scoreMultiplyerManager;
    [SerializeField] private BallPickUp ballpickup;
    [SerializeField] private HumanShooter humanShooter;
    [SerializeField] private RealisticTankDrive tankDrive;
    [SerializeField] private SpherePoolUI spherePool;
    [SerializeField] private ExtinguisherScore extinguisherScore;   // ← add this

    [Header("UI")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject prepareButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject timerUI;
    [SerializeField] private GameObject scoreUI;               // ← new
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timers")]
    [SerializeField] private float prepareTime = 30f;
    [SerializeField] private float gameTime = 150f;

    private float remainingTime;
    private bool isPreparePhase = false;
    private bool isGamePhase = false;
    private bool gameEnded = false;

    void Start()
    {
        if (welcomePanel != null) welcomePanel.SetActive(true);
        if (prepareButton != null) prepareButton.SetActive(true);
        if (startButton != null) startButton.SetActive(false);
        if (timerUI != null) timerUI.SetActive(false);
        if (scoreUI != null) scoreUI.SetActive(false);         // hidden at start

        SetGameplayEnabled(false);
    }

    void Update()
    {
        if (isPreparePhase || isGamePhase)
        {
            remainingTime = Mathf.Max(0, remainingTime - Time.deltaTime);

            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (remainingTime <= 0f)
            {
                if (isPreparePhase)
                {
                    OnPrepareFinished();
                }
                else if (isGamePhase && !gameEnded)
                {
                    OnGameFinished();
                }
            }
        }
    }

    public void OnPrepareGamePressed()
    {
        if (prepareButton != null)
            prepareButton.SetActive(false);

        if (spherePool != null)
            spherePool.ActivateSpawn();

        remainingTime = prepareTime;
        isPreparePhase = true;
        isGamePhase = false;

        if (timerUI != null)
            timerUI.SetActive(true);

        timerText.color = Color.white;
    }

    private void OnPrepareFinished()
    {
        isPreparePhase = false;

        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (startButton != null) startButton.SetActive(true);

        if (timerUI != null) timerUI.SetActive(false);
    }

    public void StartTimer()
    {
        if (startButton != null) startButton.SetActive(false);

        remainingTime = gameTime;
        isGamePhase = true;
        isPreparePhase = false;
        gameEnded = false;

        if (timerUI != null) timerUI.SetActive(true);
        if (scoreUI != null) scoreUI.SetActive(true);

        timerText.color = Color.white;

        // Enable gameplay
        SetGameplayEnabled(true);

        // Start the extinguisher scoring
        if (extinguisherScore != null)
            extinguisherScore.StartExtinguisherSystem();
    }

    private void OnGameFinished()
    {
        gameEnded = true;
        remainingTime = 0;
        timerText.color = Color.red;

        scoreMultiplyerManager.CheckRobotLocation();
        SetGameplayEnabled(false);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (tankDrive != null) tankDrive.enabled = enabled;
        if (ballpickup != null) ballpickup.enabled = enabled;
        if (humanShooter != null) humanShooter.enabled = enabled;
    }
}