using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManagerUI : MonoBehaviour
{
    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Ball Counts (1 Point Each)")]
    [SerializeField] private int compressionBalls = 0;
    [SerializeField] private int extinguisherBalls = 0;

    [Header("Endgame Multiplier")]
    [SerializeField] private float climbMultiplier = 0f;

    // Registry of all active scoring zones on the field
    private readonly List<ScoringZone> registeredZones = new List<ScoringZone>();

    public int CompressionBalls => compressionBalls;
    public int ExtinguisherBalls => extinguisherBalls;
    public float ClimbMultiplier => climbMultiplier;

    private void Awake()
    {
        UpdateScoreDisplay();
    }

    public void RegisterScoringZone(ScoringZone zone)
    {
        if (!registeredZones.Contains(zone)) registeredZones.Add(zone);
    }

    public void UnregisterScoringZone(ScoringZone zone)
    {
        registeredZones.Remove(zone);
    }

    public void AddCompressionBall()
    {
        compressionBalls++;
        UpdateScoreDisplay();
    }

    public void AddExtinguisherBall()
    {
        extinguisherBalls++;
        UpdateScoreDisplay();
    }

    public void SetClimbMultiplier(float multiplier)
    {
        climbMultiplier = Mathf.Max(0f, multiplier);
        UpdateScoreDisplay();
    }

    public int GetTotalScore()
    {
        int multipliedCompression = Mathf.CeilToInt(compressionBalls * (1f + climbMultiplier));
        return extinguisherBalls + multipliedCompression;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText == null) return;
        scoreText.SetText("Score: {0}", GetTotalScore());
    }

    /// <summary>
    /// Zeroes out counts, resets UI, and purges all ScoringZone ball registries.
    /// </summary>
    public void ResetScore()
    {
        compressionBalls = 0;
        extinguisherBalls = 0;
        climbMultiplier = 0f;
        UpdateScoreDisplay();

        // Clear HashSets across all triggers
        for (int i = 0; i < registeredZones.Count; i++)
        {
            if (registeredZones[i] != null) registeredZones[i].ResetZone();
        }
    }
}