using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowTank : EnemyTank
{
    ///Inspector Variables
    [Header("Yellow Tank AI Variables")]
    [SerializeField] private float I_MinDistBeforeMoveOnForRetreatingTank = 3.0f;

    private float m_BombDropTime = 0.0f;
    private bool m_InRadiusOfBomb = false;
    private Vector2 m_BombPos = Vector2.zero;
    private Vector2 m_BombMoveDir = Vector2.zero;

    ///Virtual Functions
    protected override void InheritedStartEnemy()
    {
        I_StateManager.Start(Time.unscaledTime);
        InitializeBulletsAndBombs(1, 4);
        m_BombDropTime = Random.Range(0.5f, 3.5f) + Time.time;
    }
    protected override void InheritedFixedUpdateEnemy()
    {
        m_DesiredTurretRotation = GetAngleFromVector2(m_PlayerRB2D.position - m_BodyRB2D.position);

        if (Time.time >= m_BombDropTime)
        {
            DropBomb();
            m_InRadiusOfBomb = true;
            m_BombDropTime = Time.time + Random.Range(3.0f, 5.0f);
            m_BombPos = m_BodyRB2D.position;
        }

        //Reset personal bomb drop
        if (Vector2.Distance(m_BombPos, m_BodyRB2D.position) > 2.5f)
        {
            m_InRadiusOfBomb = false;
            m_BombMoveDir = Vector2.zero;
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
        //If has just dropped bomb, force it to escape first
        if (m_InRadiusOfBomb)
        {
            //Go through to see if a direction needs to be picked
            if (m_BombMoveDir == Vector2.zero)
            {
                //See if can escape this direction
                RaycastHit2D[] rch = new RaycastHit2D[10];
                ContactFilter2D cf = new ContactFilter2D();
                Vector2 testingMovePos = m_CurrentWayPoint;
                int defualtBreakLoop = 10;

                while (defualtBreakLoop > 0)
                {
                    defualtBreakLoop--;
                    int num = Physics2D.CircleCast(m_BombPos, 1.0f,
                        testingMovePos - m_BodyRB2D.position, cf, rch, 2.2f);
                    if (num == 0)
                    {
                        m_BombMoveDir = testingMovePos - m_BodyRB2D.position;
                        break;
                    }

                    bool canMove = true;
                    for (int i = 0; i < num; i++)
                    {
                        //if is itself, we want to ignore it
                        if (rch[i].collider.gameObject != m_BodyRB2D.gameObject)
                            canMove = false;
                    }

                    //If there is nothing in the way, move here
                    if (canMove)
                    {
                        m_BombMoveDir = testingMovePos - m_BodyRB2D.position;
                        break;
                    }
                    else //Create a new position to check
                        testingMovePos = m_BodyRB2D.position + new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
                }
            }

            //Move the tank
            GradualMoveTankAutoIfStationary(m_BombMoveDir.normalized, I_SpeedForGradualChangeVelocity, 
                I_MoveSpeed, I_AngleForHardTurn, I_SpeedForGradualChangeVelocityStationary);
        }
        else
        {
            //Do a normal movement 
            CheckForNextWayPoint();
            bool tempNeedsMovement = AvoidBomb();

            if (tempNeedsMovement)
            {
                Vector2 direction = GetMoveDirAvoidingBullets();
                GradualMoveTankAutoIfStationary(direction.normalized, I_SpeedForGradualChangeVelocity, 
                    direction.magnitude, I_AngleForHardTurn, I_SpeedForGradualChangeVelocityStationary);
            }
        }
    }
    protected override void RecalculatePath()
    {
        if (!m_IsCheckingPath)
        {
            switch (I_StateManager.CurrentState)
            {
                case StateManager.State.Attack:
                    Vector2 bL = GlobalVariables.GetThisInstance().GetCamerBoundsBottomLeft();
                    Vector2 tR = GlobalVariables.GetThisInstance().GetCamerBoundsTopRight();
                    Vector2 targetPos = new Vector2(Random.Range(bL.x, tR.x), Random.Range(bL.y, tR.y));

                    int time = 0;
                    while (time < 20)
                    {
                        if (!CheckIfPointIsInsideWall(targetPos))
                            time = 21;
                        else
                        {
                            //if target pos in inside a wall, get a new position
                            targetPos = new Vector2(Random.Range(bL.x, tR.x), Random.Range(bL.y, tR.y));
                            time++;
                        }

                        if (time == 20)
                        {
                            //Stops random position still being supplied while the rest of the code can still run
                            targetPos = CheckForNewCornerPosAvoidingPlayer(m_BodyRB2D.position, I_MinDistBeforeMoveOnForRetreatingTank, true);
                        }
                    }
                    //Debug.DrawLine(m_BodyRB2D.position, targetPos, Color.black, 90.0f);
                    PathRequestManager.RequestPath(new PathRequest(m_BodyRB2D.position, targetPos, OnPathFound));
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
