using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour
{
    ///////////////////////
    //Inspector Variables//
    ///////////////////////
    [SerializeField] private float I_MoveSpeed = 1.0f;
    [SerializeField] private Transform I_BodyTransform;
    [SerializeField] private Transform I_TurretTransform;
    [SerializeField] private Rigidbody2D I_MainRigidBody2D;

    private Vector3 m_Velocity = Vector3.zero;
    private Vector2 m_PreviousBodyRoation = Vector2.zero;

    ////////////////////
    //Public Functions//
    ////////////////////
    private void Update()
    {
        Vector2 moveDirection = Vector2.zero;

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

        //Rotating
        if (moveDirection == Vector2.zero)
        {
            RotateBody(m_PreviousBodyRoation);
        }
        else
        {
            RotateBody(moveDirection);
        }
        RotateTurret();
        
        //Move Tank
        I_MainRigidBody2D.velocity = Vector3.SmoothDamp(I_MainRigidBody2D.velocity, moveDirection * I_MoveSpeed, ref m_Velocity, 0.05f);

        //Move Camera
        Camera.main.transform.SetPositionAndRotation(
            new Vector3 (transform.position.x, transform.position.y, Camera.main.transform.position.z), 
            Camera.main.transform.rotation);

        m_PreviousBodyRoation = moveDirection == Vector2.zero ? m_PreviousBodyRoation : moveDirection;
    }

    ////////////////////
    //Public Functions//
    ////////////////////

    public void RotateBody(Vector2 rotationDirection)
    {
        //Body
        float rotateDegree = Vector3.SignedAngle(Vector3.forward, 
            new Vector3(rotationDirection.x, 0.0f, rotationDirection.y), 
            Vector3.down);
        I_BodyTransform.eulerAngles = Vector3.forward * rotateDegree;
    }

    public void RotateTurret()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.localPosition);
        Vector2 offset = new Vector2(mouse.x - screenPoint.x, mouse.y - screenPoint.y);
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        I_TurretTransform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
    }
}
