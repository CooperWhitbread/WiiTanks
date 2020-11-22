using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    ///Private Variables
    private SpriteRenderer m_SpriteRenderer;
    private Rigidbody2D m_RigidBody2D;
    private float m_Velocity = 0.0f;
    private int m_NumberInArray = -1;
    private bool m_FirstHit = true;
    private float m_TimeForWeirdBulletWallCheck = 0.0f;


    ///Unity Functions
    void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_RigidBody2D = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    { 
        m_RigidBody2D.velocity = new Vector2(Mathf.Cos(Mathf.Deg2Rad * m_RigidBody2D.rotation), Mathf.Sin(Mathf.Deg2Rad * m_RigidBody2D.rotation)) *  m_Velocity;
        //Debug.Log(transform.up);

        //Check if bullet is going slowly against the wall
        if (Time.fixedTime <= m_TimeForWeirdBulletWallCheck)//Haven't tested, if problem doesn't turn up, all good
        {
            m_TimeForWeirdBulletWallCheck = Time.fixedTime + 0.5f;
            if (new Vector2(Mathf.Cos(Mathf.Deg2Rad * m_RigidBody2D.rotation), Mathf.Sin(Mathf.Deg2Rad * m_RigidBody2D.rotation)) != 
                new Vector2 (transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y))
            {
                DestroyBullet();
            }
        }
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        //Check Collision Type
        if (col.gameObject.layer == LayerMask.NameToLayer("Tanks") ||
            col.gameObject.layer == LayerMask.NameToLayer("Bullets"))
        {
            DestroyBullet();
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (m_FirstHit)
            {
                Vector2 post = transform.right; //Origion Direction
                Vector2 normal = col.contacts[0].normal; //Wall's normal
                Vector2 ang = post - (2 * Vector3.Dot(post, normal) * normal); //vector of desired direction
                m_RigidBody2D.SetRotation(Vector2.SignedAngle(Vector2.right, ang));
                m_FirstHit = false;
            }
            else
            {
                DestroyBullet();
            }
        }
    }

    ///Public Functions
    public void DestroyBullet()
    {
        if (GetComponentInParent<Tank>())
        {
            GetComponentInParent<Tank>().DestroyBullet(m_NumberInArray);
        }
        else
        {
            GetComponentInParent<GlobalVariables>().DestroyBullet(m_NumberInArray);
        }
    }
    public void Initialize(BulletScript objectScript, int numberInArray, Vector3 position, float rotation)
    {
        Vector3 rot = new Vector3(0.0f, 0.0f, rotation);
        transform.eulerAngles = rot;
        transform.position = position;
        m_SpriteRenderer.sprite = objectScript.Sprite;
        m_Velocity = objectScript.Velocity;
        m_NumberInArray = numberInArray;
        m_FirstHit = true;
        m_TimeForWeirdBulletWallCheck = 0.0f;
}

    public void SetLevelInArray(int level)
    {
        m_NumberInArray = level;
    }
}
