using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;



public class ScoreManager : MonoBehaviour
{
    public int score = 0;
    public TMP_Text scoreText;
    public int cooperationBonusPoints;
    public int extinguisherPoints;
    public int partnerClimbPoints;
    public int suppressionUnitPoints;

    public float climbMultiplier;


    // --------------------
    // Add Points
    // --------------------

    public void AddCooperationBonus(int amount)
    {
        cooperationBonusPoints += amount;
    }

    public void AddExtinguisherPoints(int amount)
    {
        extinguisherPoints += amount;
    }

    public void AddPartnerClimbPoints(int amount)
    {
        partnerClimbPoints += amount;
    }

    public void AddSuppressionUnitPoints(int amount)
    {
        suppressionUnitPoints += amount;
    }

    public void AddClimbMultiplier(float amount)
    {
        climbMultiplier += amount;
    }

    // --------------------
    // Optional Reset
    // --------------------

    public void ResetScore()
    {
        cooperationBonusPoints = 0;
        extinguisherPoints = 0;
        partnerClimbPoints = 0;
        suppressionUnitPoints = 0;
        climbMultiplier = 0f;
    }

    // --------------------
    // Final Match Score
    // --------------------

    public int GetMatchScore()
    {
        return Mathf.CeilToInt(
            cooperationBonusPoints +
            extinguisherPoints +
            partnerClimbPoints +
            (climbMultiplier * suppressionUnitPoints)
        );
    }
    void Update()
    {
        score = GetMatchScore();

        if (scoreText != null)
        {
            scoreText.text = "Score : " + score;
        }
    }

}


