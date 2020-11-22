using UnityEngine;

public class PlayerMovement : Tank
{
    /// Inspector Variables
    [SerializeField] protected BoxCollider2D I_CameraBoundsBox;
    [SerializeField] protected float m_rotateSpeed = 15.0f;

    ///Private Variable
    private Bomb[] m_Bombs = new Bomb[2];

    ///Virtual Functions///
    protected override void InheritedUpdate()
    {
        //Shoot check per frame
        if (Input.GetMouseButtonDown(0)) //Input.GetButtonDown("Shoot") if want space bar
        {
            Shoot();
        }
        if (Input.GetMouseButtonDown(1)) //Input.GetButtonDown("Shoot") if want space bar
        {
            DropBomb();
        }


        //Update the Camera
        MoveCamera();
    }
    protected override void InheritedFixedUpdate()
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
        RotateTurret(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        GradualMoveTank(moveDirection, m_rotateSpeed, 180.0f);

        /*
        //Fix the rotation to -180 <= r <= 180
        if (I_BodyRB2D.rotation > 180)
            I_BodyRB2D.SetRotation(I_BodyRB2D.rotation - 360);
        else if (I_BodyRB2D.rotation < -180)
            I_BodyRB2D.SetRotation(I_BodyRB2D.rotation + 360);*/
    }
    protected override void InheritedStart() 
    {
    }

    ///Public Variables
    void MoveCamera()
    {
        //Get the left bottom corner and the top right corner of the map
        Vector3 p0 = I_CameraBoundsBox.bounds.center - I_CameraBoundsBox.bounds.size / 2;
        Vector3 p1 = I_CameraBoundsBox.bounds.center + I_CameraBoundsBox.bounds.size / 2;
        
        //Get Width and height of camera screen in units
        float height = 2 * Camera.main.orthographicSize;
        float width = height * Camera.main.aspect;

        //Set the camera's position 
        Vector3 pos = new Vector3(I_BodyRB2D.position.x, I_BodyRB2D.position.y, Camera.main.transform.position.z);
        pos.x = Mathf.Clamp(pos.x, p0.x + width / 2, p1.x - width / 2);
        pos.y = Mathf.Clamp(pos.y, p0.y + height / 2, p1.y - height / 2);

        //Update the camera
        Camera.main.transform.SetPositionAndRotation(pos, Camera.main.transform.rotation);
    }

}