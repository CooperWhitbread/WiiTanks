using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalVariables : MonoBehaviour
{
    //Variables
    public static int CurrentLevel = 0;

    //Game Constants
    public const int LayerFloorBoards  = 8;
    public const int LayerTanks        = 9;
    public const int LayerWalls        = 10;
    public const int LayerBullets      = 11;
    public const string PlayerTankName = "Player Tank";

    //Maintainance Variables
    private static bool m_Start = false;

    private void Start()
    {
        if (!m_Start)
        {
            DontDestroyOnLoad(this);
            m_Start = true;
        }
    }
}
