using UnityEngine;
using UnityEngine.Video;

public class TankCollider : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected Tank I_TankScript;

    ///Unity Functions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        I_TankScript.CollisionEnter(collision);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        I_TankScript.CollisionStay(collision);
    }
}
