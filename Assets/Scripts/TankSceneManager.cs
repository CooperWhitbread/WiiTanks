using UnityEngine;
using UnityEngine.SceneManagement;

public class TankSceneManager : MonoBehaviour
{
    public bool I_Testing = false;
    public int I_NumberOfLevels = 1;
    public int I_StartingLevel = 1;
    public bool I_IsLoading = false;

    private int m_CurrentLevel = 0;
    private bool m_IsOnFirstLoad = true;

    void Update()
    {
        if (I_Testing)
        {
            //In testing scene
            if (!I_IsLoading)
            {
                //Reset testing screne when player dies
                if (GlobalVariables.GetPlayerTankBody())
                {
                    if (GlobalVariables.GetPlayerTankBody().GetComponent<SpriteRenderer>().enabled == false)
                    {
                        SceneManager.LoadSceneAsync("Dead");
                        I_IsLoading = true;
                    }
                }

                //Switch back to game screen if the screen is dead
                if (SceneManager.GetActiveScene().name == "Dead")
                {
                    SceneManager.LoadSceneAsync("TestingScene");
                    I_IsLoading = true;
                }

            }
        }
        else
        {
            //Switch back to game screen if the screen is dead
            if (SceneManager.GetActiveScene().name == "Dead")
            {
                if (!I_IsLoading)
                {
                    GlobalVariables.GetThisInstance().ResetFunctionsEvenIfDead();
                    SceneManager.LoadSceneAsync("Mission-" + (m_CurrentLevel));
                    I_IsLoading = true;
                }
            }
            else if (m_IsOnFirstLoad) //Starting screen
            {
                m_CurrentLevel = I_StartingLevel;
                SceneManager.LoadSceneAsync("Mission-" + (m_CurrentLevel));
                I_IsLoading = true;
                m_IsOnFirstLoad = false;
            }
            else if (!I_IsLoading) //Skip if it is loading at the moment
            {
                //Check if player is alive
                if (m_CurrentLevel != 0 && GlobalVariables.GetPlayerTankBody())
                {
                    if (GlobalVariables.GetPlayerTankBody().GetComponent<SpriteRenderer>().enabled == false)
                    {
                        SceneManager.LoadSceneAsync("Dead");
                        I_IsLoading = true;
                    }
                }
                //Check there are tanks alive or not
                Transform go = GameObject.Find(GlobalVariables.EnemyTankObjectName).transform;
                bool tanksLeft = false;
                for (int i = 0; i < go.childCount; i++)
                {
                    if (go.GetChild(i).GetComponent<Tank>().IsAlive())
                    {
                        tanksLeft = true;
                        break;
                    }
                }

                if (!tanksLeft)
                {
                    //Check if still a level to go on to
                    if (I_NumberOfLevels > m_CurrentLevel)
                    {
                        m_CurrentLevel++;

                        GlobalVariables.GetThisInstance().m_NonSpawingPositions = new Vector3[0];
                        SceneManager.LoadSceneAsync("Mission-" + (m_CurrentLevel));
                        I_IsLoading = true;
                    }
                }
            }
        }
    }
}
