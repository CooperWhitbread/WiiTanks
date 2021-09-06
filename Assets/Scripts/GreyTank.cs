using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GreyTank : EnemyTank
{

    ///Inspector Variables
    [Header("Grey Tank AI Variables")]
    //[SerializeField] private float I_MovementCurrentDirectionScalar = 1.0f;
    [SerializeField] private float I_BombDetectionRadius = 3.0f;
    [SerializeField] private float I_MinDistBeforeMoveOnForRetreatingTank = 3.0f;

    ///Private Variables
    private Vector2 m_CurrentRetreatPos = Vector2.zero;
    private Rigidbody2D m_BulletClose;
    private float m_Velocity = 0.0f;
    
    ///Virtual Functions
    protected override void InheritedStartEnemy()
    {
        I_StateManager.Start(Time.unscaledTime);
        InitializeBulletsAndBombs(1, 0);
        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_PlayerRB2D.position, OnPathFound));
        m_Velocity = I_MoveSpeed;
    }
    protected override void InheritedFixedUpdateEnemy()
    {
        m_DesiredTurretRotation = GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position);

        if (Vector2.Distance(m_PlayerRB2D.position, m_BodyRB2D.position) <= I_MinDistBeforeMoveOnForRetreatingTank)
        {
            I_StateManager.SwitchStateTo(StateManager.State.Escape, Time.unscaledTime);
            RecalculatePath();
        }
    }
    protected override void InheritedOnCollisionEnter(Collision2D collision)
    {
        //TODO
        //RecalculatePath();
    }
    protected override bool CanShoot()
    {
        return GenericCanShoot();
    }
    protected override void FollowPathEnemy()
    {
        //Move away from tank if too close
        if (Vector3.Distance(m_BodyRB2D.position, m_CurrentWayPoint) <= 0.70f)
        {
            if (m_TargetIndex + 1 >= m_Path.Length)
            {
                m_CurrentWayPoint = m_BodyRB2D.velocity * 5 + m_BodyRB2D.position;
                RecalculatePath();
                return;
            }

            m_CurrentWayPoint = m_Path[++m_TargetIndex];
        }

        //Bomb
        Bomb[] bombs = FindObjectsOfType<Bomb>();
        bool tempNeedsMovement = true;
        foreach (Bomb b in bombs)
        {
            if (Vector2.Distance(m_BodyRB2D.position, b.transform.position) <= I_BombDetectionRadius)
            {
                m_BodyRB2D.velocity = (m_BodyRB2D.position - (Vector2)b.transform.position) * I_MoveSpeed;
                GradualRotation(ref m_TurretRB2D, GetAngleFromVector2(m_BodyRB2D.position - (Vector2)b.transform.position), I_TurretRotationSpeed);
                tempNeedsMovement = false;
                RecalculatePath();
                if (m_TargetIndex != 0)
                    m_CurrentWayPoint = m_Path[--m_TargetIndex];
                else
                    m_CurrentWayPoint = m_BodyRB2D.velocity * 5 + m_BodyRB2D.position;
            }
        }

        //Get the bullets
        Vector2 direction = ((Vector2)m_CurrentWayPoint - m_BodyRB2D.position);
        direction = direction.normalized * m_Velocity;
        int bTag = 0;
        if (m_BulletClose)
            bTag = m_BulletClose.GetInstanceID();
        Bullet[] bullets = GetBulletsThatAreVisable();

        ///To Do: remove bullets once disapeared
        ///Reset moveSpeed
        ///Array into list

        float distance = Mathf.Infinity;
        if (m_BulletClose)
            Vector2.Distance(m_BulletClose.GetComponent<Rigidbody2D>().position, m_BodyRB2D.position);
        foreach (Bullet b in bullets)
        {
            float d = Vector2.Distance(b.transform.GetComponent<Rigidbody2D>().position, m_BodyRB2D.position);
            if (d < distance)
            {
                distance = d;
                m_BulletClose = b.transform.GetComponent<Rigidbody2D>();
            }
        }
        if (m_BulletClose && bTag != m_BulletClose.GetInstanceID())
        {
            if (distance < 2.5f)
            {
                float colliderBuffer = 0.15f + m_BodyRB2D.transform.GetComponent<CapsuleCollider2D>().size.x / 2.0f;
                Vector2 tankVel = (m_BodyRB2D.position - m_PreviousPos) / Time.deltaTime;
                Vector2 cp = GetPositionBetweenTwoLines(tankVel, m_BodyRB2D.position, m_BulletClose.velocity, m_BulletClose.position);

                Vector2 bulletPos1 = cp + m_BulletClose.velocity.normalized * colliderBuffer;
                Vector2 bulletPos2 = cp - m_BulletClose.velocity.normalized * colliderBuffer;

                float time1 = Vector2.Distance(bulletPos1, m_BulletClose.position) / m_BulletClose.velocity.magnitude;
                float time2 = Vector2.Distance(bulletPos2, m_BulletClose.position) / m_BulletClose.velocity.magnitude;
                if (time2 >= 0.0f)
                {
                    Vector2 tankPosTime1 = tankVel * time1 + m_BodyRB2D.position;
                    Vector2 tankPosTime2 = tankVel * time1 + m_BodyRB2D.position;

                    float disTime1 = Vector2.Distance(tankPosTime1, bulletPos1) - colliderBuffer;
                    float disTime2 = Vector2.Distance(tankPosTime2, bulletPos2) - colliderBuffer;
                    if (disTime1 < 0.0f || disTime2 < 0.0f) //Check if going to collide
                    {
                        Vector2 desiredBulletPos = bulletPos1 + m_BulletClose.velocity.normalized * 0.4f;
                        float desiredTime = Vector2.Distance(desiredBulletPos, m_BulletClose.position) / m_BulletClose.velocity.magnitude;

                        float distToMove = Vector2.Distance(desiredBulletPos, m_BodyRB2D.position);

                        direction = tankVel.normalized * distToMove / desiredTime;

                        //Check it is an appropriate speed
                        if (direction.magnitude > I_MoveSpeed || direction.magnitude <= 0.0f )
                            direction *= I_MoveSpeed / direction.magnitude;
                    }
                }
            }
        }

        if (tempNeedsMovement)
            GradualMoveTankAutoIfStationary(direction.normalized, I_SpeedForGradualChangeVelocity, direction.magnitude, I_AngleForHardTurn, I_SpeedForGradualChangeVelocityStationary);
    }
    protected override void RecalculatePath()
    {
        if (!m_IsCheckingPath)
        {
            switch (I_StateManager.CurrentState)
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
                            float diff = Vector2.Distance(m_BattleGroundCorners[i], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[i], m_BodyRB2D.position);
                            if (diff > closestDiff)
                            {
                                pos = m_BattleGroundCorners[i];
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
                            if (m_CurrentRetreatPos == m_BattleGroundCorners[i])
                            {
                                //See which of the adjasent corners is more ideal to go to
                                float diff = 0;
                                if (i > 0)
                                {
                                    diff = Vector2.Distance(m_BattleGroundCorners[i - 1], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[i - 1], m_BodyRB2D.position);
                                    m_CurrentRetreatPos = m_BattleGroundCorners[i - 1];
                                }
                                else
                                {
                                    diff = Vector2.Distance(m_BattleGroundCorners[3], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[3], m_BodyRB2D.position);
                                    m_CurrentRetreatPos = m_BattleGroundCorners[3];
                                }

                                if (i < 3)
                                {
                                    if (diff < Vector2.Distance(m_BattleGroundCorners[i + 1], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[i + 1], m_BodyRB2D.position))
                                    {
                                        m_CurrentRetreatPos = m_BattleGroundCorners[i + 1];
                                    }
                                }
                                else
                                {
                                    if (diff < Vector2.Distance(m_BattleGroundCorners[0], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[0], m_BodyRB2D.position))
                                    {
                                        m_CurrentRetreatPos = m_BattleGroundCorners[0];
                                    }
                                }

                            }
                        }
                    }

                    PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_CurrentRetreatPos, OnPathFound));
                    m_IsCheckingPath = true;

                    break;
            }
            SetNextTimeForRecalculatingPath();
        }
    }
    protected override void InheritedOnDrawGizmosEnemy()
    {
        if (CanShoot())
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Debug.DrawLine(m_BodyRB2D.position, m_BodyRB2D.position + GetVector2FromAngle(m_TurretRB2D.rotation) * 10.0f, Gizmos.color);
    }

    ///Private Functions
    protected Vector2 GetPositionBetweenTwoLines(Vector2 v1, Vector2 p1, Vector2 v2, Vector2 p2)
    {
        if (v1.x == 0.0f || v2.x == 0.0f)
            return Vector2.zero;

        float m1, m2, c1, c2;
        m1 = v1.y / v1.x;
        m2 = v2.y / v2.x;
        c1 = p1.y - m1 * p1.x;
        c2 = p2.y - m2 * p2.x;

        float x = (c2 - c1) / (m1 - m2);
        return new Vector2(x, m1 * x + c1);
    }
}