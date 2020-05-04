using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [System.NonSerialized] public bool hasLanded = false;
    [System.NonSerialized] public bool hasCrashed = false;
    [System.NonSerialized] public bool refueling = false;
    public Rigidbody2D rocketRB;

    private float speed;
    private Vector3 velocityBeforePhysicsUpdate;
    float impactAngle = 0;

    private void OnCollisionStay2D(Collision2D other)
    {

        if (other.collider.tag == "Planet" || other.collider.tag == "Refuel")
        {
            Vector2 rocketVector = transform.up;
            Vector2 colliderVector = transform.position - other.transform.position;
            impactAngle = Mathf.Abs(Vector2.Angle(rocketVector, colliderVector));
        }
        float relativespeed = 0f;
        if (other.gameObject.GetComponent<Rigidbody2D>())
        {
            relativespeed = Vector2.Distance(other.gameObject.GetComponent<Rigidbody2D>().velocity, velocityBeforePhysicsUpdate);
            if (relativespeed > 0.55f)
            {
                hasCrashed = true;
            }
        }
        else if (speed < 0.55f && (other.collider.tag == "Planet" || other.collider.tag == "Refuel") && impactAngle < 93)
        {
            if(other.collider.tag == "Planet" || other.collider.tag == "Refuel")
            {
                hasLanded = true;
            }
            if (other.collider.tag == "Refuel")
            {
                refueling = true;
            }
        }
        else
        {
            hasCrashed = true;
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        hasLanded = false;
        refueling = false;
    }

    private void FixedUpdate()
    {
        velocityBeforePhysicsUpdate = transform.parent.gameObject.GetComponent<Rigidbody2D>().velocity;
        speed = rocketRB.velocity.magnitude;
    }
}

