using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GreyTank : EnemyTank
{
    
    ///Inspector Variables
    [SerializeField] StateManager I_StateManager = new StateManager();
    [SerializeField] float I_MaxShootDistanceForPlayerTank = 30.0f;
    [SerializeField] float I_TurretTargetVariationMax = 20.0f;
    [SerializeField] float I_MaxDistanceForSeeingBullet = 5.0f;
    [SerializeField] float I_VisualRangeAngle = 30.0f;
    [SerializeField] float I_MovementDistanceFromBulletScalarValue = 3.0f;
    [SerializeField] float I_MovementCurrentDirectionScalar = 1.0f;
    [SerializeField] float I_BombDetectionRadius = 2.5f;
    [SerializeField] float I_MinDistBeforeMoveOnForRetreatingTank = 1.0f;

    ///Private Variables
    private float m_ResetTimeForMove = 0.0f;
    private float m_SecondsForUpdateTargetTracking = 3.0f;
    private float m_DesiredTurretRotation = 0.0f;
    private int m_DelayTurretUpdate = 0;
    private int m_MaxTurretUpdateDelay = 20;
    private Rigidbody2D m_PlayerRB2D;
    private Vector2 m_CurrentRetreatPos = Vector2.zero;
    private Vector2[] m_Corners = new Vector2[4];
    private bool m_IsCheckingPath = false;

    Vector2 debugDirectionPos = Vector2.zero;

    Vector3[] m_Path;
    private int m_TargetIndex = 0;
    Vector3 m_CurrentWayPoint;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        m_PlayerRB2D = GlobalVariables.GetPlayerTankBody();
        m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        I_StateManager.Start(Time.unscaledTime);
        m_Bullets = new Bullet[1];
        m_Bombs = new Bomb[0];
        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_PlayerRB2D.position, OnPathFound));


        Vector2 p0 = GlobalVariables.GetThisInstance().GetCamerBoundsBottomLeft();
        Vector2 p1 = GlobalVariables.GetThisInstance().GetCamerBoundsTopRight();
        m_Corners[0] = new Vector2(p0.x + 2.5f, p1.y - 2.5f); //Top left
        m_Corners[1] = p1 + new Vector2(-2.5f, -2.5f); // Top right
        m_Corners[2] = new Vector2(p1.x - 2.5f, p0.y + 2.5f); // Bottom Right
        m_Corners[3] = p0 + new Vector2(2.5f, 2.5f);// Bottom Left
    }
    protected override void InheritedFixedUpdate()
    {
        //Path Finding
        if (m_ResetTimeForMove <= Time.unscaledTime)
            UpdatePath();

        if (Vector2.Distance(m_PlayerRB2D.position, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank)
        {
            I_StateManager.SwitchStateTo(StateManager.State.Escape, Time.unscaledTime);
            UpdatePath();
        }

        FollowPath();
        UpdateTurret();

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);

        I_StateManager.Update(Time.unscaledTime);
    }
    protected override void InheritedOnCollisionEnter(Collision2D collision)
    {
        UpdatePath();
    }

    ///Private Functions
    private void UpdateTurret()
    {
        GradualRotation(ref m_TurretRB2D, m_DesiredTurretRotation + GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position), I_TurretRotationSpeed);
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
    protected override bool CanShoot()
    {
        //Basic Raycast for hitting self check before advanced check
        if (CheckIfGoingToHitSelf(m_Bullets[0].GetComponent<CapsuleCollider2D>()))
            return false;

        //Check if it is in a position that it wants to shoot
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(m_TurretRB2D.rotation),
               100.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
        if (rayCastHit.collider != null)
        {
            switch (rayCastHit.collider.gameObject.layer)
            {
                case GlobalVariables.LayerBullets:
                    if (rayCastHit.collider.gameObject.name.Contains("Bomb"))
                        return false; //Is a bomb
                    if (Vector2.Distance(rayCastHit.point, m_BodyRB2D.position) <= 1.0f)
                        return true; //Is a bullet
                    break;

                case GlobalVariables.LayerTanks:
                    if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                        return true; // looking at player tank
                    return false; //looking at an ally tank

                case GlobalVariables.LayerWalls:
                    //Check Near Player
                    if (GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position) - m_TurretRB2D.rotation <= I_MaxShootDistanceForPlayerTank &&
                        GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position) - m_TurretRB2D.rotation >= -I_MaxShootDistanceForPlayerTank)
                    {
                        RaycastHit2D rch = Physics2D.Raycast(I_ShootTransform.position, m_PlayerRB2D.position - m_BodyRB2D.position);
                        if (rch.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                            return true;
                    }

                    //Check for the rebound hit
                    Vector2 post = GetVector2FromAngle(m_TurretRB2D.rotation); //Origion Direction
                    Vector2 normal = rayCastHit.normal; //Wall's normal
                    Vector2 ang = post - (2 * Vector2.Dot(post, normal) * normal); //vector of desired direction
                    Vector2 hit = rayCastHit.point; // the point of contact
                    rayCastHit = Physics2D.Raycast(hit, ang,
                        100.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);//Don't do bullets since they will have moved by then

                    if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerTanks)
                    {
                        if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                            return true; // looking at player tank
                        return false; //looking at an ally tank
                    }

                    //Check for near player tank
                    //if (Vector2.Angle(I_PlayerRB.position - hit, ang) <= I_MaxShootDistanceForPlayerTank)
                    if (GetAngleFromVector2(m_PlayerRB2D.position - hit) - GetAngleFromVector2(ang) <= I_MaxShootDistanceForPlayerTank &&
                        GetAngleFromVector2(m_PlayerRB2D.position - hit) - GetAngleFromVector2(ang) >= -I_MaxShootDistanceForPlayerTank)
                    {
                        if (Vector2.SignedAngle(m_PlayerRB2D.position - hit, rayCastHit.normal) >= 0 && Vector2.SignedAngle(ang, rayCastHit.normal) >= 0 ||
                            Vector2.SignedAngle(m_PlayerRB2D.position - hit, rayCastHit.normal) <= 0 && Vector2.SignedAngle(ang, rayCastHit.normal) <= 0)
                        {
                            RaycastHit2D rch = Physics2D.Raycast(I_ShootTransform.position, m_PlayerRB2D.position - hit);
                            if (rch.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                                return true;
                        }
                    }

                    break;
            }
            return false;//Hit things but nothing important
        }
        return true; //hit nothing
    }
    public void OnDrawGizmos()
    {
        //Gizmos.DrawLine(m_BodyRB2D.position, GetVector2FromAngle(m_DesiredTurretRotation) * 100);
        if (m_Path != null)
        {
            for (int i = m_TargetIndex; i < m_Path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(m_Path[i], Vector3.one / 4);

                if (i == m_TargetIndex)
                {
                    Gizmos.DrawLine(m_BodyRB2D.position, m_Path[i]);
                }
                else
                {
                    Gizmos.DrawLine(m_Path[i-1], m_Path[i]);
                }
            }
        }

        //Gizmos.DrawLine(debugDirectionPos * 5 + m_BodyRB2D.position, m_BodyRB2D.position);
    }
    public void OnPathFound(Vector3[] newPath, bool pathSucess)
    {
        m_IsCheckingPath = false;
        if (pathSucess)
        {
            m_Path = newPath;
            m_CurrentWayPoint = m_Path[0];
            m_TargetIndex = 0;
        }
    }
    private void FollowPath()
    {
        if (m_Path != null)
        {
            if (Vector3.Distance(m_BodyRB2D.position, m_CurrentWayPoint) <= 0.40f)
            {
                if (m_TargetIndex + 1 >= m_Path.Length)
                {
                    m_CurrentWayPoint = m_BodyRB2D.velocity * 5 + m_BodyRB2D.position;
                    UpdatePath();
                    return;
                }

                m_CurrentWayPoint = m_Path[++m_TargetIndex];
            }

            //Get the bullets
            Bullet[] bullets = GetBulletsThatAreVisable();
            Vector2 direction = ((Vector2)m_CurrentWayPoint - m_BodyRB2D.position) * I_MovementCurrentDirectionScalar;

            foreach (Bullet b in bullets)
            {
                Rigidbody2D rb = b.GetComponent<Rigidbody2D>();

                //Check bullet is moving towards tank
                if (Mathf.Abs(GetAngleFromVector2(rb.velocity) - GetAngleFromVector2(m_BodyRB2D.position - rb.position)) < 90.0f)
                {
                    float distance = Vector2.Distance(b.transform.position, m_BodyRB2D.position);
                    Vector2 normal;
                    if (Vector2.SignedAngle(rb.velocity, m_BodyRB2D.position - rb.position) <= 0)
                        normal = new Vector2(rb.velocity.normalized.y, -rb.velocity.normalized.x);
                    else
                        normal = new Vector2(-rb.velocity.normalized.y, rb.velocity.normalized.x);

                    direction += normal / distance * I_MovementDistanceFromBulletScalarValue;
                    debugDirectionPos = normal;
                }
            }
            direction = direction.normalized;

            //Bomb
            Bomb[] bombs = GameObject.FindObjectsOfType<Bomb>();
            bool tempNeedsMovement = true;
            foreach (Bomb b in bombs)
            {
                if (Vector2.Distance(m_BodyRB2D.position, b.transform.position) <= I_BombDetectionRadius)
                {
                    m_BodyRB2D.velocity = (m_BodyRB2D.position - (Vector2)b.transform.position) * I_MoveSpeed;
                    GradualRotation(ref m_TurretRB2D, GetAngleFromVector2(m_BodyRB2D.position - (Vector2)b.transform.position), I_TurretRotationSpeed);
                    tempNeedsMovement = false;
                    UpdatePath();
                    if (m_TargetIndex != 0)
                        m_CurrentWayPoint = m_Path[--m_TargetIndex];
                    else
                        m_CurrentWayPoint = m_BodyRB2D.velocity * 5 + m_BodyRB2D.position;
                }
            }
            if (tempNeedsMovement)
                GradualMoveTank(direction, m_SpeedForGradualChangeVelocity, 120.0f, m_SpeedForGradualChangeVelocityStationary);
        }
        else
            UpdatePath();
    }
    private Bullet[] GetBulletsThatAreVisable()
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
    private void UpdatePath()
    {
        if (!m_IsCheckingPath)
        {
            switch (I_StateManager.M_CurrentState)
            {
                case StateManager.State.Attack:
                    PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_PlayerRB2D.position, OnPathFound));
                    m_CurrentRetreatPos = Vector2.zero;
                    break;
                case StateManager.State.Escape:

                    if (m_CurrentRetreatPos == Vector2.zero)
                    {
                        Vector2 pos = Vector2.zero;
                        float closestDiff = 0.0f;

                        for (int i = 0; i < 4; i++)
                        {
                            float diff = Vector2.Distance(m_Corners[i], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[i], m_BodyRB2D.position);
                            if (diff > closestDiff)
                            {
                                pos = m_Corners[i];
                                closestDiff = diff;
                            }
                        }
                        m_CurrentRetreatPos = pos;
                    }
                    //Check if new pos needs to be identified
                    else if (Vector2.Distance(m_CurrentRetreatPos, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            //Get the current corner
                            if (m_CurrentRetreatPos == m_Corners[i])
                            {
                                //See which of the adjasent corners is more ideal to go to
                                float diff = 0;
                                if (i > 0)
                                {
                                    diff = Vector2.Distance(m_Corners[i - 1], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[i - 1], m_BodyRB2D.position);
                                    m_CurrentRetreatPos = m_Corners[i - 1];
                                }
                                else
                                {
                                    diff = Vector2.Distance(m_Corners[3], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[3], m_BodyRB2D.position);
                                    m_CurrentRetreatPos = m_Corners[3];
                                }

                                if (i < 3)
                                {
                                    if (diff < Vector2.Distance(m_Corners[i + 1], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[i + 1], m_BodyRB2D.position))
                                    {
                                        m_CurrentRetreatPos = m_Corners[i + 1];
                                    }
                                }
                                else
                                {
                                    if (diff < Vector2.Distance(m_Corners[0], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[0], m_BodyRB2D.position))
                                    {
                                        m_CurrentRetreatPos = m_Corners[0];
                                    }
                                }

                            }
                        }
                    }

                    PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_CurrentRetreatPos, OnPathFound));
                    m_IsCheckingPath = true;

                    break;
            }
            m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        }
    }
}