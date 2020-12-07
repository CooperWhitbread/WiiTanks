using UnityEngine;

public class EnemyTank : Tank
{
    ///Inspector Variables
    [SerializeField] protected float I_TurretRotationSpeed = 1.0f;
    [SerializeField] protected float I_MinTimeToShoot = 1.0f;
    [SerializeField] protected float I_MaxTimeToShoot = 1.0f;
    [SerializeField] protected float I_MiniIntervalDelay = 1.0f;

    ///Protected Variables
    protected float m_TimeAtNextShoot = 0.0f;
    protected float m_MiniTimeForNextShoot = 0.0f;
    protected bool m_HasShot = true;
    protected int m_CurrentWayPath = 0;
    protected bool m_ReachedEndOfPath = false;

    ///Inherited Function
    protected virtual bool CanShoot() { return BasicCheckForOkShootHit(); }

    ///Protected Functions
    protected void SetNextShootTime(float min, float max)
    {
        m_TimeAtNextShoot = Time.unscaledTime + Random.Range(min, max);
        m_HasShot = false;
    }
    protected void AutomaticShoot()
    {
        if (!m_HasShot)
        {
            if (Time.unscaledTime >= m_TimeAtNextShoot)
            {
                if (Time.fixedTime >= m_MiniTimeForNextShoot)
                {
                    if (CanShoot())
                    {
                        //Time to shoot
                        Shoot();
                        m_HasShot = true;
                        DelayShoot();
                    }
                    m_MiniTimeForNextShoot = Time.fixedTime + I_MiniIntervalDelay;
                }
            }
        }
    }
    protected bool BasicCheckForOkShootHit()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(I_TurretRB2D.rotation),
            30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
        if (rayCastHit.collider != null)
        {
            if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName || rayCastHit.collider.gameObject.layer == GlobalVariables.LayerBullets)
                return true;
            else if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
            {
                Vector2 post = GetVector2FromAngle(I_TurretRB2D.rotation); //Origion Direction
                Vector2 normal = rayCastHit.normal; //Wall's normal
                Vector2 ang = post - (2 * Vector2.Dot(post, normal) * normal); //vector of desired direction

                rayCastHit = Physics2D.Raycast(rayCastHit.point, ang,
                    30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);
                if (rayCastHit.collider != null)
                {
                    if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankName ||
                        rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
                        return true;

                    return false;
                }
                return true;
            }
            return false;
        }
        return true;
    }
}