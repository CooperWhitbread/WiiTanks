using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



///Abstract Tank Class
abstract public class Tank : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected BulletScript I_BulletScript;
    [SerializeField] protected Bullet I_BulletPrefab;
    [SerializeField] protected Transform I_TurretTransform;
    [SerializeField] protected Transform I_ShootTransform;
    [SerializeField] protected float I_MoveSpeed = 1.0f;
    [SerializeField] protected float I_StopDelayForShoot = 0.1f;

    ///Protected Variables
    protected Rigidbody2D m_RigidBody2D;
    static protected ContactFilter2D s_ContactFilter = new ContactFilter2D();
    protected float m_targetRotation = 0.0f;
    protected Vector2 m_PreviousBodyRoation = Vector2.zero;
    protected Bullet[] m_Bullets = new Bullet[5];
    protected int m_NumOfBullets = 0;
    protected bool m_ShootDelayActive = false;
    protected float m_DesiredTimeForShoot = 0.0f;

    ///Constant Variables
    protected const float m_collissionBuffer = 0.1f;
    protected const float m_rotateSpeed = 15.0f;

    ///Inherited Function
    protected virtual void InheritedFixedUpdate() { }
    protected virtual void InheritedUpdate() { }

    ///Unity Functions
    public void Start()
    {
        m_RigidBody2D = transform.GetComponent<Rigidbody2D>();

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
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        float rotateDegree1 = Vector3.SignedAngle(Vector3.forward,
            new Vector3(-collision.contacts[0].normal.y, 0.0f, collision.contacts[0].normal.x),
            Vector3.down);

        float rotateDegree2 = Vector3.SignedAngle(Vector3.forward,
            new Vector3(collision.contacts[0].normal.y, 0.0f, -collision.contacts[0].normal.x),
            Vector3.down);

        //Want the smallest change
        if (Mathf.Abs(m_RigidBody2D.rotation - rotateDegree1) < Mathf.Abs(m_RigidBody2D.rotation - rotateDegree2))
            m_targetRotation = rotateDegree1;
        else
            m_targetRotation = rotateDegree2;
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

    ///Private Functions
    private void ConstantUpdatePre()
    {
        //Set Up and reset
        m_RigidBody2D.velocity = Vector2.zero;
        DelayShoot(false);
    }
    private void ConstantUpdatePost()
    {
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
                float rotateDegree = Vector3.SignedAngle(Vector3.forward,
                       new Vector3(moveDirection.x, 0.0f, moveDirection.y),
                       Vector3.down);

                //Check Rotation Fine
                if (CheckCollisionForRotation(rotateDegree, moveDirection, I_MoveSpeed * Time.deltaTime + m_collissionBuffer))
                    m_targetRotation = rotateDegree;
                m_RigidBody2D.velocity = moveDirection.normalized * I_MoveSpeed;
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
    protected void RotateTurret()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.localPosition);
        Vector2 offset = new Vector2(mouse.x - screenPoint.x, mouse.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        I_TurretTransform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
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
        float oldRotAng = GetComponent<Rigidbody2D>().rotation;
        m_RigidBody2D.MoveRotation(rotation);

        //Do the check
        int numResults = m_RigidBody2D.Cast(moveDirection, s_ContactFilter, rayCastHit, distance);

        //Rotate Back
        m_RigidBody2D.MoveRotation(oldRotAng);
        return numResults;
    }
    protected float Dot(Vector2 vec1, Vector2 vec2) => vec1.x * vec2.x + vec1.y * vec2.y;
}
