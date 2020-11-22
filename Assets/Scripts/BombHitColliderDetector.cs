using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombHitColliderDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GetComponentInParent<Bomb>().HitColliderCall(collision.gameObject);
    }
}