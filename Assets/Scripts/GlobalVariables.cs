using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalVariables : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected GameObject I_PlayerTankPrefab;
    [SerializeField] protected GameObject I_BrownTankPrefab;
    [SerializeField] protected GameObject I_GreyTankPrefab;

    //Variables
    public static int CurrentLevel = 0;

    ///Protected Variables
    protected static Bullet[] m_Bullets = new Bullet[0];
    protected static int m_CurrentBulletIndex = -1;
    protected static Bomb[] m_Bombs = new Bomb[0];
    protected static int m_CurrentBombIndex = -1;

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
    public const string SpawnPointsName = "SpawnPoints";
    public const string PlayerTankSpawnPoint = "PlayerTankSpawnPoint";
    public const string BrownTankSpawnPoint = "BrownTankSpawnPoint";
    public const string GreyTankSpawnPoint = "GreyTankSpawnPoint";
    public const string NavyTankSpawnPoint = "NavyTankSpawnPoint";
    public const string EnemyTankObjectName = "Enemy Tanks";
    public const string TagBomb = "Bomb";
    public const string TagBullet = "Bullet";

    //Private Variables
    static bool m_HasStarted = false;

    private void Start()
    {
        if (!m_HasStarted)
        {
            DontDestroyOnLoad(this);
            m_HasStarted = true;
        }
        for (int i = 0; i < m_Bullets.Length; i++)
        {
            if (m_Bullets[i] != null)
                Destroy(m_Bullets[i].gameObject);
        }
        for (int i = 0; i < m_Bombs.Length; i++)
        {
            Destroy(m_Bombs[i].gameObject);
        }
        //Treds
        GameObject[] go = GameObject.FindGameObjectsWithTag("Tred");
        for (int i = 0; i < go.Length; i++)
        {
            if (go[i].transform.parent.name == gameObject.transform.name)
            {
                Destroy(go[i]);
            }
        }
        //DeathCross
        GameObject[] DethCrosses = GameObject.FindGameObjectsWithTag("DeathCross");
        for (int i = 0; i < DethCrosses.Length; i++)
        {
            if (DethCrosses[i].transform.parent.name == gameObject.transform.name)
            {
                Destroy(DethCrosses[i]);
            }
        }


        //Bullets
        m_Bullets = new Bullet[0];
        m_CurrentBulletIndex = -1;
        m_Bombs = new Bomb[0];
        m_CurrentBombIndex = -1;

        //Initialisizing Tanks
        int numTanks = GameObject.Find(SpawnPointsName).transform.childCount;
        if (numTanks != 0)
        {
            for (int i = 0; i < numTanks; i++)
            {
                Transform tank = GameObject.Find(SpawnPointsName).transform.GetChild(i);
                switch (tank.name)
                {
                    case PlayerTankSpawnPoint:
                        Instantiate(I_PlayerTankPrefab, tank.position, tank.rotation, transform.parent);
                        tank.gameObject.SetActive(false);
                        break;
                    case BrownTankSpawnPoint:
                        Instantiate(I_BrownTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                        tank.gameObject.SetActive(false);
                        break;
                    case GreyTankSpawnPoint:
                        Instantiate(I_GreyTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                        tank.gameObject.SetActive(false);
                        break;
                }
            }
        }
    }

    public void SetBullets(ref Bullet[] bullets)
    {
        foreach (Bullet b in bullets)
        {
            if (b.isActiveAndEnabled)
            {
                //Want to keep it
                if (m_Bullets.Length <= m_CurrentBulletIndex + 1)
                {
                    Bullet[] bullet = m_Bullets;
                    m_Bullets = new Bullet[m_CurrentBulletIndex + 2];
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
    public void SetBombs(ref Bomb[] bombs)
    {
        foreach (Bomb b in bombs)
        {
            if (b.isActiveAndEnabled)
            {
                //Want to keep it
                if (m_Bombs.Length <= m_CurrentBombIndex + 1)
                {
                    Bomb[] bomb = m_Bombs;
                    m_Bombs = new Bomb[m_CurrentBombIndex + 2];
                    for (int i = 0; i < bomb.Length; i++)
                    {
                        m_Bombs[i] = bomb[i];
                    }
                }

                m_Bombs[m_CurrentBombIndex + 1] = b;
                m_CurrentBombIndex++;
                m_Bombs[m_CurrentBombIndex].SetLevelInArray(m_CurrentBombIndex);

                m_Bombs[m_CurrentBombIndex].transform.parent = gameObject.transform;
            }
        }
    }
    public void DestroyBullet(int numberInArray)
    {
        if (m_Bullets[numberInArray])
            Destroy(m_Bullets[numberInArray].gameObject);
        //Drop all the bullets back
        for (int i = numberInArray; i < m_Bullets.Length -1; i++)
        {
            m_Bullets[i] = m_Bullets[i + 1];
        }
        m_CurrentBulletIndex--;
    }
    public void ExplodeBomb(int numberInArray)
    {
        if (m_Bombs[numberInArray])
            Destroy(m_Bombs[numberInArray].gameObject);
        //Drop all the bullets back
        for (int i = numberInArray; i < m_Bombs.Length - 1; i++)
        {
            m_Bombs[i] = m_Bombs[i + 1];
        }
        m_CurrentBombIndex--;
    }
}
