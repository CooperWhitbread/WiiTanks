using UnityEngine;
using UnityEngine.Tilemaps;

///Abstract Tank Class
abstract public class Tank : MonoBehaviour
{
    ///Help
    //https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect

    ///Inspector Variables
    [SerializeField] protected BulletScript I_BulletScript;
    [SerializeField] protected Transform I_ShootTransform;
    [SerializeField] protected float I_MoveSpeed = 1.0f;
    [SerializeField] protected float I_StopDelayForShoot = 0.1f;
    [SerializeField] protected Transform I_TredTransform1;
    [SerializeField] protected Transform I_TredTransform2;
    [SerializeField] protected float I_TredDistanceBetweenTreds = 0.2f;

    ///Protected Variables
    //Objects
    protected Rigidbody2D m_BodyRB2D;
    protected Rigidbody2D m_TurretRB2D;
    protected GameObject m_TredPrefab;
    protected Bomb m_BombPrefab;
    protected GameObject m_DeathCrossPrefab;

    protected GameObject m_TredsParentObject;
    static protected ContactFilter2D s_ContactFilterWalls = new ContactFilter2D();
    protected Bullet[] m_Bullets = new Bullet[0];
    protected Bomb[] m_Bombs = new Bomb[0];

    protected float m_HitTimeForDeath = 0.0f;
    private Vector3 m_PositionWhenDead = Vector3.zero;
    private Vector3 m_StartingPos = Vector3.zero;
    protected Vector2 m_PreviousPos = Vector3.zero;
    private float m_moveBackRotateDistance = 0.12f;
    private int m_isCorrectingStationary = 0;
    private float m_TimeStationary = 0.0f;
    //Treds
    protected Vector2 m_TredLastTransform = Vector2.zero;
    //Shooting variables
    protected int m_NumOfBullets = 0;
    protected bool m_ShootDelayActive = false;
    protected float m_DesiredTimeForShoot = 0.0f;
    protected ParticleSystem m_ShootParticleSystem;
    protected ParticleSystem m_BodyExplosionParticleSystem;
    //Body Rotation
    protected bool m_SharpRotationOn = false;

    ///Constant Variables
    protected const float m_collissionBuffer = 0.1f;
    protected const float m_SpeedForGradualChangeVelocity = 2.0f;
    protected const float m_SpeedForGradualChangeVelocityStationary = 3.0f;

    ///Inherited Function
    protected virtual void InheritedStart() { }
    protected virtual void InheritedFixedUpdate() { }
    protected virtual void InheritedUpdate() { }
    protected virtual void InheritedOnCollisionEnter(Collision2D collision) { }
    protected virtual void InheritedOnCollisionStay(Collision2D collision) { }

    ///Unity Functions
    private void Awake()
    {
        m_BodyRB2D = transform.GetChild(0).GetComponent<Rigidbody2D>();
        m_TurretRB2D = transform.GetChild(1).GetComponent<Rigidbody2D>();
        m_TredsParentObject = transform.GetChild(2).gameObject;

        GlobalVariables gv = GlobalVariables.GetThisInstance();
        m_BombPrefab = gv.I_BombPrefab;
        m_DeathCrossPrefab = gv.I_DeathCrossPrefab;
        m_TredPrefab = gv.I_TredPrefab;

        ParticleSystem[] pts = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in pts)
        {
            if (p.transform.parent.name == GlobalVariables.TurretName)
                m_ShootParticleSystem = p;
            else
                m_BodyExplosionParticleSystem = p;
        }

        m_ShootParticleSystem.Stop();
        m_BodyExplosionParticleSystem.Stop();

        transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = true;
    }
    private void Start()
    {
        m_BodyRB2D = transform.GetChild(0).GetComponent<Rigidbody2D>();
        m_TurretRB2D = transform.GetChild(1).GetComponent<Rigidbody2D>();

        //Contact filter initializeation
        s_ContactFilterWalls.useTriggers = false;
        s_ContactFilterWalls.SetLayerMask((1 << GlobalVariables.LayerWalls) | (1 << GlobalVariables.LayerWallHole));

        InheritedStart();

        //Initialize all the bullets & bombs
        for (int i = 0; i < m_Bullets.Length; i++)
        {
            m_Bullets[i] = Instantiate(I_BulletScript.BulletPrefab, I_ShootTransform.position, I_ShootTransform.rotation);
            m_Bullets[i].gameObject.SetActive(false);
            m_Bullets[i].gameObject.transform.SetParent(gameObject.transform);
        }
        for (int i = 0; i < m_Bombs.Length; i++)
        {
            m_Bombs[i] = Instantiate(m_BombPrefab, I_ShootTransform.position, I_ShootTransform.rotation);
            m_Bombs[i].gameObject.SetActive(false);
            m_Bombs[i].gameObject.transform.SetParent(gameObject.transform);
        }

        m_StartingPos = m_BodyRB2D.transform.position;
    }
    private void FixedUpdate()
    {
        ConstantUpdatePre();
        if (IsAlive())
            InheritedFixedUpdate();
        ConstantUpdatePost();
    }
    private void Update()
    {
        if (m_BodyExplosionParticleSystem.particleCount == 0 &&
            !GetComponentInChildren<SpriteRenderer>().enabled &&
            Time.fixedTime >= m_HitTimeForDeath + 0.1f)
            if (gameObject.name != GlobalVariables.PlayerTankName)
            {
                Destroy(gameObject);
            }

        if (IsAlive())
        {
            InheritedUpdate();
        }
    }

    ///Public Functions
    public void DestroyBullet(int numberInArray)
    {
        m_Bullets[numberInArray].gameObject.SetActive(false);
    }
    public void ExplodeBomb(int numberInArray)
    {
        m_Bombs[numberInArray].gameObject.SetActive(false);
    }
    public void CollisionEnter(Collision2D collision)
    {
        //Check What is colliding
        switch (collision.gameObject.layer)
        {
            case GlobalVariables.LayerBullets:
                if (collision.gameObject.tag != GlobalVariables.TagBomb)
                    DestroyTank();
                break;
            case GlobalVariables.LayerWalls:
                //WallRotate(collision);
                break;
        }

        InheritedOnCollisionEnter(collision);
    }
    public void CollisionStay(Collision2D collision)
    {
        InheritedOnCollisionStay(collision);
    }
    public void DestroyTank()
    {
        if (!gameObject.name.Contains(GlobalVariables.PlayerTankName))
        {
            SpawnDeathCross();
            GlobalVariables.GetThisInstance().SetNonSpawingTankPos(m_StartingPos);
        }

        GlobalVariables.GetThisInstance().SetBullets(ref m_Bullets);
        GlobalVariables.GetThisInstance().SetBombs(ref m_Bombs);
        m_TredsParentObject.transform.parent = GameObject.Find(GlobalVariables.GlobalVariableObjectName).transform;
        m_BodyExplosionParticleSystem.Play();

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;

        GetComponentInChildren<BoxCollider2D>().enabled = false;
        m_HitTimeForDeath = Time.fixedTime;
        m_PositionWhenDead = transform.GetChild(0).position;
        transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = false;
    }
    public bool IsAlive() { return !m_BodyExplosionParticleSystem.IsAlive(); }

    ///Private Functions
    private void ConstantUpdatePre()
    {
        //Set Up and reset
        m_BodyRB2D.velocity = Vector2.zero;
        DelayShoot(false);
        m_TurretRB2D.position = m_BodyRB2D.position;

        //Treds
        if (!m_SharpRotationOn)
        {
            if (Vector2.Distance(m_TredLastTransform, m_BodyRB2D.position) >= I_TredDistanceBetweenTreds)
            {
                Instantiate(m_TredPrefab, I_TredTransform1.position, m_BodyRB2D.transform.rotation, m_TredsParentObject.transform);
                Instantiate(m_TredPrefab, I_TredTransform2.position, m_BodyRB2D.transform.rotation, m_TredsParentObject.transform);
                m_TredLastTransform = m_BodyRB2D.position;
            }
        }
    }
    private void ConstantUpdatePost()
    {
        if (!IsAlive())
        {
            transform.GetChild(0).position = m_PositionWhenDead;
        }
    }

    ///Protected Functions
    protected void Shoot()
    {
        //Checks if there are no walls between turret and body
        RaycastHit2D hit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(180 + m_TurretRB2D.rotation), 2.0f);
        if(hit.collider != null)
        {
            if (hit.collider.gameObject.layer == GlobalVariables.LayerTanks || hit.collider.gameObject.layer == GlobalVariables.LayerWallHole ||
                hit.collider.gameObject.layer == GlobalVariables.LayerBullets)
            {
                for (int i = 0; i < m_Bullets.Length; i++)
                {
                    if (!m_Bullets[i].GetComponent<SpriteRenderer>().enabled)
                    {
                        //Shoot Bullets
                        m_ShootParticleSystem.Play();
                        m_Bullets[i].Initialize(I_BulletScript, i, I_ShootTransform.position, m_TurretRB2D.rotation, I_BulletScript.NumberOfBounces);
                        m_Bullets[i].gameObject.SetActive(true);

                        //Stop Tank Temperarily
                        DelayShoot();
                        break;
                    }
                }
            }
        }
    }
    protected void DropBomb()
    {
        for (int i = 0; i < m_Bombs.Length; i++)
        {
            if (!m_Bombs[i].gameObject.activeSelf)
            {
                //Shoot Bullets
                m_Bombs[i].gameObject.SetActive(true);
                m_Bombs[i].Initialize(i, m_BodyRB2D.position, m_TurretRB2D.rotation);
                break;
            }
        }
    }
    protected void DelayShoot(bool TurnOn = true)
    {
        if (TurnOn)
        {
            //Initiate shoot delay
            m_ShootDelayActive = true;
            m_DesiredTimeForShoot = Time.time + I_StopDelayForShoot;
            return;
        }

        //Check for shoot delay stop
        if (m_ShootDelayActive)
        {
            if (m_DesiredTimeForShoot <= Time.time)
            {
                m_ShootDelayActive = false;
                m_DesiredTimeForShoot = 0.0f;
                m_ShootParticleSystem.Stop();
                return;
            }
        }
    }
    protected void GradualMoveTankAutoIfStationary(Vector2 moveDirection, float rotationSpeed, float sharpTurnRot = 90.0f, float sharpRotationSpeed = 3.0f)
    {
        if (m_PreviousPos == m_BodyRB2D.position)
            m_TimeStationary += Time.deltaTime;
        else
            m_TimeStationary = 0;

        if (m_TimeStationary >= 1.0f)
        {
            Vector2 dir = Vector2.zero;
            foreach (Tilemap tm in FindObjectsOfType<Tilemap>())
            {
                Vector3Int pos = tm.WorldToCell(m_BodyRB2D.position);
                if (tm.GetTile(pos + Vector3Int.right))
                    dir = Vector2.left;
                else if (tm.GetTile(pos + Vector3Int.left))
                    dir = Vector2.right;
                else if (tm.GetTile(pos + Vector3Int.up))
                    dir = Vector2.down;
                else if (tm.GetTile(pos + Vector3Int.down))
                    dir = Vector2.up;

                
            }
            GradualMoveTank(dir, rotationSpeed, sharpTurnRot, sharpRotationSpeed);

            m_isCorrectingStationary = 1;
        }
        else if (m_isCorrectingStationary > 0)
        {
            Vector2 dir = m_BodyRB2D.position - m_PreviousPos;
            Vector2 tarDir = dir - moveDirection;
            GradualMoveTank(dir + tarDir, rotationSpeed, sharpTurnRot, sharpRotationSpeed);
            m_isCorrectingStationary++;
            if (m_isCorrectingStationary == 20)
                m_isCorrectingStationary = 0;
        }
        else
            GradualMoveTank(moveDirection, rotationSpeed, sharpTurnRot, sharpRotationSpeed);

        m_PreviousPos = m_BodyRB2D.position;
    }
    protected void GradualMoveTank(Vector2 moveDirection, float rotationSpeed, float sharpTurnRot = 90.0f, float sharpRotationSpeed = 3.0f)
    {
        if (moveDirection != Vector2.zero)
        {
            //Only want to move if it can move
            if (!m_ShootDelayActive)
            {
                //Normalise the move direction
                moveDirection = moveDirection.normalized;

                //Calculate rotation in degrees
                float fullRotateDegree = GetAngleFromVector2(moveDirection);
                float diff = GetDiffInRotation(fullRotateDegree, m_BodyRB2D.rotation);
                float newRotateDegree = m_BodyRB2D.rotation;

                if (Mathf.Abs(Mathf.Abs(diff) - 180.0f) < 20.0f)
                {
                    newRotateDegree += 180.0f;
                    diff = 0;
                    m_BodyRB2D.SetRotation(m_BodyRB2D.rotation + 180.0f);
                }
                else if (diff > rotationSpeed) //Want to rotate anticlockwise if larger than min rotation
                    newRotateDegree += rotationSpeed;
                else if (diff < -rotationSpeed) //Want to rotate clockwise if larger than min rotation
                    newRotateDegree -= rotationSpeed;
                else
                {
                    newRotateDegree = fullRotateDegree;
                    m_SharpRotationOn = false;
                }

                //Fix the rotation to -180 <= r <= 180
                if (newRotateDegree > 180)
                {
                    newRotateDegree -= 360;
                    m_BodyRB2D.SetRotation(m_BodyRB2D.rotation - 360);
                }
                else if (newRotateDegree < -180)
                {
                    newRotateDegree += 360;
                    m_BodyRB2D.SetRotation(m_BodyRB2D.rotation + 360);
                }

                    //do a stationary rotation if distance is too great
                if (diff > sharpTurnRot ||
                    m_SharpRotationOn && diff > sharpRotationSpeed)
                {
                    newRotateDegree += sharpRotationSpeed;
                    m_SharpRotationOn = true;
                }
                else if (diff < -sharpTurnRot ||
                    m_SharpRotationOn && diff < -sharpRotationSpeed)
                {
                    newRotateDegree -= sharpRotationSpeed;
                    m_SharpRotationOn = true;
                }
                else
                    m_BodyRB2D.velocity = GetVector2FromAngle(newRotateDegree) * I_MoveSpeed;

                //Correct rotation for wall collision
                Collider2D[] cc1 = new Collider2D[8];
                int numOfHits1 = Physics2D.OverlapBox(m_BodyRB2D.position, GetComponentInChildren<BoxCollider2D>().size, newRotateDegree, s_ContactFilterWalls, cc1);

                for (int i = 0; i < numOfHits1; i++)
                {
                    if (cc1[i].gameObject.layer != GlobalVariables.LayerTanks)
                    {
                        Vector2 dir = cc1[i].ClosestPoint(m_BodyRB2D.position) - m_BodyRB2D.position;
                        if (IsWithinCertainValue(GetAngleFromVector2(dir), m_BodyRB2D.rotation, 8.0f) &&
                            !IsWithinCertainValue(fullRotateDegree, m_BodyRB2D.rotation, 89.0f)) //check if is hard against the wall
                        {
                            //Change the tank to face the other direction and rotate it relative to that direction
                            m_BodyRB2D.SetRotation(GetAngleBetween(m_BodyRB2D.rotation + 180.0f, -180.0f, 180.0f));
                            newRotateDegree = m_BodyRB2D.rotation;
                        }

                        Collider2D[] cc2 = new Collider2D[8];
                        Vector2 dir1 = m_BodyRB2D.position - cc1[0].ClosestPoint(m_BodyRB2D.position);
                        int numOfHits2 = Physics2D.OverlapBox(m_BodyRB2D.position + dir1.normalized * m_moveBackRotateDistance, GetComponentInChildren<BoxCollider2D>().size, newRotateDegree, s_ContactFilterWalls, cc2);
                        if (numOfHits2 == 0 && diff >= rotationSpeed)
                            m_BodyRB2D.position += dir1.normalized * m_moveBackRotateDistance;

                    }
                }

                m_BodyRB2D.MoveRotation(newRotateDegree);
            }
        }
    }
    protected void GradualMoveTankToTarget(Vector2 target, float rotationSpeed, float sharpTurnRot = 90.0f, float sharpRotationSpeed = 3.0f)
    {
        GradualMoveTank((target - m_BodyRB2D.position).normalized, rotationSpeed, sharpTurnRot, sharpRotationSpeed);
    }
    protected void RotateTurret(Vector3 lookAt)
    {
        Vector3 screenPoint = m_BodyRB2D.position;
        Vector2 offset = new Vector2(lookAt.x - screenPoint.x, lookAt.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        m_TurretRB2D.SetRotation(angle);
    }
    protected void RotateTurret(Vector2 lookAt)
    {
        Vector2 screenPoint = m_BodyRB2D.position;
        Vector2 offset = lookAt - screenPoint;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        m_TurretRB2D.SetRotation(angle);
    }
    protected void GradualRotation(ref Rigidbody2D rb, float targetDeg, float rotationSpeed)
    {
        //The angle that still needs to be made up
        float diff = GetDiffInRotation(targetDeg, rb.rotation);
        float rotSpd = rotationSpeed * Time.deltaTime;

        if (diff == 180 || diff == -180) //Don't want to be seen to rotate but still rotate it for logic
            rb.SetRotation(targetDeg);
        else if (diff > rotSpd) //Want to rotate anticlockwise if larger than min rotation
            rb.SetRotation(rb.rotation + rotSpd);
        else if (diff < - rotSpd) //Want to rotate clockwise if larger than min rotation
            rb.SetRotation(rb.rotation - rotSpd);
        else //Value is too small to do min rotation so rotation is done to exact value
            rb.SetRotation(targetDeg);

        //Fix the rotation to -180 <= r <= 180
        if (rb.rotation > 180)
            rb.SetRotation(rb.rotation - 360);
        else if (rb.rotation < -180)
            rb.SetRotation(rb.rotation + 360);
    }
    protected void SpawnDeathCross()
    {
        Instantiate(m_DeathCrossPrefab, m_BodyRB2D.position, Quaternion.identity, GameObject.Find(GlobalVariables.GlobalVariableObjectName).transform);
    }

    ///Logic protected Functions
    protected bool CheckIfGoingToHitSelf(CapsuleCollider2D testCapsule)
    {
        Vector2 selfAng = GetVector2FromAngle(m_TurretRB2D.rotation);
        ContactFilter2D cF2D = new ContactFilter2D();
        cF2D.SetLayerMask(1 << GlobalVariables.LayerWalls);

        RaycastHit2D[] selfRayCastHit = new RaycastHit2D[16];
        Physics2D.CapsuleCast(I_ShootTransform.position, testCapsule.size,
            testCapsule.direction, m_TurretRB2D.rotation, 
            selfAng, cF2D, selfRayCastHit);
        if (selfRayCastHit[0].collider != null)
        {
            Vector2 selfPost = selfAng; //Origion Direction
            Vector2 selfNormal = selfRayCastHit[0].normal; //Wall's normal
            selfAng = selfPost - (2 * Vector2.Dot(selfPost, selfNormal) * selfNormal); //vector of desired direction

            cF2D.SetLayerMask(1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerTanks);

            int numHit = Physics2D.CapsuleCast(selfRayCastHit[0].point, testCapsule.size,
                testCapsule.direction, GetAngleFromVector2(selfAng),
                selfAng, cF2D, selfRayCastHit);
            for (int i = 0; i < numHit; i++)
            {
                if (selfRayCastHit[i].collider == null)
                    break;
                if (selfRayCastHit[i].collider.gameObject.layer == GlobalVariables.LayerTanks)
                {
                    if (selfRayCastHit[i].collider.gameObject.transform.parent == transform)
                        return true; //going to hit itself
                                     //keep going otherwise, unitl hit wall or self
                }
                else //hitting wall
                    break;
            }
            //hitting nothing so return false (uses next line)
        }
        return false;
    }
    protected int GetNextFreeBulletIndex() //Returns -1 if all are being used
    {
        for (int i = 0; i < m_Bullets.Length; i++)
        {
            if (!m_Bullets[i].GetComponent<SpriteRenderer>().enabled)
                return i;
        }
        return -1;
    }
    //Not currently Using (haven't touched since change of sprite rotation)
    protected bool CheckCollisionForRotation(float rotation, float distance)
    {
        RaycastHit2D[] results = new RaycastHit2D[16];
        Vector2 moveDirection = Vector2.right * rotation;
        return CheckCollisionForRotation(rotation, moveDirection, distance, ref results) == 0 ? true : false;
    }
    protected bool CheckCollisionForRotation(float rotation, Vector2 moveDirection, float distance)
    {
        RaycastHit2D[] results = new RaycastHit2D[16];
        return CheckCollisionForRotation(rotation, moveDirection, distance, ref results) == 0 ? true : false;
    }
    protected int CheckCollisionForRotation(float rotation, Vector2 moveDirection, float distance, ref RaycastHit2D[] rayCastHit)
    {
        //Rotate it first
        float oldRotAng = m_BodyRB2D.rotation;
        m_BodyRB2D.MoveRotation(rotation);

        //Do the check
        int numResults = m_BodyRB2D.Cast(moveDirection, s_ContactFilterWalls, rayCastHit, distance);

        //Rotate Back
        m_BodyRB2D.MoveRotation(oldRotAng);
        return numResults;
    }

    ///Logic public Functions
    static public float Dot(Vector2 vec1, Vector2 vec2) => vec1.x * vec2.x + vec1.y * vec2.y;
    static public float GetAngleFromVector2(Vector2 angle)
    {
        return Vector3.SignedAngle(Vector3.right,
                       new Vector3(angle.x, 0.0f, angle.y),
                       Vector3.down);
    }
    static public float GetAngleFromVector3(Vector3 angle)
    {
        return Vector3.SignedAngle(Vector3.right,
                        new Vector3(angle.x, 0.0f, angle.y),
                        Vector3.down);
    }
    static public float GetDiffInRotation(float target, float current)
    {
        float diff = target - current;

        if (diff > 180)
        {
            //The difference is greater that 180 so needs to be changed to rotate the shorter distance
            target = target - 360;

            //Recalculate the diff
            diff = target - current;
        }
        else if (diff < -180)
        {
            //The difference is greater that 180 so needs to be changed to rotate the shorter distance
            target = target + 360;

            //Recalculate the diff
            diff = target - current;
        }

        return diff;

    }
    static public Vector2 GetVector2FromAngle(float angleDeg)
    {
        return new Vector2(Mathf.Cos(Mathf.Deg2Rad * angleDeg), Mathf.Sin(Mathf.Deg2Rad * angleDeg));
    }
    static public Vector3 GetVector3FromAngle(float angleDeg)
    {
        return new Vector3(Mathf.Cos(Mathf.Deg2Rad * angleDeg), Mathf.Sin(Mathf.Deg2Rad * angleDeg), 0.0f);
    }
    static public bool IsWithinCertainValue(float ang1, float ang2, float desiredDiff)
    {
        if (ang1 > 180)
            ang1 -= 360;
        else if (ang1 < -180)
            ang1 += 360;

        if (ang2 > 180)
            ang2 -= 360;
        else if (ang2 < -180)
            ang2 += 360;

        //normal
        if (Mathf.Abs(ang1 - ang2) <= desiredDiff)
            return true;

        //check if angle is at high extreme
        ang1 -= 360;
        if (Mathf.Abs(ang1 - ang2) <= desiredDiff)
            return true;

        //check if angle is at low extreme
        ang1 += 720;
        if (Mathf.Abs(ang1 - ang2) <= desiredDiff)
            return true;

        //not close ever
        return false;
    }
    static public float GetAngleBetween(float angle, float min, float max)
    {
        if (angle < min)
            angle += (max - min);
        else if (angle > max)
            angle -= (max - min);

        return angle;
    }
    static public bool TwoLinesColliding(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
    {
        if (Mathf.Max(A1.x, A2.x) < Mathf.Min(B1.x, B2.x))
            return false;

        float mA = (A1.y - A2.y) / (A1.x - A2.x);
        float mB = (B1.y - B2.y) / (B1.x - B2.x);
        float cA = A1.y - mA * A1.x;
        float cB = B1.y - mB * B1.x;

        if (mA == mB)
            return false;

        Vector2 intersect = Vector2.zero;
        intersect.x = (cB - cA) / (mA - mB);
        intersect.y = mA * intersect.x + cA;

        if (intersect.x < Mathf.Max(Mathf.Min(A1.x, A2.x), Mathf.Min(B1.x, B2.x))
            || intersect.x > Mathf.Min(Mathf.Max(A1.x, A2.x), Mathf.Max(B1.x, B2.x)))
            return false;

        return true;
    }
}
