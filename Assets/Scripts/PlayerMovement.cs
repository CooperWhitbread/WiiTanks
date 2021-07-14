using UnityEngine;

public class PlayerMovement : Tank
{
    /// Inspector Variables
    [SerializeField] protected float I_RotateSpeed = 6.0f;
    [SerializeField] protected bool I_IsFullGameInView = true;

    ///Virtual Functions///
    protected override void InheritedUpdate()
    {
        //Input.GetButtonDown("Shoot") if want space bar

        //Shoot check per frame
        if (Input.GetMouseButtonDown(0))
            if (CanShoot())
                Shoot();
        if (Input.GetMouseButtonDown(1))
            DropBomb();

        //Update the Camera
        MoveCamera();
    }
    protected override void InheritedFixedUpdate()
    {
        //Set Up and reset
        Vector2 moveDirection = Vector2.zero;

        //Get the direction of movement from the player
        if (Input.GetButton("MoveLeft"))
            moveDirection.x -= 1;
        if (Input.GetButton("MoveRight"))
            moveDirection.x += 1;
        if (Input.GetButton("MoveDown"))
            moveDirection.y -= 1;
        if (Input.GetButton("MoveUp"))
            moveDirection.y += 1;

        //Move and rotate the tank
        RotateTurret(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        GradualMoveTank(moveDirection, I_RotateSpeed, 180.0f);
    }
    protected override void InheritedStart()
    {
        m_Bullets = new Bullet[5];
        m_Bombs = new Bomb[2];
    }
    protected bool CanShoot()
    {
        //Checks if there are no walls between turret and body
        RaycastHit2D hits = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(180 + m_TurretRB2D.rotation), 2.0f);
        if (hits.collider != null)
        {
            if (hits.collider.gameObject != m_BodyRB2D.gameObject && hits.collider.gameObject.layer != GlobalVariables.LayerWallHole &&
                hits.collider.gameObject.layer != GlobalVariables.LayerBullets)
                return false;
        }
        return true;
    }

    ///Public Variables
    void MoveCamera()
    {
        if (I_IsFullGameInView)
        {
            Bounds cB = GlobalVariables.GetThisInstance().GetCameraBounds();
            float screenRatio = (float)Screen.width / (float)Screen.height;
            float targetRatio = cB.size.x / cB.size.y;

            if (screenRatio >= targetRatio)
                Camera.main.orthographicSize = cB.size.y / 2;
            else
                Camera.main.orthographicSize = cB.size.y / 2 * targetRatio / screenRatio;

            Camera.main.transform.SetPositionAndRotation(Vector3.forward * -10 + (Vector3) GlobalVariables.GetThisInstance().GetCameraCenter(), Camera.main.transform.rotation);
        }
        else
        {
            //Get the left bottom corner and the top right corner of the map
            Vector3 p0 = GlobalVariables.GetThisInstance().GetCamerBoundsBottomLeft();
            Vector3 p1 = GlobalVariables.GetThisInstance().GetCamerBoundsTopRight();

            //Get Width and height of camera screen in units
            float height = 2 * Camera.main.orthographicSize;
            float width = height * Camera.main.aspect;

            //Set the camera's position 
            Vector3 pos = new Vector3(Mathf.Clamp(m_BodyRB2D.position.x, p0.x + width / 2, p1.x - width / 2), 
                Mathf.Clamp(m_BodyRB2D.position.y, p0.y + height / 2, p1.y - height / 2), 
                Camera.main.transform.position.z);

            //Update the camera
            Camera.main.transform.SetPositionAndRotation(pos, Camera.main.transform.rotation);
        }
    }

}