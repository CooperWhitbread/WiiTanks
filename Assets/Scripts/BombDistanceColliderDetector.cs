using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombDistanceColliderDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GetComponentInParent<Bomb>().DistanceColliderCall(collision.gameObject.layer);
    }
}
