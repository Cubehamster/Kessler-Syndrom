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
    [SerializeField] private float intensity;
    private Material mat;

    private Vector2 velocityBeforePhysicsUpdate;
    public float hitpoints;

    private void Awake()
    {
        playOnce = true;
        debris = GetComponent<AudioSource>();
        mat = GetComponent<Renderer>().material;
        mat.EnableKeyword("_EMISSION");
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
        intensity = ((transform.gameObject.GetComponent<Rigidbody2D>().mass * 40.0f) - hitpoints) / (transform.gameObject.GetComponent<Rigidbody2D>().mass * 40.0f);
        if (intensity < 0)
        {
            intensity = 0f;
        }
        mat.SetColor("_EmissionColor", new Color(0.7f, 0.15f, 0.15f, 1.0f) * 0.5f * Mathf.Pow(2f, (-8f + 8f *intensity)));
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
