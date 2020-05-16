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
    [SerializeField] private Material mat;

    private Vector2 velocityBeforePhysicsUpdate;
    public float hitpoints;
    public float hpSizePower = 0.9f;

    private void Awake()
    {
        playOnce = true;
        debris = GetComponent<AudioSource>();
        if(GetComponent<Renderer>() !=null)
        {
            mat = GetComponent<Renderer>().material;
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            for (int n = 0; n < transform.childCount; n++)
            {
                if (transform.GetChild(n).gameObject.tag == "Untagged")
                {
                    mat = transform.GetChild(n).gameObject.GetComponent<Renderer>().material;
                    mat.EnableKeyword("_EMISSION");
                }

            }
        }

        for (int n = 0; n < transform.childCount; n++)
        {
            if (transform.GetChild(n).gameObject.tag == "Shockwave")
            {
                impactExplosion = transform.GetChild(n).gameObject;
                impactExplosion.GetComponent<PointEffector2D>().forceMagnitude = transform.gameObject.GetComponent<Rigidbody2D>().mass * 0.2f;
                impactExplosion.GetComponent<PointEffector2D>().forceVariation = transform.gameObject.GetComponent<Rigidbody2D>().mass * 0.2f;
                impactExplosion.SetActive(false);
            }

        }
    }

    private void FixedUpdate()
    {
        velocityBeforePhysicsUpdate = transform.gameObject.GetComponent<Rigidbody2D>().velocity;
        if (triggerDestruction)
        {
            StartCoroutine(DestroyObject());
        }
        intensity = ((Mathf.Pow((transform.gameObject.GetComponent<Rigidbody2D>().mass * 80f), hpSizePower)) - hitpoints) / (Mathf.Pow((transform.gameObject.GetComponent<Rigidbody2D>().mass * 80f), hpSizePower));
        if (intensity < 0)
        {
            intensity = 0f;
        }
        mat.SetColor("_EmissionColor", new Color(0.7f, 0.15f, 0.15f, 1.0f) * 0.5f * Mathf.Pow(2f, (-7f + 9f * intensity)));
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
            hitpoints -= 8f * Mathf.Pow(relativespeed, 3f) * other.gameObject.GetComponent<Rigidbody2D>().mass / (transform.gameObject.GetComponent<Rigidbody2D>().mass + other.gameObject.GetComponent<Rigidbody2D>().mass);
        }
        else
        {
            relativespeed = Vector2.Distance(velocityBeforePhysicsUpdate, new Vector2(0f, 0f));
            hitpoints -= 4f * Mathf.Pow(velocityBeforePhysicsUpdate.magnitude, 3f);
        }

    }
}
