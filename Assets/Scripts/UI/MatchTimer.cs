using System;
using UnityEngine;
using TMPro;

public class MatchTimer : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color endedColor = Color.red;

    [Header("Settings")]
    [SerializeField] private float matchDurationSeconds = 150f; // e.g., 2:30

    public event Action OnMatchTimerEnd;

    private float remainingTime;
    private bool isRunning = false;
    private int lastDisplayedSecond = -1;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        remainingTime = matchDurationSeconds;
        UpdateDisplay(forceUpdate: true);
    }

    private void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            UpdateDisplay(forceUpdate: true);
            timerText.color = endedColor;
            OnMatchTimerEnd?.Invoke();
            return;
        }

        UpdateDisplay(forceUpdate: false);
    }

    public void StartTimer()
    {
        timerText.color = normalColor;
        remainingTime = matchDurationSeconds;
        lastDisplayedSecond = -1;
        isRunning = true;
        UpdateDisplay(forceUpdate: true);
    }

    public void ResetTimer()
    {
        isRunning = false;
        remainingTime = matchDurationSeconds;
        timerText.color = normalColor;
        lastDisplayedSecond = -1;
        UpdateDisplay(forceUpdate: true);
    }

    private void UpdateDisplay(bool forceUpdate)
    {
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        if (!forceUpdate && totalSeconds == lastDisplayedSecond) return;

        lastDisplayedSecond = totalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // Zero-allocation text update using SetText
        timerText.SetText("{0:00}:{1:00}", minutes, seconds);
    }
}