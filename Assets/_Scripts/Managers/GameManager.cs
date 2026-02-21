using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Hearths")]
    [SerializeField] GameObject hearthOne;
    [SerializeField] GameObject hearthTwo;
    [SerializeField] GameObject hearthThree;
    [SerializeField] int lives = 3;

    void Awake()
    {
        Instance = this;
    }

    public void LostHearths()
    {
        lives --;
        switch (lives)
        {
            case 2:
                hearthThree.SetActive(false);
                break;
            
            case 1:
                hearthTwo.SetActive(false);
                break;
            
            case 0:
                hearthOne.SetActive(false);
                GameOver();    
                break;
            
        }
    }

    void GameOver()
    {
        // No more lives, game is over
    }
    
}
