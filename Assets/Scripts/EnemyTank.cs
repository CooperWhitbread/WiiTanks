using UnityEngine;
using Pathfinding;

public class EnemyTank : Tank
{
    ///Inspector Variables
    [SerializeField] protected float I_TurretRotationSpeed = 1.0f;
    [SerializeField] protected float I_MinTimeToShoot = 1.0f;
    [SerializeField] protected float I_MaxTimeToShoot = 1.0f;

    ///Protected Variables
    protected float m_TimeAtNextShoot = 0.0f;
    protected bool m_HasShot = true;
    protected Path m_MovePath;
    protected int m_CurrentWayPath = 0;
    protected bool m_ReachedEndOfPath = false;
    protected Seeker m_Seeker;

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
                if (CheckForOkShootHit())
                {
                    //Time to shoot
                    Shoot();
                    m_HasShot = true;
                    DelayShoot();
                }
            }
        }
    }
}