using UnityEngine;
using UnityEngine.Animations;
using Pathfinding;
using Unity.Profiling;

public class BasicEnemy : EnemyTank
{
    ///Inspector Variables
    [SerializeField] Rigidbody2D I_PlayerRB;
    [SerializeField] float I_MoveToNextPointDistance = 1.0f;
    [SerializeField] StateManager I_StateManager;
    [SerializeField] protected float m_rotateSpeed = 15.0f;

    ///Private Variables
    private float m_ResetTimeForMove = 0.0f;
    private float m_SecondsForUpdateTargetTracking = 1.0f;
    private float m_SecondsForUpdateAllTracking = 2.0f;
    private int m_TimesBetweenTracking = 0;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        //m_MoveTo = new Vector2(Random.Range(-20.0f, 20.0f), Random.Range(-20.0f, 20.0f));
        m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        I_StateManager.Start(Time.unscaledTime);
        m_Seeker = GetComponent<Seeker>();
        RecheckMovementTargeting();
    }
    protected override void InheritedFixedUpdate()
    {
        //Movement and rotation
        //Vector2 move = m_MoveTo - I_BodyRB2D.position;
        //MoveTank(move.normalized);
        //I_BodyRB2D.SetRotation(GetAngleFromVector2(move.normalized));

        //Path Finding
        if (m_ResetTimeForMove <= Time.unscaledTime)
        {
            if (m_TimesBetweenTracking < m_SecondsForUpdateAllTracking / m_SecondsForUpdateTargetTracking)
            {
                //Update only those that are important
                RecheckMovementTargeting(true);
                m_TimesBetweenTracking++;
            }
            else
            {
                //Update all
                RecheckMovementTargeting();
                m_TimesBetweenTracking = 0;
            }

            m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        }

        UpdateMove();
        I_StateManager.Update(Time.unscaledTime);
    }
    protected override void InheritedOnCollisionStay(Collision2D collision)
    {
        //Debug.Log(collision.gameObject + ": hit: " + gameObject);
        if (collision.gameObject.layer == GlobalVariables.LayerWalls)
        {
            //m_MoveTo = new Vector2(Random.Range(-20.0f, 20.0f), Random.Range(-20.0f, 20.0f));
        }

    }

    ///Private Functions
    private void SeekerReturn(Path p)
    {
        if (!p.error)
        {
            m_MovePath = p;
            m_CurrentWayPath = 0;
            m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
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
                GradualMoveTank(move, m_rotateSpeed);
                //GradualRotation(ref I_BodyRB2D, GetAngleFromVector2(move), I_BodyRB2D.rotation, m_rotateSpeed);

                //if ready to move on to next section of path, do so
                if (Vector2.Distance(I_BodyRB2D.position, m_MovePath.vectorPath[m_CurrentWayPath]) <= I_MoveToNextPointDistance)
                {
                    m_CurrentWayPath++;
                }
            }
        }
    }
    private void RecheckMovementTargeting(bool targetingOnly = false)
    {
        m_ReachedEndOfPath = true;
        switch (I_StateManager.M_CurrentState)
        {
            case StateManager.State.Chase:
                m_Seeker.StartPath(I_BodyRB2D.position, I_PlayerRB.position, SeekerReturn);
                break;
            case StateManager.State.Attack:
                if (!targetingOnly)
                {
                    Vector2 target = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized *
                        4 + I_BodyRB2D.position;
                    m_Seeker.StartPath(I_BodyRB2D.position, target, SeekerReturn);
                }
                break;
            default:
                break;
        }
    }
}
