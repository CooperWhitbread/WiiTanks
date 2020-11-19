using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalVariables : MonoBehaviour
{
    //Variables
    public static int CurrentLevel = 0;
    protected Bullet[] m_Bullets = new Bullet[1];
    protected int m_CurrentBulletIndex = -1;

    //Game Constants
    public const int LayerFloorBoards  = 8;
    public const int LayerTanks        = 9;
    public const int LayerWalls        = 10;
    public const int LayerBullets      = 11;
    public const string PlayerTankName = "Player Tank";
    public const string PlayerTankBodyName = "PlayerBody";
    public const string TankLayerName = "Tanks";
    public const string WallLayerName = "Walls";
    public const string BulletLayerName = "Bullets";
    public const string FloorLayerName = "FloorBoards";
    public const string GlobalVariableObjectName = "Global";

    //Maintainance Variables
    private static bool m_Start = false;

    private void Start()
    {
        if (!m_Start)
        {
            DontDestroyOnLoad(this);
            m_Start = true;
        }

        m_Bullets = new Bullet[1];
        m_CurrentBulletIndex = -1;
    }

    public void SetBullets(Bullet[] bullets)
    {
        foreach (Bullet b in bullets)
        {
            if (b.isActiveAndEnabled)
            {
                //Want to keep it
                if (m_Bullets.Length <= m_CurrentBulletIndex + 1)
                {
                    Bullet[] bullet = m_Bullets;
                    m_Bullets = new Bullet[m_CurrentBulletIndex + 1];
                    for (int i = 0; i < bullet.Length; i++)
                    {
                        m_Bullets[i] = bullet[i];
                    }
                }

                m_Bullets[m_CurrentBulletIndex + 1] = b;
                m_CurrentBulletIndex++;
                m_Bullets[m_CurrentBulletIndex].SetLevelInArray(m_CurrentBulletIndex);

                m_Bullets[m_CurrentBulletIndex].transform.parent = gameObject.transform;
            }
        }
    }

    public void DestroyBullet(int numberInArray)
    {
        Destroy(m_Bullets[numberInArray].gameObject);
        //Drop all the bullets back
        for (int i = numberInArray; i < m_Bullets.Length -1; i++)
        {
            m_Bullets[i] = m_Bullets[i + 1];
        }
        m_CurrentBulletIndex--;
    }
}
