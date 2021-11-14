using System.Collections.Generic;
using UnityEngine;

public class EnemyTank : Tank
{
    ///Inspector Variables
    [SerializeField] protected StateManager I_StateManager = new StateManager();
    [SerializeField] protected float I_TurretRotationSpeed = 36.0f;
    [Header("Shooting variables")]
    [SerializeField] protected float I_MinTimeToShoot = 6.0f;
    [SerializeField] protected float I_MaxTimeToShoot = 8.0f;
    [SerializeField] protected float I_MiniIntervalDelay = 0.3f;
    [SerializeField] protected float I_TurretTargetVariationMax = 20.0f;
    [SerializeField] protected float I_MaxDistanceForSeeingBullet = 5.0f;
    [SerializeField] protected float I_MaxShootDistanceForPlayerTank = 20.0f;
    [SerializeField] protected float I_VisualRangeAngle = 30.0f;
    [SerializeField] protected float I_SpeedForGradualChangeVelocity = 2.0f;
    [SerializeField] protected float I_SpeedForGradualChangeVelocityStationary = 3.0f;
    [SerializeField] protected float I_AngleForHardTurn = 120.0f;
    [SerializeField] private float I_BombDetectionRadius = 3.0f;

    ///Protected Variables
    protected float m_TimeAtNextShoot = 0.0f;
    protected float m_MiniTimeForNextShoot = 0.0f;
    protected bool m_HasShot = true;
    protected int m_CurrentWayPath = 0;
    protected bool m_ReachedEndOfPath = false;
    protected Rigidbody2D m_PlayerRB2D;
    protected Vector3[] m_Path;

    protected float m_DesiredTurretRotation = 0.0f;
    protected int m_DelayTurretUpdate = 0;

    protected bool m_IsCheckingPath = false;
    protected int m_TargetIndex = 0;
    protected Vector3 m_CurrentWayPoint;
    protected Rigidbody2D m_BulletClose;

    /// Private Variables
    private float m_TimeBeforeRecalculatigPath = 0.0f; 

    private const float m_SecondsForUpdateTargetTracking = 3.0f;
    private const int m_MaxTurretUpdateDelay = 20;

    ///Constant Variables

    ///Inherited Function
    protected virtual bool CanShoot() { return BasicCheckForOkShootHit(); }
    protected virtual void InheritedStartEnemy() { }
    protected virtual void InheritedFixedUpdateEnemy() { }
    protected virtual void FollowPathEnemy() { }
    protected virtual void RecalculatePath() { }
    protected virtual void InheritedOnDrawGizmosEnemy() { }

    /// Overridden Functions
    protected override void InheritedStart()
    {
        m_PlayerRB2D = GlobalVariables.GetPlayerTankBody();

        InheritedStartEnemy();

        RecalculatePath();
    }
    protected override void InheritedFixedUpdate()
    {
        //Path Finding
        if (m_TimeBeforeRecalculatigPath <= Time.unscaledTime)
            RecalculatePath();

        InheritedFixedUpdateEnemy();

        FollowPath();
        UpdateTurret();

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);

