using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GreyTank : EnemyTank
{

    ///Inspector Variables
    [Header("Grey Tank AI Variables")]
    [SerializeField] private float I_MovementDistanceFromBulletScalarValue = 8.0f;
    [SerializeField] private float I_MovementCurrentDirectionScalar = 1.0f;
    [SerializeField] private float I_BombDetectionRadius = 3.0f;
    [SerializeField] private float I_MinDistBeforeMoveOnForRetreatingTank = 3.0f;

    ///Private Variables
    private Vector2 m_CurrentRetreatPos = Vector2.zero;
    
    ///Virtual Functions
    protected override void InheritedStartEnemy()
    {
        I_StateManager.Start(Time.unscaledTime);
        m_Bullets = new Bullet[1];
        m_Bombs = new Bomb[0];
        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, m_PlayerRB2D.position, OnPathFound));
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
            }
        }
        direction = direction.normalized;

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
        if (tempNeedsMovement)
            GradualMoveTankAutoIfStationary(direction, I_SpeedForGradualChangeVelocity, I_AngleForHardTurn, I_SpeedForGradualChangeVelocityStationary);
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
}