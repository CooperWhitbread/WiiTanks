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

    ///Unity Functions
    public void Awake()
    {
        m_SpriteRenderer = transform.GetComponent<SpriteRenderer>();
        m_RigidBody2D = transform.GetComponent<Rigidbody2D>();
    }
    public void FixedUpdate()
    {
        m_RigidBody2D.velocity = new Vector2(transform.up.x, transform.up.y) *  m_Velocity;
        //Debug.Log(transform.up);
    }
    private void OnCollisionEnter2D(Collision2D col)
    {
        //Check Collision Type
        if (col.gameObject.layer == LayerMask.NameToLayer("Tanks") ||
            col.gameObject.layer == LayerMask.NameToLayer("Bullets"))
        {
            GetComponentInParent<Tank>().DestroyBullet(m_NumberInArray);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (m_FirstHit)
            {
                Vector2 post = transform.up; //Origion Direction
                Vector2 normal = col.contacts[0].normal; //Wall's normal
                Vector2 ang = post - (2 * Vector3.Dot(post, normal) * normal); //vector of desired direction
                m_RigidBody2D.SetRotation(Vector2.SignedAngle(Vector2.up, ang));
                m_FirstHit = false;
            }
            else
            {
                GetComponentInParent<Tank>().DestroyBullet(m_NumberInArray);
            }
        }
    }

    ///Public Functions
    public void Initialize(BulletScript objectScript, int numberInArray, Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        m_SpriteRenderer.sprite = objectScript.Sprite;
        m_Velocity = objectScript.Velocity;
        m_NumberInArray = numberInArray;
        m_FirstHit = true;
        //Adjust Bullet
        transform.position += transform.forward * transform.position.y / 2;
    }
}
