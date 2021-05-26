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
        //Basic Raycast for hitting self check before advanced check
        if (CheckIfGoingToHitSelf(m_Bullets[0].GetComponent<CapsuleCollider2D>()))
            return false;

        //Check if it is in a position that it wants to shoot
        CapsuleCollider2D cc = m_Bullets[0].GetComponent<CapsuleCollider2D>();
        RaycastHit2D rayCastHit = Physics2D.CapsuleCast(I_ShootTransform.position, cc.size, cc.direction, m_TurretRB2D.rotation, GetVector2FromAngle(m_TurretRB2D.rotation),
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
                    rayCastHit = Physics2D.CapsuleCast(hit, cc.size, cc.direction, m_TurretRB2D.rotation, ang,
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
            GradualMoveTankAutoIfStationary(direction, m_SpeedForGradualChangeVelocity, 120.0f, m_SpeedForGradualChangeVelocityStationary);
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
}