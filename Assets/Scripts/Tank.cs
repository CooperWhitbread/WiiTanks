using UnityEngine;

///Abstract Tank Class
abstract public class Tank : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected BulletScript I_BulletScript;
    [SerializeField] protected Bullet I_BulletPrefab;
    [SerializeField] protected Transform I_ShootTransform;
    [SerializeField] protected float I_MoveSpeed = 1.0f;
    [SerializeField] protected float I_StopDelayForShoot = 0.1f;
    [SerializeField] protected Rigidbody2D I_BodyRB2D;
    [SerializeField] protected Rigidbody2D I_TurretRB2D;
    [SerializeField] protected GameObject I_TredPrefab;
    [SerializeField] protected GameObject I_TredsParentObject;
    [SerializeField] protected Transform I_TredTransform1;
    [SerializeField] protected Transform I_TredTransform2;
    [SerializeField] protected float I_TredDistanceBetweenTreds = 0.2f;
    [SerializeField] protected Bomb I_BombPrefab;
    [SerializeField] protected GameObject I_DeathCrossPrefab;

    ///Protected Variables
    //Objects
    static protected ContactFilter2D s_ContactFilter = new ContactFilter2D();
    protected Bullet[] m_Bullets = new Bullet[5];
    private Bomb[] m_Bombs = new Bomb[2];
    //Treds
    protected Vector2 m_TredLastTransform = Vector2.zero;
    //Shooting variables
    protected int m_NumOfBullets = 0;
    protected bool m_ShootDelayActive = false;
    protected float m_DesiredTimeForShoot = 0.0f;
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
    private void Start()
    {
        //Contact filter initializeation
        s_ContactFilter.useTriggers = false;
        s_ContactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        s_ContactFilter.useLayerMask = true;

        //Initialize all the bullets & bombs
        for (int i = 0; i < m_Bullets.Length; i++)
        {
            m_Bullets[i] = Instantiate(I_BulletPrefab, I_ShootTransform.position, I_ShootTransform.rotation);
            m_Bullets[i].gameObject.SetActive(false);
            m_Bullets[i].gameObject.transform.SetParent(gameObject.transform);
        }
        for (int i = 0; i < m_Bombs.Length; i++)
        {
            m_Bombs[i] = Instantiate(I_BombPrefab, I_ShootTransform.position, I_ShootTransform.rotation);
            m_Bombs[i].gameObject.SetActive(false);
            m_Bombs[i].gameObject.transform.SetParent(gameObject.transform);
        }

        InheritedStart();
    }
    private void FixedUpdate()
    {
        ConstantUpdatePre();
        InheritedFixedUpdate();
        ConstantUpdatePost();
    }
    private void Update()
    {
        InheritedUpdate();
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
        GameObject.Find(GlobalVariables.GlobalVariableObjectName).GetComponent<GlobalVariables>().SetBullets(ref m_Bullets);
        GameObject.Find(GlobalVariables.GlobalVariableObjectName).GetComponent<GlobalVariables>().SetBombs(ref m_Bombs);
        I_TredsParentObject.transform.parent = GameObject.Find(GlobalVariables.GlobalVariableObjectName).transform;
        SpawnDeathCross();
       Destroy(gameObject);
    }

    ///Private Functions
    private void ConstantUpdatePre()
    {
        //Set Up and reset
        I_BodyRB2D.velocity = Vector2.zero;
        DelayShoot(false);

        //Treds
        if (Vector2.Distance(m_TredLastTransform, I_BodyRB2D.position) >= I_TredDistanceBetweenTreds)
        {
            Instantiate(I_TredPrefab, I_TredTransform1.position, I_BodyRB2D.transform.rotation, I_TredsParentObject.transform);
            Instantiate(I_TredPrefab, I_TredTransform2.position, I_BodyRB2D.transform.rotation, I_TredsParentObject.transform);
            m_TredLastTransform = I_BodyRB2D.position;
        }
    }
    private void ConstantUpdatePost()
    {
    }
    /*private void WallRotate(Collision2D collision)
    {

        float rotateDegree1 = GetAngleFromVector2(new Vector2(-collision.contacts[0].normal.y, collision.contacts[0].normal.x));
        float rotateDegree2 = GetAngleFromVector2(new Vector2(collision.contacts[0].normal.y, -collision.contacts[0].normal.x));

        //Want the smallest change
        //if (Mathf.Abs(I_BodyRB2D.rotation - rotateDegree1) < Mathf.Abs(I_BodyRB2D.rotation - rotateDegree2))
            //m_targetRotation = rotateDegree1;
        //else
            //m_targetRotation = rotateDegree2;

    } */ //Not using

    ///Protected Functions
    protected void Shoot()
    {
        RaycastHit2D hit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(180 + I_TurretRB2D.rotation), 0.5f);
        if(hit.collider != null)
        {
            if (hit.collider.gameObject.layer == GlobalVariables.LayerTanks || hit.collider.gameObject.layer == GlobalVariables.LayerWallHole)
            {
                for (int i = 0; i < m_Bullets.Length; i++)
                {
                    if (!m_Bullets[i].gameObject.activeSelf)
                    {
                        //Shoot Bullets
                        m_Bullets[i].Initialize(I_BulletScript, i, I_ShootTransform.position, I_TurretRB2D.rotation);
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
                m_Bombs[i].Initialize(i, I_BodyRB2D.position, I_TurretRB2D.rotation);
                m_Bombs[i].gameObject.SetActive(true);
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
                return;
            }
        }
    }
    /*protected void MoveTank(Vector2 moveDirection)
    {
        if (moveDirection != Vector2.zero)
        {
            //Only want to move if it can move
            if (!m_ShootDelayActive)
            {
                //Normalise the move direction
                moveDirection = moveDirection.normalized;

                //Calculate rotation in degrees
                float rotateDegree = GetAngleFromVector2(moveDirection);

                //Check Rotation Fine
                //if (CheckCollisionForRotation(rotateDegree, moveDirection, I_MoveSpeed * Time.deltaTime + m_collissionBuffer))
                
                I_BodyRB2D.velocity = moveDirection.normalized * I_MoveSpeed;
            }
        }
    } */ //Not Using
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
                float diff = GetDiffInRotation(fullRotateDegree, I_BodyRB2D.rotation);
                float newRotateDegree = I_BodyRB2D.rotation;

                if (diff == 180 || diff == -180)
                    newRotateDegree = fullRotateDegree;
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
                    newRotateDegree -= 360;
                else if (newRotateDegree < -180)
                    newRotateDegree += 360;

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
                    I_BodyRB2D.velocity = GetVector2FromAngle(newRotateDegree) * I_MoveSpeed;

                I_BodyRB2D.SetRotation(newRotateDegree);
            }
        }
    }
    protected void RotateTurret(Vector3 lookAt)
    {
        Vector3 screenPoint = I_BodyRB2D.position;
        Vector2 offset = new Vector2(lookAt.x - screenPoint.x, lookAt.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        I_TurretRB2D.SetRotation(angle);
    }
    protected void RotateTurret(Vector2 lookAt)
    {
        Vector2 screenPoint = I_BodyRB2D.position;
        Vector2 offset = lookAt - screenPoint;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        I_TurretRB2D.SetRotation(angle);
    }
    protected void GradualRotation(ref Rigidbody2D rb, float targetDeg, float rotationSpeed)
    {
        //The angle that still needs to be made up
        float diff = GetDiffInRotation(targetDeg, rb.rotation);

        if (diff == 180 || diff == -180) //Don't want to be seen to rotate but still rotate it for logic
            rb.SetRotation(targetDeg);
        else if (diff > rotationSpeed) //Want to rotate anticlockwise if larger than min rotation
            rb.SetRotation(rb.rotation + rotationSpeed);
        else if (diff < -rotationSpeed) //Want to rotate clockwise if larger than min rotation
            rb.SetRotation(rb.rotation - rotationSpeed);
        else //Value is too small to do min rotation so rotation is done to exact value
            rb.SetRotation(targetDeg);

        //Fix the rotation to -180 <= r <= 180
        if (rb.rotation > 180)
            rb.SetRotation(rb.rotation - 360);
        else if (rb.rotation < -180)
            rb.SetRotation(rb.rotation + 360);
    }
    protected bool CheckForOkShootHit()
    {
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(I_TurretRB2D.rotation),
            30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);
        if (rayCastHit.collider != null)
        {
            if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                return true;
            else if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerWalls)
            {
                Vector2 post = GetVector2FromAngle(I_TurretRB2D.rotation); //Origion Direction
                Vector2 normal = rayCastHit.normal; //Wall's normal
                Vector2 ang = post - (2 * Vector2.Dot(post, normal) * normal); //vector of desired direction

                rayCastHit = Physics2D.Raycast(rayCastHit.point, ang,
                    30.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);
                if (rayCastHit.collider != null)
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
    protected void SpawnDeathCross()
    {
        Instantiate(I_DeathCrossPrefab, I_BodyRB2D.position, Quaternion.identity, GameObject.Find(GlobalVariables.GlobalVariableObjectName).transform);
    }

    ///Logic protected Functions
    //Not currently Using
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
        float oldRotAng = I_BodyRB2D.rotation;
        I_BodyRB2D.MoveRotation(rotation);

        //Do the check
        int numResults = I_BodyRB2D.Cast(moveDirection, s_ContactFilter, rayCastHit, distance);

        //Rotate Back
        I_BodyRB2D.MoveRotation(oldRotAng);
        return numResults;
    }

    ///Logic public Functions
    public float Dot(Vector2 vec1, Vector2 vec2) => vec1.x * vec2.x + vec1.y * vec2.y;
    public float GetAngleFromVector2(Vector2 angle)
    {
        return Vector3.SignedAngle(Vector3.right,
                       new Vector3(angle.x, 0.0f, angle.y),
                       Vector3.down);
    }
    public float GetAngleFromVector3(Vector3 angle)
    {
        return Vector3.SignedAngle(Vector3.right,
                        new Vector3(angle.x, 0.0f, angle.y),
                        Vector3.down);
    }
    public float GetDiffInRotation(float target, float current)
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
    public Vector2 GetVector2FromAngle(float angleDeg)
    {
        return new Vector2(Mathf.Cos(Mathf.Deg2Rad * angleDeg), Mathf.Sin(Mathf.Deg2Rad * angleDeg));
    }
}
