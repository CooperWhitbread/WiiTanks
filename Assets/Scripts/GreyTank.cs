using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GreyTank : EnemyTank
{

    ///Inspector Variables
    [Header("Grey Tank AI Variables")]
    [SerializeField] private float I_MinDistBeforeMoveOnForRetreatingTank = 3.0f;

    ///Private Variables
    private Vector2 m_CurrentRetreatPos = Vector2.zero;
    
    ///Virtual Functions
    protected override void InheritedStartEnemy()
    {
        I_StateManager.Start(Time.unscaledTime);
        InitializeBulletsAndBombs(1, 0); 
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
    }
    protected override bool CanShoot()
    {
        return GenericCanShoot();
    }
    protected override void FollowPathEnemy()
    {
        CheckForNextWayPoint();
        bool tempNeedsMovement = AvoidBomb();

        if (tempNeedsMovement)
        {
            Vector2 direction = GetMoveDirAvoidingBullets();
            GradualMoveTankAutoIfStationary(direction.normalized, I_SpeedForGradualChangeVelocity, direction.magnitude, I_AngleForHardTurn, I_SpeedForGradualChangeVelocityStationary);
        }
    }
    protected override void RecalculatePath()
    {
        if (!m_IsCheckingPath)
        {
            //check for tanks in front, stop collisions
            Vector2 dir = ((Vector2)m_CurrentWayPoint - m_BodyRB2D.position);
            Tank[] tanks = FindObjectsOfType<Tank>();
            bool forceSwitch = false;
            foreach (Tank t in tanks)
            {
                if (t.transform != transform)
                {
                    Vector2 vecBetween = (Vector2)t.transform.GetChild(0).position - m_BodyRB2D.position;
                    float angle = Vector2.Angle(vecBetween, dir);
                    if (angle <= 30 && vecBetween.magnitude <= 2.0f)
                    {
                        forceSwitch = true;
                        break;
                    }
                }
            }

            switch (I_StateManager.CurrentState)
            {
                case StateManager.State.Attack:
                    if (forceSwitch) //want to picka random direction to stop them from being near each other temperarily
                        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, 
                            m_BodyRB2D.position + new Vector2(Random.Range(0, 1), Random.Range(0, 1)).normalized * 5, OnPathFound));
                    else //no issue with approximity to other tanks so trget the player
                        PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position,m_PlayerRB2D.position, OnPathFound));

                    m_CurrentRetreatPos = Vector2.zero;
                    break;

                case StateManager.State.Escape:
                    m_CurrentRetreatPos = CheckForNewCornerPosAvoidingPlayer(m_CurrentRetreatPos, I_MinDistBeforeMoveOnForRetreatingTank, forceSwitch);
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