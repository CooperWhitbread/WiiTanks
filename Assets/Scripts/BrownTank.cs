using UnityEngine;


public class BrownTank : EnemyTank
{
    ///Inspector Variables
    [SerializeField] Rigidbody2D I_PlayerRB;

    ///Private Variables
    private float m_DesiredTurretRotation = 0.0f;
    private int m_DelayTurretUpdate = 0;
    private int m_MaxTurretUpdateDelay = 20;

    ///Virtual Functions
    protected override void InheritedFixedUpdate()
    {
        //Movement and rotation
        UpdateTurret();

        //Shooting
        AutomaticShoot();
        if (m_HasShot)
        {
            SetNextShootTime(I_MinTimeToShoot, I_MaxTimeToShoot);
        }
    }
    protected override void InheritedStart()
    {
        m_Bullets = new Bullet[5]; 
        m_Bombs = new Bomb[2];
        m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
    }
    protected override bool CanShoot()
    {
        return true;
    }

    ///Private Functions
    private void UpdateTurret()
    {
        GradualRotation(ref I_TurretRB2D, m_DesiredTurretRotation, I_TurretRotationSpeed);

        if (Mathf.Abs(I_TurretRB2D.rotation - m_DesiredTurretRotation) < 0.5f)
        {
            m_DelayTurretUpdate++;
            if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
            {
                m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
                m_DelayTurretUpdate = 0;
            }
        }
    }
}
