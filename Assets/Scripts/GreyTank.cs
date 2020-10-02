﻿using UnityEngine;
using Pathfinding;

public class GreyTank : EnemyTank
{

    ///Inspector Variables
    [SerializeField] Rigidbody2D I_PlayerRB2D;
    [SerializeField] float I_MoveToNextPoinPathFindingDistance = 1.0f;
    [SerializeField] StateManager I_StateManager;

    ///Private Variables
    private float m_ResetTimeForMove = 0.0f;
    private float m_SecondsForUpdateTargetTracking = 3.0f;
    private float m_DesiredTurretRotation = 0.0f;
    private int m_DelayTurretUpdate = 0;
    private int m_MaxTurretUpdateDelay = 20;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        I_StateManager.Start(Time.unscaledTime);
        m_Seeker = GetComponent<Seeker>();
        RecheckMovementTargeting();
    }
    protected override void InheritedFixedUpdate()
    {
        //Path Finding
        if (m_ResetTimeForMove <= Time.unscaledTime)
        {
            RecheckMovementTargeting();
            m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        }

        UpdateMove();
        UpdateTurret();
        CheckShoot();
        I_StateManager.Update(Time.unscaledTime);
    }

    ///Private Functions
    private void SeekerReturn(Path p)
    {
        if (!p.error)
        {
            m_MovePath = p;
            m_CurrentWayPath = 0;
            m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
            m_SharpRotationOn = false;
        }
        else
        {
            RecheckMovementTargeting();
        }
    }
    private void UpdateMove()
    {
        if (m_MovePath != null)
        {
            if (m_CurrentWayPath >= m_MovePath.vectorPath.Count)
            {
                RecheckMovementTargeting();
            }
            else
            {
                m_ReachedEndOfPath = false;
                Vector2 move = ((Vector2)m_MovePath.vectorPath[m_CurrentWayPath] - I_BodyRB2D.position).normalized;

                //move and rotate
                GradualMoveTank(move, m_SpeedForGradualChangeVelocity, 120.0f, m_SpeedForGradualChangeVelocityStationary);

                //if ready to move on to next section of path, do so
                if (Vector2.Distance(I_BodyRB2D.position, m_MovePath.vectorPath[m_CurrentWayPath]) <= I_MoveToNextPoinPathFindingDistance)
                {
                    m_CurrentWayPath++;
                    m_SharpRotationOn = false;
                }
            }
        }
    }
    private void UpdateTurret()
    {
        switch (I_StateManager.M_CurrentState)
        {
            case StateManager.State.Stelth:
            case StateManager.State.Idle:
                GradualRotation(ref I_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);
                if (Mathf.Abs(I_TurretRB2D.rotation - m_DesiredTurretRotation) < 0.5f)
                {
                    m_DelayTurretUpdate++;
                    if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
                    {
                        m_DesiredTurretRotation = Random.Range(-120.0f, 120.0f) + GetAngleFromVector2(I_PlayerRB2D.position - I_BodyRB2D.position);
                        if (m_DesiredTurretRotation > 180)
                            m_DesiredTurretRotation -= 360;
                        else if (m_DesiredTurretRotation < -180)
                            m_DesiredTurretRotation += 360;
                        m_DelayTurretUpdate = 0;
                    }
                }
                break;
            case StateManager.State.Attack:
                GradualRotation(ref I_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);
                if (Mathf.Abs(I_TurretRB2D.rotation - m_DesiredTurretRotation) < 0.5f)
                {
                    m_DelayTurretUpdate++;
                    if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
                    {
                        m_DesiredTurretRotation = GetAngleFromVector2(I_PlayerRB2D.position - I_BodyRB2D.position);
                        if (m_DesiredTurretRotation > 180)
                            m_DesiredTurretRotation -= 360;
                        else if (m_DesiredTurretRotation < -180)
                            m_DesiredTurretRotation += 360;
                        m_DelayTurretUpdate = 0;
                    }
                }
                break;
            default:
                break;
        }
    }
    private void CheckShoot()
    {
        if (I_StateManager.M_CurrentState == StateManager.State.Attack ||
            I_StateManager.M_CurrentState == StateManager.State.Stelth)
        {
            Vector2 lookDir = GetVector2FromAngle(I_TurretRB2D.rotation);
            Vector2 playerRel = (I_PlayerRB2D.position - I_BodyRB2D.position).normalized;
            float angle = Vector2.Angle(lookDir, playerRel);

            if (Vector2.Distance(I_BodyRB2D.position, I_PlayerRB2D.position) < 14.0f && angle < 20.0f)
            {
                AutomaticShoot();
                if (m_HasShot)
                {
                    SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);
                }
            }
        }
    }
    private void RecheckMovementTargeting()
    {
        m_ReachedEndOfPath = true;
        switch (I_StateManager.M_CurrentState)
        {
            case StateManager.State.Stelth:
            case StateManager.State.Attack:
            case StateManager.State.Idle:
                Vector2 target = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * 4;
                target += I_BodyRB2D.position;
                Vector2 pos = GetVector2FromAngle(I_BodyRB2D.rotation).normalized * 2f;
                target += pos;
                m_Seeker.StartPath(I_BodyRB2D.position, target, SeekerReturn);
                break;
            default:
                break;
        }
    }
}