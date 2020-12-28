using UnityEngine;
using UnityEngine.Video;

public class TankCollider : MonoBehaviour
{
    ///Unity Functions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GetComponentInParent<Tank>().CollisionEnter(collision);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        GetComponentInParent<Tank>().CollisionStay(collision);
    }
}
