using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    ///Private Variables
    private SpriteRenderer m_SpriteRenderer;
    private ParticleSystem m_ParticleSystem;
    private CapsuleCollider2D m_Collider;
    private Rigidbody2D m_RigidBody2D;
    private float m_Velocity = 0.0f;
    private int m_NumberInArray = -1;
    private int m_NumberOfHit = 0;
    private int m_MaxNumberOfHits = 0;
    private float m_TimeForWeirdBulletWallCheck = 0.0f;
    private Vector2 m_PreviousPos = Vector2.zero;
    private float m_CensecutiveWeirdBulletWallCheck = 0.0f;

    ///Unity Functions
    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_RigidBody2D = GetComponent<Rigidbody2D>();
        m_ParticleSystem = GetComponentInChildren<ParticleSystem>();
        m_Collider = GetComponentInChildren<CapsuleCollider2D>();
    }
    void FixedUpdate()
    { 
        m_RigidBody2D.velocity = new Vector2(Mathf.Cos(Mathf.Deg2Rad * m_RigidBody2D.rotation), Mathf.Sin(Mathf.Deg2Rad * m_RigidBody2D.rotation)) *  m_Velocity;

        //Check if bullet is going slowly against the wall
        if (Time.fixedTime >= m_TimeForWeirdBulletWallCheck)
        {
            m_TimeForWeirdBulletWallCheck = Time.fixedTime + 0.1f;
            if (Mathf.Abs(Tank.GetAngleFromVector2(m_RigidBody2D.position - m_PreviousPos) - m_RigidBody2D.rotation) >= 5.0f)
            {
                m_CensecutiveWeirdBulletWallCheck++;

                if (m_CensecutiveWeirdBulletWallCheck == 2)
                    DestroyBullet();
            }
        }

        //Destory the bullet once particle system is gone
        if (!m_ParticleSystem.IsAlive())
        {
            if (GetComponentInParent<Tank>())
                GetComponentInParent<Tank>().DestroyBullet(m_NumberInArray);
            else
                GetComponentInParent<GlobalVariables>().DestroyBullet(m_NumberInArray);
        }

        m_PreviousPos = m_RigidBody2D.position;
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        //Check Collision Type
        if (col.gameObject.layer == LayerMask.NameToLayer("Tanks") ||
            col.gameObject.layer == LayerMask.NameToLayer("Bullets"))
            DestroyBullet();
        else if (col.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (m_NumberOfHit < m_MaxNumberOfHits)
            {
                Vector2 post = transform.right; //Origion Direction
                Vector2 normal = col.contacts[0].normal; //Wall's normal
                Vector2 ang = post - (2 * Vector3.Dot(post, normal) * normal); //vector of desired direction
                m_RigidBody2D.SetRotation(Vector2.SignedAngle(Vector2.right, ang));
                m_NumberOfHit++;
            }
            else 
                DestroyBullet();
        }
    }

    ///Public Functions
    public void DestroyBullet()
    {
        //Particle System Cancel 
        foreach (ParticleSystem p in GetComponentsInChildren<ParticleSystem>())
        {
            if (p == m_ParticleSystem)
                m_ParticleSystem.Stop();
            else
            {
                p.Clear();
                p.Stop();
            }

        }

        m_SpriteRenderer.enabled = false;
        m_Collider.enabled = false;
    }
    public void Initialize(BulletScript objectScript, int numberInArray, Vector3 position, float rotation, int maxNumberOfHits)
    {
        Vector3 rot = new Vector3(0.0f, 0.0f, rotation);
        transform.eulerAngles = rot;
        transform.position = position;
        m_SpriteRenderer.sprite = objectScript.Sprite;
        m_Velocity = objectScript.Velocity;
        m_NumberInArray = numberInArray;
        m_MaxNumberOfHits = maxNumberOfHits;
        m_NumberOfHit = 0;
        foreach (ParticleSystem p in GetComponentsInChildren<ParticleSystem>())
            p.Play();
        m_SpriteRenderer.enabled = true;
        m_Collider.enabled = true; 
        m_TimeForWeirdBulletWallCheck = Time.fixedTime + 0.3f;
        m_CensecutiveWeirdBulletWallCheck = 0;
    }
    public void SetLevelInArray(int level)
    {
        m_NumberInArray = level;
    }
}
