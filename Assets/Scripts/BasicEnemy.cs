using UnityEngine;
using UnityEngine.Animations;

public class BasicEnemy : EnemyTank
{
    [SerializeField] Rigidbody2D I_PlayerRB;
    ///Private Variables
    private Vector2 m_MoveTo = Vector2.zero;

    ///Virtual Functions
    protected override void InheritedStart()
    {
        m_MoveTo = new Vector2(Random.Range(-20.0f, 20.0f), Random.Range(-20.0f, 20.0f));
    }
    protected override void InheritedFixedUpdate()
    {
        Vector2 move = m_MoveTo - I_BodyRB2D.position;
        MoveTank(move.normalized);
        I_BodyRB2D.SetRotation(GetAngleFromVector2(move.normalized));
        UpdateTurret();
    }
    protected override void InheritedOnCollisionStay(Collision2D collision)
    {
        //Debug.Log(collision.gameObject + ": hit: " + gameObject);
        if (collision.gameObject.layer == GlobalVariables.LayerWalls)
        {
            m_MoveTo = new Vector2(Random.Range(-20.0f, 20.0f), Random.Range(-20.0f, 20.0f));
        }

    }

    ///Private Functions
    private void UpdateTurret()
    {
        if (I_PlayerRB.position.x - transform.position.x < 0)
        {
            GradualRotation(ref I_TurretRB2D, GetAngleFromVector2(I_PlayerRB.position - I_BodyRB2D.position), I_TurretRB2D.rotation, I_TurretRotationSpeed);
        }
        else
        {
            GradualRotation(ref I_TurretRB2D, GetAngleFromVector2(I_PlayerRB.position - I_BodyRB2D.position), I_TurretRB2D.rotation, I_TurretRotationSpeed);
        }
    }
}
