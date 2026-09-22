using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
     

public class Timer : MonoBehaviour
{
    [SerializeField] private ScoreMultiplyerManager scoreMultiplyerManager;
    [SerializeField] private BallPickUp ballpickup;
    [SerializeField] private HumanShooter humanShooter;
    [SerializeField] private MovemntScript movementScript;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;
    private bool timerStarted = false;
    private bool gameEnded = false;


    void Update()
    {
        if (timerStarted)
        {
            remainingTime = Mathf.Max(0, remainingTime - Time.deltaTime);
            TimerEnds();
        }
        
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
       

    }
    public void TimerEnds()
    {

       
       


        if (remainingTime <= 0f && !gameEnded)

        {
            gameEnded = true;


            scoreMultiplyerManager.CheckRobotLocation();
            
            remainingTime = 0;
            timerText.color = Color.red;
            movementScript.enabled = false;
            ballpickup.enabled = false;
            humanShooter.enabled = false;

            




        }
    }
    public void StartTimer()
    {
        timerStarted = true;
    }
}


