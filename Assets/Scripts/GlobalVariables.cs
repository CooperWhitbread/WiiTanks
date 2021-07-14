using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalVariables : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] public Tank I_PlayerTankPrefab;
    [SerializeField] public Tank I_BrownTankPrefab;
    [SerializeField] public Tank I_GreyTankPrefab;
    [SerializeField] public Tank I_TealTankPrefab;
    [SerializeField] public Tank I_YellowTankPrefab;
    [SerializeField] public Tank I_GreenTankPrefab;
    [SerializeField] public Tank I_PinkTankPrefab;
    [SerializeField] public Tank I_PurlpeTankPrefab;
    [SerializeField] public Tank I_WhiteTankPrefab;
    [SerializeField] public Tank I_BlackTankPrefab;
    [SerializeField] public Bullet I_BulletPrefab;
    [SerializeField] public Bomb I_BombPrefab;
    [SerializeField] public GameObject I_TredPrefab;
    [SerializeField] public GameObject I_DeathCrossPrefab;
    [SerializeField] protected BoxCollider2D I_CameraBounds;

    ///Protected Variables
    protected static Bullet[] m_Bullets = new Bullet[0];
    protected static int m_CurrentBulletIndex = -1;
    protected static Bomb[] m_Bombs = new Bomb[0];
    protected static int m_CurrentBombIndex = -1;

    //Layers
    public const int LayerFloorBoards  = 8;
    public const int LayerTanks        = 9;
    public const int LayerWalls        = 10;
    public const int LayerBullets      = 11;
    public const int LayerWallHole     = 12;

    //Tank Name
    public const string PlayerTankName = "Player Tank";
    public const string PlayerTankBodyName = "PlayerBody";
    public const string TurretName = "Turret";

    //Layer Names
    public const string TankLayerName = "Tanks";
    public const string WallLayerName = "Walls";
    public const string BulletLayerName = "Bullets";
    public const string FloorLayerName = "FloorBoards";

    public const string GlobalVariableObjectName = "Global";
    static public bool HasJustStartedAMission = false;

    //Tilemaps
    public const string TilemapWallName = "Walls";
    public const string TilemapHoleName = "Holes";
    public const string TilemapFloorName = "Floor";

    //Spawn Point Names
    public const string SpawnPointsName = "SpawnPoints";
    public const string PlayerTankSpawnPoint = "PlayerTankSpawnPoint";
    public const string BrownTankSpawnPoint = "BrownTankSpawnPoint";
    public const string GreyTankSpawnPoint = "GreyTankSpawnPoint";
    public const string TealTankSpawnPoint = "TealTankSpawnPoint";
    public const string YellowTankSpawnPoint = "YellowTankSpawnPoint";
    public const string GreenTankSpawnPoint = "GreenTankSpawnPoint";
    public const string PinkTankSpawnPoint = "PinkTankSpawnPoint";
    public const string PurpleTankSpawnPoint = "PurpleTankSpawnPoint";
    public const string WhiteTankSpawnPoint = "WhiteTankSpawnPoint";
    public const string BlackTankSpawnPoint = "BlackTankSpawnPoint";
    public const string EnemyTankObjectName = "Enemy Tanks";

    //Weapon Names
    public const string TagBomb = "Bomb";
    public const string TagBullet = "Bullet";
    public const string TagWalls = "Wall";

    //Private Variables
    static int m_SceneIndexNumber = -1;
    static bool m_HasBeenInitialized = false;
    private Vector3[] m_NonSpawingPositions = new Vector3[0];

    private void Start()
    {
        if (!m_HasBeenInitialized)
        {
            DontDestroyOnLoad(this);
            m_SceneIndexNumber = SceneManager.GetActiveScene().handle;
            m_HasBeenInitialized = true;
        }
    }

    private void Update()
    {
        if (m_SceneIndexNumber != SceneManager.GetActiveScene().handle && SceneManager.GetActiveScene().name != "Dead")
        {
            HasJustStartedAMission = true;
            ResetFunctionsEvenIfDead();

            //Initialisizing Tanks
            for (int i = 0; i < GameObject.Find(SpawnPointsName).transform.childCount; i++)
            {
                Transform tank = GameObject.Find(SpawnPointsName).transform.GetChild(i);
                tank.gameObject.SetActive(false);
                bool remove = false;
                for (int j = 0; j < m_NonSpawingPositions.Length; j++)
                {
                    if (tank.position == m_NonSpawingPositions[j])
                    {
                        remove = true;
                        break;
                    }
                }
                if (!remove)
                {
                    Tank temp = null;
                    //Initiate the desired tank
                    switch (tank.gameObject.name)
                    {
                        case PlayerTankSpawnPoint:
                            temp = Instantiate(I_PlayerTankPrefab, tank.position, tank.rotation, transform.parent);
                            break;
                        case BrownTankSpawnPoint:
                            temp = Instantiate(I_BrownTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case GreyTankSpawnPoint:
                            temp = Instantiate(I_GreyTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case TealTankSpawnPoint:
                            temp = Instantiate(I_TealTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case YellowTankSpawnPoint:
                            temp = Instantiate(I_YellowTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case GreenTankSpawnPoint:
                            temp = Instantiate(I_GreenTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case PinkTankSpawnPoint:
                            temp = Instantiate(I_PinkTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case PurpleTankSpawnPoint:
                            temp = Instantiate(I_PurlpeTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case WhiteTankSpawnPoint:
                            temp = Instantiate(I_WhiteTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                        case BlackTankSpawnPoint:
                            temp = Instantiate(I_BlackTankPrefab, tank.position, tank.rotation, GameObject.Find(EnemyTankObjectName).transform);
                            break;
                    }

                    if (temp != null)
                    {
                        temp.name = temp.name.Remove(temp.name.Length - 7);
                    }
                }
            }

            m_NonSpawingPositions = new Vector3[0];
        }
        else
            HasJustStartedAMission = false;

        if (SceneManager.GetActiveScene().name == "Dead" && m_SceneIndexNumber != SceneManager.GetActiveScene().handle)
        {
            ResetFunctionsEvenIfDead();
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
                        m_Bullets[i] = bullet[i];
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
    public void SetNonSpawingTankPos(Vector3 pos)
    {
        Vector3[] temp = m_NonSpawingPositions;
        m_NonSpawingPositions = new Vector3[temp.Length + 1];
        for (int i = 0; i < m_NonSpawingPositions.Length - 1; i++)
            m_NonSpawingPositions[i] = temp[i];

        m_NonSpawingPositions[m_NonSpawingPositions.Length - 1] = pos;
    }
    public void DestroyBullet(int numberInArray)
    {
        if (m_Bullets[numberInArray])
            Destroy(m_Bullets[numberInArray].gameObject);
        //Drop all the bullets back
        for (int i = numberInArray; i < m_Bullets.Length -1; i++)
        {
            m_Bullets[i] = m_Bullets[i + 1];
            m_Bullets[i].SetLevelInArray(i);
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
            m_Bombs[i].SetLevelInArray(i);
        }
        m_CurrentBombIndex--;
    }

    //Getters
    static public Rigidbody2D GetPlayerTankBody()
    {
        if (GameObject.Find(GlobalVariables.PlayerTankBodyName))
            return GameObject.Find(GlobalVariables.PlayerTankBodyName).GetComponent<Rigidbody2D>();
        return null;
    }
    static public GlobalVariables GetThisInstance() { return GameObject.Find(GlobalVariables.GlobalVariableObjectName).GetComponent<GlobalVariables>(); }
    public Vector2 GetCamerBoundsBottomLeft() { return I_CameraBounds.bounds.center - I_CameraBounds.bounds.size / 2; }
    public Vector2 GetCamerBoundsTopRight() { return I_CameraBounds.bounds.center + I_CameraBounds.bounds.size / 2; }
    public Vector2 GetCameraCenter() { return I_CameraBounds.bounds.center; }
    public Bounds GetCameraBounds() { return I_CameraBounds.bounds; }

    private void ResetFunctionsEvenIfDead()
    {
        GetComponentInChildren<TankSceneManager>().I_IsLoading = false;
        m_SceneIndexNumber = SceneManager.GetActiveScene().handle;

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
    }
}
