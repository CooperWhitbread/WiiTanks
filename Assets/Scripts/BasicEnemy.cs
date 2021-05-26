using UnityEngine;
using UnityEngine.Animations;
using Unity.Profiling;

public class BasicEnemy : EnemyTank
{
    ///Inspector Variables
    [SerializeField] Rigidbody2D I_PlayerRB;
    [SerializeField] protected float m_rotateSpeed = 15.0f;

    ///Private Variables
    private float m_ResetTimeForMove = 0.0f;
    private const float m_SecondsForUpdateTargetTracking = 1.0f;
    private const float m_SecondsForUpdateAllTracking = 2.0f;
    private int m_TimesBetweenTracking = 0;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        //m_MoveTo = new Vector2(Random.Range(-20.0f, 20.0f), Random.Range(-20.0f, 20.0f));
        m_ResetTimeForMove = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
        I_StateManager.Start(Time.unscaledTime);
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
    private void RecheckMovementTargeting(bool targetingOnly = false)
    {
        m_ReachedEndOfPath = true;
        switch (I_StateManager.CurrentState)
        {
            case StateManager.State.Chase:
                break;
            case StateManager.State.Attack:
                if (!targetingOnly)
                {
                    //Vector2 target = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized *
                        //4 + m_BodyRB2D.position;
                }
                break;
            default:
                break;
        }
    }
}
