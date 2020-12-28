using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinkTank : EnemyTank
{

    ///Inspector Variables
    [SerializeField] float I_MaxDistanceForSeeingBullet = 5.0f;
    [SerializeField] float I_VisualRangeAngle = 30.0f;
    [SerializeField] float I_MovementDistanceFromBulletScalarValue = 3.0f;
    [SerializeField] float I_MovementCurrentDirectionScalar = 1.0f;
    [SerializeField] float I_BombDetectionRadius = 2.5f;
    [SerializeField] float I_MinDistBeforeMoveOnForRetreatingTank = 1.0f;
    [SerializeField] float I_MaxDistancebeforeCanReturnMovement = 7.0f;
    [SerializeField] float I_DistanceBetweenTankScalar = 1.0f;
    [SerializeField] float I_RandomDistanceAddedValue = 3.0f;
    [SerializeField] float I_AngleForHardTurn = 120.0f;

    ///Private Variables
    private float m_ResetTimeForMove = 0.0f;
    private float m_SecondsForUpdateTargetTracking = 3.0f;
    private Rigidbody2D m_PlayerRB2D;
    private Vector2 m_CurrentRetreatPos = Vector2.zero;
    private Vector2 m_PreviousPosition = Vector2.zero;
    private Vector2[] m_Corners = new Vector2[4];
    private bool m_IsCheckingPath = false;
    private Vector2 m_PreviousTargetPos = Vector2.zero;
    private bool m_WantToCheckAllCornersAgain = true;
    private float m_TimeForNextDistanceCheck = 0.0f;

    Vector2 debugDirectionPos = Vector2.zero;

    Vector3[] m_Path;
    private int m_TargetIndex = 0;
    Vector3 m_CurrentWayPoint;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        m_PlayerRB2D = GlobalVariables.GetPlayerTankBody();
        m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        m_Bullets = new Bullet[1];
        m_Bombs = new Bomb[0];
        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_PlayerRB2D.position, OnPathFound));


        Vector2 p0 = GlobalVariables.GetThisInstance().GetCamerBoundsBottomLeft();
        Vector2 p1 = GlobalVariables.GetThisInstance().GetCamerBoundsTopRight();
        m_Corners[0] = new Vector2(p0.x + 2.5f, p1.y - 2.5f); //Top left
        m_Corners[1] = p1 + new Vector2(-2.5f, -2.5f); // Top right
        m_Corners[2] = new Vector2(p1.x - 2.5f, p0.y + 2.5f); // Bottom Right
        m_Corners[3] = p0 + new Vector2(2.5f, 2.5f);// Bottom Left

        SetNextShootTime(0.2f, 0.2f);
    }
    protected override void InheritedFixedUpdate()
    {
        //Path Finding
        if (m_ResetTimeForMove <= Time.unscaledTime)
            //UpdatePath();

        if (Vector2.Distance(m_PlayerRB2D.position, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank)
        {
            UpdatePath();
        }

        FollowPath();
        UpdateTurret();

        if (Time.fixedTime >= m_TimeForNextDistanceCheck)
        {
            m_TimeForNextDistanceCheck = Time.fixedTime + 4.0f;
            m_PreviousPosition = m_BodyRB2D.position;
            if (Vector2.Distance(m_BodyRB2D.position, m_PreviousPosition) <= 0.2f)
            {
                m_WantToCheckAllCornersAgain = true;
                UpdatePath();
            }

        }

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);

        if (Vector2.Distance(m_CurrentRetreatPos, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank)
            m_WantToCheckAllCornersAgain = true;
    }
    protected override void InheritedOnCollisionEnter(Collision2D collision)
    {
        m_WantToCheckAllCornersAgain = true;
        UpdatePath();
    }

    ///Private Functions
    private void UpdateTurret()
    {
        GradualRotation(ref m_TurretRB2D, GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position), I_TurretRotationSpeed);
    }
    protected override bool CanShoot()
    {
        //Check if it is in a position that it wants to shoot
        CapsuleCollider2D ColiderInfo = GlobalVariables.GetThisInstance().I_BulletPrefab.GetComponent<CapsuleCollider2D>();
        RaycastHit2D rayCastHit = Physics2D.CapsuleCast(I_ShootTransform.position, ColiderInfo.size, ColiderInfo.direction, 
            m_TurretRB2D.rotation, GetVector2FromAngle(m_TurretRB2D.rotation), 100.0f, 
            1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);

        //Make sure nothing is too close to the tank 
        RaycastHit2D rayCastHitShort1 = Physics2D.Raycast(m_BodyRB2D.position, 
            GetVector2FromAngle(m_TurretRB2D.rotation + 4.0f), 2.0f,
            1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);
        RaycastHit2D rayCastHitShort2 = Physics2D.Raycast(m_BodyRB2D.position, 
            GetVector2FromAngle(m_TurretRB2D.rotation - 4.0f), 2.0f,
            1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);

        if (rayCastHit.collider != null && rayCastHitShort1.collider == null && rayCastHitShort2.collider == null)
        {
            if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                return true;
            else if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerBullets && Vector2.Distance(rayCastHit.point, m_BodyRB2D.position) <= 2.0f)
                return true;
        }
        return false; //Diddn't hit player tank
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
                    Gizmos.DrawLine(m_Path[i - 1], m_Path[i]);
                }
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawCube(m_CurrentWayPoint, Vector3.one / 2);

        //Gizmos.DrawLine(debugDirectionPos * 5 + m_BodyRB2D.position, m_BodyRB2D.position);
    }
    public void OnPathFound(Vector3[] newPath, bool pathSucess)
    {
        m_IsCheckingPath = false;
        if (pathSucess)
        {
            m_Path = newPath;
            m_CurrentWayPoint = m_BodyRB2D.position;
            m_TargetIndex = 0;
        }
    }
    private void FollowPath()
    {
        if (m_Path != null)
        {
            if (Vector3.Distance(m_BodyRB2D.position, m_CurrentWayPoint) <= 0.80f)
            {
                if (m_TargetIndex + 1 >= m_Path.Length)
                {
                    m_CurrentWayPoint = m_BodyRB2D.velocity * 5 + m_BodyRB2D.position;
                    UpdatePath();
                    return;
                }

                //more swery path following
                if (Vector3.Distance(m_Path[m_TargetIndex + 1], m_CurrentWayPoint) >= 3.0f)
                {
                    //Add swerve
                    while (true)
                    {
                        float ang = GetAngleFromVector3(m_Path[m_TargetIndex + 1] - (Vector3)m_BodyRB2D.position);
                        ang += Random.Range(40.0f, 80.0f) * (Random.Range(0, 2) * 2 - 1);
                        Vector2 theDirection = GetVector2FromAngle(ang);
                        RaycastHit2D rch = Physics2D.Raycast(m_BodyRB2D.position, theDirection, 2.0f,
                            1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerWallHole);
                        if (rch.collider == null)
                        {
                            m_CurrentWayPoint += (Vector3)theDirection * 2.0f;
                            break;
                        }
                    }
                }
                else
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
            foreach (Tank i in GameObject.FindObjectsOfType<Tank>())
            {
                if (i.gameObject != gameObject && i.gameObject != GlobalVariables.GetPlayerTankBody().transform.parent.gameObject)
                {
                    direction += (m_BodyRB2D.position - i.GetComponentInChildren<Rigidbody2D>().position).normalized /
                        Mathf.Pow(Vector2.Distance(m_BodyRB2D.position, i.GetComponentInChildren<Rigidbody2D>().position), 1) *
                        I_DistanceBetweenTankScalar;
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
                GradualMoveTank(direction, m_SpeedForGradualChangeVelocity, I_AngleForHardTurn, m_SpeedForGradualChangeVelocityStationary);
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
            if (Random.Range(0, 2) == 1 || !m_WantToCheckAllCornersAgain)
                m_WantToCheckAllCornersAgain = false;

            if (Vector2.Distance(m_CurrentRetreatPos, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank || 
                Vector2.Distance(m_PreviousTargetPos, m_BodyRB2D.position) >= I_MaxDistancebeforeCanReturnMovement && m_WantToCheckAllCornersAgain)
            {
                m_WantToCheckAllCornersAgain = false;
                m_PreviousTargetPos = m_CurrentRetreatPos;
                Vector2 pos = Vector2.zero;
                float closestDiff = -500.0f;

                for (int i = 0; i < 4; i++)
                {
                    //Get the current corner
                    if (m_CurrentRetreatPos != m_Corners[i])
                    {
                        float diff = Vector2.Distance(m_Corners[i], m_PlayerRB2D.position) - Vector2.Distance(m_Corners[i], m_BodyRB2D.position) + 
                            Random.Range(-I_RandomDistanceAddedValue, I_RandomDistanceAddedValue);
                        if (diff > closestDiff)
                        {
                            pos = m_Corners[i];
                            closestDiff = diff;
                        }
                    }
                }
                m_CurrentRetreatPos = pos;
            }

            PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_CurrentRetreatPos, OnPathFound));
            m_IsCheckingPath = true;
        }
    }
}
