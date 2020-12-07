using UnityEngine;


public class BrownTank : EnemyTank
{
    ///Inspector Variables
    [SerializeField] float I_MaxShootDistanceForPlayerTank = 30.0f;

    ///Private Variables
    private float m_DesiredTurretRotation = 0.0f;
    private int m_DelayTurretUpdate = 0;
    private int m_MaxTurretUpdateDelay = 20;
    private Rigidbody2D m_PlayerRB;

    ///Virtual Functions
    protected override void InheritedFixedUpdate()
    {
        //Movement and rotation
        UpdateTurret();

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);
    }
    protected override void InheritedStart()
    {
        m_Bullets = new Bullet[1]; 
        m_Bombs = new Bomb[0];
        m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
        m_PlayerRB = GlobalVariables.GetPlayerTankBody();
    }
    protected override bool CanShoot()
    {
        //Basic Raycast for hitting self check before advanced check
        if (CheckIfGoingToHitSelf(m_Bullets[0].GetComponent<CapsuleCollider2D>()))
            return false;

        //Check if it is in a position that it wants to shoot
        RaycastHit2D rayCastHit = Physics2D.Raycast(I_ShootTransform.position, GetVector2FromAngle(m_TurretRB2D.rotation),
               100.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls | 1 << GlobalVariables.LayerBullets);
        if (rayCastHit.collider != null)
        {
            switch (rayCastHit.collider.gameObject.layer)
            {
                case GlobalVariables.LayerBullets:
                    if (rayCastHit.collider.gameObject.name.Contains("Bomb"))
                        return false; //Is a bomb
                    if (Vector2.Distance(rayCastHit.point, m_BodyRB2D.position) <= 1.0f)
                        return true; //Is a bullet
                    break;

                case GlobalVariables.LayerTanks:
                    if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                        return true; // looking at player tank
                    return false; //looking at an ally tank

                case GlobalVariables.LayerWalls:
                    //Check Near Player
                    if (GetAngleFromVector2(m_PlayerRB.position - m_BodyRB2D.position) - m_TurretRB2D.rotation <= I_MaxShootDistanceForPlayerTank &&
                        GetAngleFromVector2(m_PlayerRB.position - m_BodyRB2D.position) - m_TurretRB2D.rotation >= -I_MaxShootDistanceForPlayerTank)
                    {
                        RaycastHit2D rch = Physics2D.Raycast(I_ShootTransform.position, m_PlayerRB.position - m_BodyRB2D.position);
                        if (rch.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                            return true;
                    }

                    //Check for the rebound hit
                    Vector2 post = GetVector2FromAngle(m_TurretRB2D.rotation); //Origion Direction
                    Vector2 normal = rayCastHit.normal; //Wall's normal
                    Vector2 ang = post - (2 * Vector2.Dot(post, normal) * normal); //vector of desired direction
                    Vector2 hit = rayCastHit.point; // the point of contact
                    rayCastHit = Physics2D.Raycast(hit, ang,
                        100.0f, 1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerWalls);//Don't do bullets since they will have moved by then

                    if (rayCastHit.collider.gameObject.layer == GlobalVariables.LayerTanks)
                    {
                        if (rayCastHit.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                            return true; // looking at player tank
                        return false; //looking at an ally tank
                    }

                    //Check for near player tank
                    //if (Vector2.Angle(I_PlayerRB.position - hit, ang) <= I_MaxShootDistanceForPlayerTank)
                    if (GetAngleFromVector2(m_PlayerRB.position - hit) - GetAngleFromVector2(ang) <= I_MaxShootDistanceForPlayerTank &&
                        GetAngleFromVector2(m_PlayerRB.position - hit) - GetAngleFromVector2(ang) >= -I_MaxShootDistanceForPlayerTank)
                    {
                        if (Vector2.SignedAngle(m_PlayerRB.position - hit, rayCastHit.normal) >= 0 && Vector2.SignedAngle(ang, rayCastHit.normal) >= 0 ||
                            Vector2.SignedAngle(m_PlayerRB.position - hit, rayCastHit.normal) <= 0 && Vector2.SignedAngle(ang, rayCastHit.normal) <= 0)
                        {
                            RaycastHit2D rch = Physics2D.Raycast(I_ShootTransform.position, m_PlayerRB.position - hit);
                            if (rch.collider.gameObject.name == GlobalVariables.PlayerTankBodyName)
                                return true;
                        }
                    }

                    break;
            }
            return false;//Hit things but nothing important
        }
        return true; //hit nothing
    }

    ///Private Functions
    private void UpdateTurret()
    {
        GradualRotation(ref m_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);

        if (Mathf.Abs(m_TurretRB2D.rotation - m_DesiredTurretRotation) < 0.5f)
        {
            m_DelayTurretUpdate++;
            if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
            {
                m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
                m_DelayTurretUpdate = 0;
            }
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawLine(I_ShootTransform.position, tempPos);
        //CheckIfGoingToHitSelf(m_Bullets[0].GetComponent<CapsuleCollider2D>());
    }
}
