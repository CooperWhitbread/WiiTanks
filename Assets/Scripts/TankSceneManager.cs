using UnityEngine;
using UnityEngine.SceneManagement;

public class TankSceneManager : MonoBehaviour
{
    public bool I_Testing = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!I_Testing)
        {
            if (GlobalVariables.CurrentLevel == 0)
            {
                SceneManager.LoadScene("Level-" + (GlobalVariables.CurrentLevel + 1));
                GlobalVariables.CurrentLevel++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.Find(GlobalVariables.PlayerTankName) == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        /*if (!I_Testing)
        {
            //Check if Player is dead
            if (GameObject.Find(GlobalVariables.PlayerTankName) == null)
            {
                SceneManager.LoadScene("Level-" + GlobalVariables.CurrentLevel);
            }
        }
        else
        {
            //Check if Player is dead
            if (GameObject.Find(GlobalVariables.PlayerTankName) == null)
            {
                SceneManager.LoadScene("TestingScene");
            }
        }*/
    }
}
