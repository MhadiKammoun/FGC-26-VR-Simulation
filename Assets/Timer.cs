using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private ScoreMultiplyerManager scoreMultiplyerManager;
    [SerializeField] private BallPickUp ballpickup;
    
    [SerializeField] private MovemntScript movementScript;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;

    void Update()
    {
        remainingTime = Mathf.Max(0, remainingTime - Time.deltaTime);
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        TimerEnds();
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
       

    }
    public void TimerEnds()
    {

       
       


        if (remainingTime <= 0f)

        {


            scoreMultiplyerManager.CheckRobotLocation();
            
            remainingTime = 0;
            movementScript.enabled = false;
            ballpickup.enabled = false;
            
            

        }
    }
}

