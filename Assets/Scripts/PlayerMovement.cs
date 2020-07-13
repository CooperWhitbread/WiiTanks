using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;

public class PlayerMovement : Tank
{
    ///Unity Functions///
    protected override void InheritedUpdate()
    {
        //Set Up and reset
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
        float diff = m_targetRotation - m_RigidBody2D.rotation;

        if (diff > 180)
        {
            //The difference is greater that 180 so needs to be changed to rotate the shorter distance
            m_targetRotation = m_targetRotation - 360;

            //Recalculate the diff
            diff = m_targetRotation - m_RigidBody2D.rotation;
        }
        else if (diff < -180)
        {
            //The difference is greater that 180 so needs to be changed to rotate the shorter distance
            m_targetRotation = m_targetRotation + 360;

            //Recalculate the diff
            diff = m_targetRotation - m_RigidBody2D.rotation;
        }

        if (diff == 180 || diff == -180) //Don't want to be seen to rotate but still rotate it for logic
            m_RigidBody2D.SetRotation(m_targetRotation);
        else if (diff > m_rotateSpeed) //Want to rotate anticlockwise if larger than min rotation
            m_RigidBody2D.SetRotation(m_RigidBody2D.rotation + m_rotateSpeed); 
        else if (diff < -m_rotateSpeed) //Want to rotate clockwise if larger than min rotation
            m_RigidBody2D.SetRotation(m_RigidBody2D.rotation - m_rotateSpeed);
        else //Value is too small to do min rotation so rotation is done to exact value
            m_RigidBody2D.SetRotation(diff + m_RigidBody2D.rotation);

        //Fix the rotation to -180 <= r <= 180
        if (m_RigidBody2D.rotation > 180)
            m_RigidBody2D.SetRotation(m_RigidBody2D.rotation - 360);
        else if (m_RigidBody2D.rotation < -180)
            m_RigidBody2D.SetRotation(m_RigidBody2D.rotation + 360);


        //Move Camera
        Camera.main.transform.SetPositionAndRotation(
            new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z),
            Camera.main.transform.rotation);

        //Update variables for next frame
        //If there is no input from the player take position from the previous frame
        m_PreviousBodyRoation = moveDirection == Vector2.zero ? m_PreviousBodyRoation : moveDirection;
    }

}
