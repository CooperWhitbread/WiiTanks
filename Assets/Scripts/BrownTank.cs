using UnityEngine;

public class BrownTank : EnemyTank
{
    ///Private Variables
    private const int m_MaxTurretUpdateDelay = 30;
    private Rigidbody2D m_PlayerRB;

    ///Virtual Functions
    protected override void InheritedStartEnemy()
    {
        m_Bullets = new Bullet[1]; 
        m_Bombs = new Bomb[0];
        m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
        m_PlayerRB = GlobalVariables.GetPlayerTankBody();
    }
    protected override void InheritedFixedUpdateEnemy()
    {
        if (GetDiffInRotation(m_DesiredTurretRotation, m_TurretRB2D.rotation) < 5)
        {
            m_DelayTurretUpdate++;
            if (m_DelayTurretUpdate >= m_MaxTurretUpdateDelay)
            {
                m_DesiredTurretRotation = Random.Range(-180.0f, 180.0f);
                m_DelayTurretUpdate = 0;
            }
        }

        m_BodyRB2D.rotation = 0.0f;
    }
    protected override bool CanShoot()
    {
        return GenericCanShoot();
    }
}
