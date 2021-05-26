using UnityEngine;
using System.Collections.Generic;

public class EnemyTank : Tank
{
    ///Inspector Variables
    [SerializeField] protected StateManager I_StateManager = new StateManager();
    [SerializeField] protected float I_TurretRotationSpeed = 36.0f;
    [Header("Shooting variables")]
    [SerializeField] protected float I_MinTimeToShoot = 6.0f;
    [SerializeField] protected float I_MaxTimeToShoot = 8.0f;
    [SerializeField] protected float I_MiniIntervalDelay = 0.3f;
    [SerializeField] protected float I_TurretTargetVariationMax = 20.0f;
    [SerializeField] protected float I_MaxDistanceForSeeingBullet = 5.0f;
    [SerializeField] protected float I_MaxShootDistanceForPlayerTank = 20.0f;
    [SerializeField] protected float I_VisualRangeAngle = 30.0f;

    ///Protected Variables
    protected float m_TimeAtNextShoot = 0.0f;
    protected float m_MiniTimeForNextShoot = 0.0f;
    protected bool m_HasShot = true;
    protected int m_CurrentWayPath = 0;
    protected bool m_ReachedEndOfPath = false;
    protected Rigidbody2D m_PlayerRB2D;
    protected Vector3[] m_Path;

    protected float m_DesiredTurretRotation = 0.0f;
    protected int m_DelayTurretUpdate = 0;

    protected bool m_IsCheckingPath = false;
    protected int m_TargetIndex = 0;
    protected Vector3 m_CurrentWayPoint;

    /// Private Variables
    private float m_TimeBeforeRecalculatigPath = 0.0f; 

    private const float m_SecondsForUpdateTargetTracking = 3.0f;
    private const int m_MaxTurretUpdateDelay = 20;

    ///Constant Variables
    protected const float m_SpeedForGradualChangeVelocity = 2.0f;
    protected const float m_SpeedForGradualChangeVelocityStationary = 3.0f;

    ///Inherited Function
    protected virtual bool CanShoot() { return BasicCheckForOkShootHit(); }
    protected virtual void InheritedStartEnemy() { }
    protected virtual void InheritedFixedUpdateEnemy() { }
    protected virtual void FollowPathEnemy() { }
    protected virtual void RecalculatePath() { }

    /// Overridden Functions
    protected override void InheritedStart()
    {
        m_PlayerRB2D = GlobalVariables.GetPlayerTankBody();

        InheritedStartEnemy();

        RecalculatePath();
    }
    protected override void InheritedFixedUpdate()
    {
        //Path Finding
        if (m_TimeBeforeRecalculatigPath <= Time.unscaledTime)
            RecalculatePath();

        InheritedFixedUpdateEnemy();

        FollowPath();
        UpdateTurret();

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);

        I_StateManager.Update(Time.unscaledTime);
    }

    /// Protected Functions
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
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(m_TurretRB2D.rotation),
            30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
        if (rayCastHit.collider != null)
        {
            if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName || rayCastHit.collider.gameObject.layer == GlobalVariables.LayerBullets)
                return true;
            else if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
            {
                Vector2 post = GetVector2FromAngle(m_TurretRB2D.rotation); //Origion Direction
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
    protected void SetNextTimeForRecalculatingPath()
    {
        m_TimeBeforeRecalculatigPath = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
    }
    protected void OnPathFound(Vector3[] newPath, bool pathSucess)
    {
        m_IsCheckingPath = false;
        if (pathSucess)
        {
            m_Path = newPath;
            m_CurrentWayPoint = m_Path[0];
            m_TargetIndex = 0;
        }
    }
    protected Bullet[] GetBulletsThatAreVisable()
    {
        List<Bullet> bulletsCanSee = new List<Bullet>();
        foreach (GameObject gob in GameObject.FindGameObjectsWithTag(GlobalVariables.TagBullet))
        {
            Rigidbody2D bulletRB2D = gob.GetComponent<Rigidbody2D>();
            if (Vector2.Distance(m_BodyRB2D.position, bulletRB2D.position) <= I_MaxDistanceForSeeingBullet)
            {
                RaycastHit2D hit = Physics2D.Raycast(I_ShootTransform.position, bulletRB2D.position - (Vector2)I_ShootTransform.position, I_MaxDistanceForSeeingBullet + 1,
                    1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
                if (hit.collider != null)
                {
                    if (hit.collider.gameObject == gob)
                    {
                        if (Mathf.Abs(GetAngleFromVector2(bulletRB2D.position - m_BodyRB2D.position) - m_TurretRB2D.rotation) <= I_VisualRangeAngle)
                        {
                            bulletsCanSee.Add(gob.GetComponent<Bullet>());
                        }
                    }
                }
            }
        }

        return bulletsCanSee.ToArray();
    }

    /// Private Functions
    private void FollowPath()
    {
        if (m_Path != null)
        {
            FollowPathEnemy();
        }
        else
            RecalculatePath();
    }
    private void UpdateTurret()
    {
        GradualRotation(ref m_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);
        if (Mathf.Abs(m_TurretRB2D.rotation - m_DesiredTurretRotation + GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position)) < 0.5f)
        {
            m_DelayTurretUpdate++;
            if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
            {
                m_DesiredTurretRotation = Random.Range(-I_TurretTargetVariationMax, I_TurretTargetVariationMax);
                if (m_DesiredTurretRotation > 180)
                    m_DesiredTurretRotation -= 360;
                else if (m_DesiredTurretRotation < -180)
                    m_DesiredTurretRotation += 360;
                m_DelayTurretUpdate = 0;
            }
        }
    }
}