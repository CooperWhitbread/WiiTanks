using UnityEngine;
using System.Collections.Generic;

///Abstract Tank Class
abstract public class Tank : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected BulletScript I_BulletScript;
    [SerializeField] protected Bullet I_BulletPrefab;
    [SerializeField] protected Transform I_ShootTransform;
    [SerializeField] protected float I_MoveSpeed = 1.0f;
    [SerializeField] protected float I_StopDelayForShoot = 0.1f;
    [SerializeField] protected float m_rotateSpeed = 15.0f;
    [SerializeField] protected Rigidbody2D I_BodyRB2D;
    [SerializeField] protected Rigidbody2D I_TurretRB2D;

    ///Protected Variables
    static protected ContactFilter2D s_ContactFilter = new ContactFilter2D();
    protected float m_targetRotation = 0.0f;
    protected Vector2 m_PreviousBodyRoation = Vector2.zero;
    protected Bullet[] m_Bullets = new Bullet[5];
    protected int m_NumOfBullets = 0;
    protected bool m_ShootDelayActive = false;
    protected float m_DesiredTimeForShoot = 0.0f;

    ///Constant Variables
    protected const float m_collissionBuffer = 0.1f;

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

        //Initialize all the bullets
        for (int i = 0; i < 5; i++)
        {
            m_Bullets[i] = Instantiate(I_BulletPrefab, I_ShootTransform.position, I_ShootTransform.rotation);
            m_Bullets[i].gameObject.SetActive(false);
            m_Bullets[i].gameObject.transform.SetParent(gameObject.transform);
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
    public void CollisionEnter(Collision2D collision)
    {
        //Check What is colliding
        switch (collision.gameObject.layer)
        {
            case GlobalVariables.LayerBullets:
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

    ///Private Functions
    private void ConstantUpdatePre()
    {
        //Set Up and reset
        I_BodyRB2D.velocity = Vector2.zero;
        DelayShoot(false);
    }
    private void ConstantUpdatePost()
    {
    }
    private void WallRotate(Collision2D collision)
    {

        float rotateDegree1 = GetAngleFromVector2(new Vector2(-collision.contacts[0].normal.y, collision.contacts[0].normal.x));
        float rotateDegree2 = GetAngleFromVector2(new Vector2(collision.contacts[0].normal.y, -collision.contacts[0].normal.x));

        //Want the smallest change
        if (Mathf.Abs(I_BodyRB2D.rotation - rotateDegree1) < Mathf.Abs(I_BodyRB2D.rotation - rotateDegree2))
            m_targetRotation = rotateDegree1;
        else
            m_targetRotation = rotateDegree2;

    }
    private void DestroyTank()
    {
        Destroy(gameObject);
    }

    ///Protected Functions
    protected void MoveTank(Vector2 moveDirection)
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
                if (CheckCollisionForRotation(rotateDegree, moveDirection, I_MoveSpeed * Time.deltaTime + m_collissionBuffer))
                {
                    m_targetRotation = rotateDegree;
                }
                I_BodyRB2D.velocity = moveDirection.normalized * I_MoveSpeed;
            }
        }
    }
    protected void Shoot()
    {
        for (int i = 0; i < 5; i++)
        {
            if (!m_Bullets[i].gameObject.activeSelf)
            {
                //Shoot Bullets
                m_Bullets[i].Initialize(I_BulletScript, i, I_ShootTransform.position, I_ShootTransform.rotation);
                m_Bullets[i].gameObject.SetActive(true);

                //Stop Tank Temperarily
                DelayShoot();
                break;
            }
        }
    }
    protected void RotateTurret(Vector3 lookAt)
    {
        Vector3 screenPoint = I_BodyRB2D.position;
        Vector2 offset = new Vector2(lookAt.x - screenPoint.x, lookAt.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        I_TurretRB2D.SetRotation(angle);
    }
    protected void RotateTurret(Vector2 lookAt)
    {
        Vector2 screenPoint = I_BodyRB2D.position;
        Vector2 offset = lookAt - screenPoint;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        I_TurretRB2D.SetRotation(angle);
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
    protected void GradualRotation(ref Rigidbody2D rb, float targetDeg, float current, float rotationSpeed)
    {
        //The angle that still needs to be made up
        float diff = GetDiffInRotation(targetDeg, current);

        if (diff == 180 || diff == -180) //Don't want to be seen to rotate but still rotate it for logic
            rb.SetRotation(targetDeg);
        else if (diff > rotationSpeed) //Want to rotate anticlockwise if larger than min rotation
            rb.SetRotation(rb.rotation + rotationSpeed);
        else if (diff < -rotationSpeed) //Want to rotate clockwise if larger than min rotation
            rb.SetRotation(rb.rotation - rotationSpeed);
        else //Value is too small to do min rotation so rotation is done to exact value
            rb.SetRotation(diff + rb.rotation);

        //Fix the rotation to -180 <= r <= 180
        if (rb.rotation > 180)
            rb.SetRotation(rb.rotation - 360);
        else if (rb.rotation < -180)
            rb.SetRotation(rb.rotation + 360);
    }
   
    ///Logic Protected Functions
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
    protected float Dot(Vector2 vec1, Vector2 vec2) => vec1.x * vec2.x + vec1.y * vec2.y;
    protected float GetAngleFromVector2(Vector2 angle)
    {
        return Vector3.SignedAngle(Vector3.forward,
                       new Vector3(angle.x, 0.0f, angle.y),
                       Vector3.down);
    }
    protected float GetAngleFromVector3(Vector3 angle)
    {
        return Vector3.SignedAngle(Vector3.forward,
                        new Vector3(angle.x, 0.0f, angle.y),
                        Vector3.down);
    }
    protected float GetDiffInRotation(float target, float current)
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
}

//State Management
[System.Serializable]
class StateManager
{
    //Can be controlled mainly in parent class but contains basic functionality to work in the background

    /// Data storage
    public enum State
    {
        Empty,
        Attack,
        Idle,
        Chase
    }
    [System.Serializable]
    public struct StateInfo
    {
        public State M_State;
        public float M_TimeLength;
    }

    /// Variables
    public State M_CurrentState { get; set; } = State.Empty;
    public float M_TimeForNextSwitch { get; set; } = 0.0f;
    public int M_CurrentLevelInState { get; set; } = 0;
    [SerializeField] List<StateInfo> M_States;

    /// Functions
    public void Start(float currentTime)
    {
        if (M_States.Count != 0)
        {
            M_CurrentState = M_States[M_CurrentLevelInState].M_State;
            M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;

        }
    }
    public void Update(float currentTime)
    {
        if (M_CurrentState != State.Empty && M_States.Count != 0)
        {
            //Error checking to make sure states are initialized correctly
            if (M_TimeForNextSwitch <= currentTime)
            {
                M_CurrentLevelInState++;
                if (M_States.Count > M_CurrentLevelInState)
                {
                    M_CurrentState = M_States[M_CurrentLevelInState].M_State;
                    M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;
                }
                else
                {
                    M_CurrentLevelInState = 0;
                    M_CurrentState = M_States[M_CurrentLevelInState].M_State;
                    M_TimeForNextSwitch = currentTime + M_States[M_CurrentLevelInState].M_TimeLength;
                }
            }
        }
    }
}