        I_StateManager.Update(Time.unscaledTime);
    }
    protected override void InheritedOnDrawGizmos()
    {
        if (m_BodyRB2D && m_Path != null)
        {
            for (int i = m_CurrentWayPath; i < m_Path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(m_Path[i], Vector3.one * 0.3f);

                if (i != 0)
                    Gizmos.DrawLine(m_Path[i], m_Path[i - 1]);

            }

            InheritedOnDrawGizmosEnemy();
        }
    }

    /// Protected Functions
    protected void SetNextShootTime(float min, float max)
    {
        m_TimeAtNextShoot = Time.unscaledTime + Random.Range(min, max);
        m_HasShot = false;
    }
    protected void AutomaticShoot()
    {
        if (!m_HasShot)
        {
            if (Time.unscaledTime >= m_TimeAtNextShoot)
            {
                if (Time.fixedTime >= m_MiniTimeForNextShoot)
                {
                    if (CanShoot())
                    {
                        //Time to shoot
                        Shoot();
                        m_HasShot = true;
                        DelayShoot();
                    }
                    m_MiniTimeForNextShoot = Time.fixedTime + I_MiniIntervalDelay;
                }
            }
        }
    }
    protected bool BasicCheckForOkShootHit()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(m_TurretRB2D.rotation),
            30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
        if (rayCastHit.collider)
        {
            if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName || rayCastHit.collider.gameObject.layer == GlobalVariables.LayerBullets)
                return true;
            else if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
            {
                Vector2 post = GetVector2FromAngle(m_TurretRB2D.rotation); //Origion Direction
                Vector2 normal = rayCastHit.normal; //Wall's normal
                Vector2 ang = post - (2 * Vector2.Dot(post, normal) * normal); //vector of desired direction

                rayCastHit = Physics2D.Raycast(rayCastHit.point, ang,
                    30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);
                if (rayCastHit.collider)
                {
                    if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankName ||
                        rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
                        return true;

                    return false;
                }
                return true;
            }
            return false;
        }
        return true;
    }
    protected void SetNextTimeForRecalculatingPath()
    {
        m_TimeBeforeRecalculatigPath = Time.unscaledTime + m_SecondsForUpdateTargetTracking;
    }
    protected void OnPathFound(Vector3[] newPath, bool pathSucess)
    {
        m_IsCheckingPath = false;
        if (pathSucess)
        {
            m_Path = newPath;
            m_CurrentWayPoint = m_Path[0];
            m_TargetIndex = 0;
        }
    }
    protected Bullet[] GetBulletsThatAreVisable()
    {
        List<Bullet> bulletsCanSee = new List<Bullet>();
        foreach (GameObject gob in GameObject.FindGameObjectsWithTag(GlobalVariables.TagBullet))
        {
            Rigidbody2D bulletRB2D = gob.GetComponent<Rigidbody2D>();
            if (Vector2.Distance(m_BodyRB2D.position, bulletRB2D.position) <= I_MaxDistanceForSeeingBullet)
            {
                RaycastHit2D hit = Physics2D.Raycast(I_ShootTransform.position, bulletRB2D.position - (Vector2)I_ShootTransform.position, I_MaxDistanceForSeeingBullet + 1,
                    1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
                if (hit.collider)
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
    protected bool GenericCanShoot()
    {
        if (IsAlive())
        {
            //Checks if there are no walls between turret and body
            RaycastHit2D hits = Physics2D.Raycast(m_BodyRB2D.position, GetVector2FromAngle(m_TurretRB2D.rotation), Vector2.Distance(m_BodyRB2D.position, I_ShootTransform.position));
            if (hits.collider)
            {
                if (hits.collider.gameObject != m_BodyRB2D.gameObject &&
                    hits.collider.gameObject.layer != GlobalVariables.LayerWallHole &&
                    hits.collider.gameObject.layer != GlobalVariables.LayerBullets)
                    return false;
            }

            //Basic Raycast for hitting self check before advanced check
            if (CheckIfGoingToHitSelf(m_Bullets[0].GetComponent<CapsuleCollider2D>()))
                return false;

            //Check if it is in a position that it wants to shoot
            CapsuleCollider2D cc = m_Bullets[0].GetComponent<CapsuleCollider2D>();
            RaycastHit2D rayCastHit = Physics2D.CapsuleCast(I_ShootTransform.position, cc.size, cc.direction, m_TurretRB2D.rotation, GetVector2FromAngle(m_TurretRB2D.rotation),
                   100.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
            if (rayCastHit.collider)
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
        return false; //Dead
    }
    protected Vector2 GetCornerPosFurthestAwayPlayer(Vector2 currentRetreatPos, float minRetreatDist, bool forceChange = false)
    {
        if (currentRetreatPos == Vector2.zero)
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
            currentRetreatPos = pos;
        }
        //Check if new pos needs to be identified
        else if (Vector2.Distance(currentRetreatPos, m_BodyRB2D.position) <= minRetreatDist || forceChange)
        {
            for (int i = 0; i < 4; i++)
            {
                //Get the current corner
                if (currentRetreatPos == m_BattleGroundCorners[i])
                {
                    //See which of the adjasent corners is more ideal to go to
                    float diff = 0;
                    if (i > 0)
                    {
                        diff = Vector2.Distance(m_BattleGroundCorners[i - 1], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[i - 1], m_BodyRB2D.position);
                        currentRetreatPos = m_BattleGroundCorners[i - 1];
                    }
                    else
                    {
                        diff = Vector2.Distance(m_BattleGroundCorners[3], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[3], m_BodyRB2D.position);
                        currentRetreatPos = m_BattleGroundCorners[3];
                    }

                    if (i < 3)
                    {
                        if (diff < Vector2.Distance(m_BattleGroundCorners[i + 1], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[i + 1], m_BodyRB2D.position))
                        {
                            currentRetreatPos = m_BattleGroundCorners[i + 1];
                        }
                    }
                    else
                    {
                        if (diff < Vector2.Distance(m_BattleGroundCorners[0], m_PlayerRB2D.position) - Vector2.Distance(m_BattleGroundCorners[0], m_BodyRB2D.position))
                        {
                            currentRetreatPos = m_BattleGroundCorners[0];
                        }
                    }
                    break;
                }
            }
        }

        return currentRetreatPos;
    }
    protected Vector2 CheckForNewCornerPosAvoidingPlayer(Vector2 currentRetreatPos, float minRetreatDist, bool forceChange = false)
    {
        if (Vector2.Distance(currentRetreatPos, m_BodyRB2D.position) <= minRetreatDist
            || forceChange || currentRetreatPos == new Vector2(999,999))
        {
            List<Vector2> possible = new List<Vector2>();
            Transform LevelRetreatPosManager = GameObject.Find("RetreatPos").transform;
            float minDisForPlayerTank = 4.0f;

            //Add all potential positions into the possible list
            for (int i = 0; i < LevelRetreatPosManager.childCount; i++)
            {
                //Only if distance between the tank and pos is greater than the distance between player and pos
                //If player tank is too close to the point, ingore it since don't want to be too close to the player
                Vector2 testPos = LevelRetreatPosManager.GetChild(i).position;
                if (Vector2.Distance(testPos, m_BodyRB2D.position) <
                    Vector2.Distance(testPos, m_PlayerRB2D.position) &&
                    Vector2.Distance(testPos, m_PlayerRB2D.position) > minDisForPlayerTank)
                    possible.Add(testPos);
            }
            //Remove last point if included
            if (currentRetreatPos != new Vector2(999,999))
            {
                if (possible.Contains(currentRetreatPos))
                    possible.Remove(currentRetreatPos);
            }

            //Select all that aren't too close to player tank if none are closer to tank
            if (possible.Count == 0)
            {
                for (int i = 0; i < LevelRetreatPosManager.childCount; i++)
                {
                    if (Vector2.Distance(LevelRetreatPosManager.GetChild(i).position, m_PlayerRB2D.position) > minDisForPlayerTank)
                        possible.Add(LevelRetreatPosManager.GetChild(i).position);
                }
            }
            
            return possible[Random.Range(0, possible.Count)];
        }

        //Don't want to force it and it isn't close so keep the current pos
        return currentRetreatPos;
    }
    //Movement AI
    protected void CheckForNextWayPoint()
    {
        //Move away from tank if too close
        if (Vector3.Distance(m_BodyRB2D.position, m_CurrentWayPoint) <= 0.5f)
        {
            if (m_TargetIndex + 1 >= m_Path.Length)
            {
                RecalculatePath();
                return;
            }

            m_CurrentWayPoint = m_Path[++m_TargetIndex];
        }
    }
    protected bool AvoidBomb()
    {
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
        return tempNeedsMovement;
    }
    protected Vector2 GetMoveDirAvoidingBullets()
    {
        Vector2 direction = ((Vector2)m_CurrentWayPoint - m_BodyRB2D.position);
        direction = direction.normalized * I_MoveSpeed;
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
                        if (direction.magnitude > I_MoveSpeed || direction.magnitude <= 0.0f)
                            direction *= I_MoveSpeed / direction.magnitude;
                    }
                }
            }
        }
        return direction;
    }

    /// Private Functions
    private void FollowPath()
    {
        if (m_Path != null)
        {
            FollowPathEnemy();
        }
        else
        {
            m_IsCheckingPath = false;
            RecalculatePath();
        }
    }
    private void UpdateTurret()
    {
        GradualRotation(ref m_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);
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
}