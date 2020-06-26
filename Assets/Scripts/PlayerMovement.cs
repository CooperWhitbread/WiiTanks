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
    [SerializeField] private Transform I_BodyTransform;
    [SerializeField] private Transform I_TurretTransform;
    [SerializeField] private Rigidbody2D I_MainRigidBody2D;

    ///Private Variables///
    private Vector2 m_PreviousBodyRoation = Vector2.zero;
    private ContactFilter2D m_ContactFilter;

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

        //Move Camera
        Camera.main.transform.SetPositionAndRotation(
            new Vector3 (transform.position.x, transform.position.y, Camera.main.transform.position.z), 
            Camera.main.transform.rotation);

        //Update variables for next frame
        //If there is no input from the player take position from the previous frame
        m_PreviousBodyRoation = moveDirection == Vector2.zero ? m_PreviousBodyRoation : moveDirection;
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
        Vector2 velocity = moveDirection;
        Vector2 normalsToRemove = Vector2.zero;

        if (moveDirection != Vector2.zero)
        {
            //Don't want to do any of this checking if the object is motionless
            float rotateDegree = Vector3.SignedAngle(Vector3.forward,
                   new Vector3(velocity.x, 0.0f, velocity.y),
                   Vector3.down);

            //Rotate it first
            Vector3 oldRotDir = I_BodyTransform.eulerAngles;
            float oldRotAng = GetComponent<Rigidbody2D>().rotation;
            GetComponent<Rigidbody2D>().rotation = rotateDegree;
            Debug.Log("RotatingBody");
            Debug.Log("MoveDir" + moveDirection.x + ", " + moveDirection.y);

            //check
            RaycastHit2D[] results = new RaycastHit2D[16];
            float distance = I_MoveSpeed;
            int numResults = GetComponent<Rigidbody2D>().Cast(velocity, m_ContactFilter, results, distance * Time.deltaTime + 0.01f);

            bool OKToMOve = false;
            Debug.Log(numResults + ": NumResults");

            //If collides, rotate back
            if (numResults != 0)
            {
                I_BodyTransform.eulerAngles = oldRotDir;
                Debug.Log("IsColliding");

                for (int i = 0; i < numResults; i++)
                {
                    RaycastHit2D[] results2 = new RaycastHit2D[16];

                    Vector2 toMoveDir = new Vector2(-results[i].normal.y, results[i].normal.x);
                    int numResults2 = GetComponent<Rigidbody2D>().Cast(toMoveDir, m_ContactFilter, results2, distance * Time.deltaTime + 0.01f);
                    Debug.Log("numResults2" + numResults2);
                    Debug.Log("NormalAdded" + results[i].normal.x + ", " + results[i].normal.y);
                    //normalsToRemove += results[i].normal;

                    if (numResults2 == 0)
                    {
                        //move
                        OKToMOve = true;
                    }
                    else
                    {
                        RaycastHit2D[] results3 = new RaycastHit2D[16];

                        Vector2 toMoveDir3 = new Vector2(results[i].normal.y, -results[i].normal.x);
                        int numResults3 = GetComponent<Rigidbody2D>().Cast(toMoveDir3, m_ContactFilter, results3, distance * Time.deltaTime + 0.01f);

                        Debug.Log("numResults3" + numResults3);
                        if (numResults3 == 0)
                        {
                            //move
                            OKToMOve = true;
                        }
                    }
                }
            }
            else
            {
                OKToMOve = true;
            }

            if (OKToMOve)
            {
                //Rotate body
                I_BodyTransform.eulerAngles = Vector3.forward * rotateDegree;
            }

            //Rotate Back
            GetComponent<Rigidbody2D>().rotation = oldRotAng;

            //move body
            Debug.Log("Normals To Remove: " + normalsToRemove.x + ", " + normalsToRemove.y);
            //Vector2 actualMove = velocity.normalized - new Vector2(Math.Abs(normalsToRemove.normalized.x), Math.Abs(normalsToRemove.normalized.y));
            //I_MainRigidBody2D.velocity = (actualMove.normalized + normalsToRemove.normalized) * distance; 
            I_MainRigidBody2D.velocity = velocity.normalized * distance;
        }
    }

}
