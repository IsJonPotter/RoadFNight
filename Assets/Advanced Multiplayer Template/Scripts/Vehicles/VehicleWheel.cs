using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleWheel : MonoBehaviour
{
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == 1 || collision.gameObject.tag == "Player" & collision.gameObject.layer == 6)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
        }
    }
}
