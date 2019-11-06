﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [System.NonSerialized] public bool hasLanded = false;
    [System.NonSerialized] public bool hasCrashed = false;
    [System.NonSerialized] public bool refueling = false;
    public Rigidbody2D rocketRB;

    private float speed;

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("Poof");
        if (speed < 0.45f && (other.collider.tag == "Planet" || other.collider.tag == "Refuel"))
        {
            Debug.Log("Poof2");
            hasLanded = true;
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
        speed = rocketRB.velocity.magnitude;
    }
}

