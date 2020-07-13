using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour
{
    ///Inspector Variables///
    [SerializeField] private float I_MoveSpeed = 1.0f;
    [SerializeField] private Transform I_TurretTransform;
    [SerializeField] private Rigidbody2D I_MainRigidBody2D;

    ///Private Variables///
    private Vector2 m_PreviousBodyRoation = Vector2.zero;
    private ContactFilter2D m_ContactFilter;
    private float m_collissionBuffer = 0.1f;
    private float m_rotateSpeed = 15.0f;
    private float m_targetRotation = 0.0f;


    ///Unity Functions///
    private void Start()
    {
        //Contact filter initializeation
        m_ContactFilter.useTriggers = false;
        m_ContactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        m_ContactFilter.useLayerMask = true;
    }
    private void FixedUpdate()
    {
        //Set Up and reset
        I_MainRigidBody2D.velocity = Vector2.zero;
        Vector2 moveDirection = Vector2.zero;

        //Get the direction of movement from the player
        if (Input.GetButton("MoveLeft"))
        {
            moveDirection.x -= 1;
        }
        if (Input.GetButton("MoveRight"))
        {
            moveDirection.x += 1;
        }
        if (Input.GetButton("MoveDown"))
        {
            moveDirection.y -= 1;
        }
        if (Input.GetButton("MoveUp"))
        {
            moveDirection.y += 1;
        }

        //Move and rotate the tank
        MoveTank(moveDirection);
        RotateTurret();

        //Gradually rotate the tank
            //The angle that still needs to be made up
            float diff = m_targetRotation - I_MainRigidBody2D.rotation;

            if (diff > 180)
            {
                //The difference is greater that 180 so needs to be changed to rotate the shorter distance
                m_targetRotation = m_targetRotation - 360;

                //Recalculate the diff
                diff = m_targetRotation - I_MainRigidBody2D.rotation;
            }
            else if (diff < -180)
            {
                //The difference is greater that 180 so needs to be changed to rotate the shorter distance
                m_targetRotation = m_targetRotation + 360;

                //Recalculate the diff
                diff = m_targetRotation - I_MainRigidBody2D.rotation;
            }

            if (diff == 180 || diff == -180) //Don't want to be seen to rotate but still rotate it for logic
                I_MainRigidBody2D.SetRotation(m_targetRotation);
            else if (diff > m_rotateSpeed) //Want to rotate anticlockwise if larger than min rotation
                I_MainRigidBody2D.SetRotation(I_MainRigidBody2D.rotation + m_rotateSpeed); 
            else if (diff < -m_rotateSpeed) //Want to rotate clockwise if larger than min rotation
                I_MainRigidBody2D.SetRotation(I_MainRigidBody2D.rotation - m_rotateSpeed);
            else //Value is too small to do min rotation so rotation is done to exact value
                I_MainRigidBody2D.SetRotation(diff + I_MainRigidBody2D.rotation);

            //Fix the rotation to -180 <= r <= 180
            if (I_MainRigidBody2D.rotation > 180)
                I_MainRigidBody2D.SetRotation(I_MainRigidBody2D.rotation - 360);
            else if (I_MainRigidBody2D.rotation < -180)
                I_MainRigidBody2D.SetRotation(I_MainRigidBody2D.rotation + 360);
        
        //Move Camera
        Camera.main.transform.SetPositionAndRotation(
            new Vector3 (transform.position.x, transform.position.y, Camera.main.transform.position.z), 
            Camera.main.transform.rotation);

        //Update variables for next frame
        //If there is no input from the player take position from the previous frame
        m_PreviousBodyRoation = moveDirection == Vector2.zero ? m_PreviousBodyRoation : moveDirection;
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
        if (Math.Abs(I_MainRigidBody2D.rotation - rotateDegree1) < Math.Abs(I_MainRigidBody2D.rotation - rotateDegree2))
            m_targetRotation = rotateDegree1;
        else
            m_targetRotation = rotateDegree2;
    }

    ///Public Functions///
    public void RotateTurret()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.localPosition);
        Vector2 offset = new Vector2(mouse.x - screenPoint.x, mouse.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        I_TurretTransform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
    }

    ///Private Functions///
    private void MoveTank(Vector2 moveDirection)
    {
        if (moveDirection != Vector2.zero)
        {
            //Normalise the move direction
            moveDirection = moveDirection.normalized;


            //Don't want to do any of this checking if the object is motionless
            float rotateDegree = Vector3.SignedAngle(Vector3.forward,
                   new Vector3(moveDirection.x, 0.0f, moveDirection.y),
                   Vector3.down);

            if (CheckCollisionForRotation(rotateDegree, moveDirection, I_MoveSpeed * Time.deltaTime + m_collissionBuffer))
                m_targetRotation = rotateDegree;
            I_MainRigidBody2D.velocity = moveDirection.normalized * I_MoveSpeed;
        }
    }

    //Returns true if Ok to move
    private bool CheckCollisionForRotation(float rotation, float distance)
    {
        RaycastHit2D[] results = new RaycastHit2D[16];
        Vector2 moveDirection = Vector2.right * rotation;
        return CheckCollisionForRotation(rotation, moveDirection, distance, ref results) == 0 ? true : false;
    }
    private bool CheckCollisionForRotation(float rotation, Vector2 moveDirection, float distance)
    {
        RaycastHit2D[] results = new RaycastHit2D[16];
        return CheckCollisionForRotation(rotation, moveDirection, distance, ref results) == 0 ? true : false;
    }
    private int CheckCollisionForRotation(float rotation, Vector2 moveDirection, float distance, ref RaycastHit2D[] rayCastHit )
    {
        //Rotate it first
        float oldRotAng = GetComponent<Rigidbody2D>().rotation;
        I_MainRigidBody2D.MoveRotation(rotation);

        //Do the check
        int numResults = I_MainRigidBody2D.Cast(moveDirection, m_ContactFilter, rayCastHit, distance);

        //Rotate Back
        I_MainRigidBody2D.MoveRotation(oldRotAng);
        return numResults;
    }
    private float Dot(Vector2 vec1, Vector2 vec2) => vec1.x * vec2.x + vec1.y* vec2.y;

}
