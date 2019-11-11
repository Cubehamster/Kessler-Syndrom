using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionImpactSound : MonoBehaviour
{
    public AudioSource debris;
    public bool playOnce = true;
    public GameObject impactExplosion;
    public bool triggerDestruction = false;
    public bool destructionComplete = false;

    private Vector2 velocityBeforePhysicsUpdate;
    public float hitpoints;

    private void Awake()
    {
        playOnce = true;
        debris = GetComponent<AudioSource>();
        if (transform.childCount > 0)
        {
            impactExplosion = transform.GetChild(0).gameObject;
        }
    }

    private void FixedUpdate()
    {
        velocityBeforePhysicsUpdate = transform.gameObject.GetComponent<Rigidbody2D>().velocity;
        if (triggerDestruction)
        {
            StartCoroutine(DestroyObject());
        }
    }

    IEnumerator DestroyObject()
    {
        yield return new WaitForSeconds(3f);
        destructionComplete = true;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        float relativespeed = 0f;

        if (other.gameObject.GetComponent<Rigidbody2D>())
        {
            relativespeed = Vector2.Distance(other.gameObject.GetComponent<Rigidbody2D>().velocity, velocityBeforePhysicsUpdate);
        }
        else
        {
            relativespeed = Vector2.Distance(velocityBeforePhysicsUpdate, new Vector2(0f, 0f));
        }
        hitpoints -= 4f * Mathf.Pow(relativespeed,4f);
    }
}
