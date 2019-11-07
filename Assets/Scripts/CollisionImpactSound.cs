using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionImpactSound : MonoBehaviour
{
    private AudioSource debris;
    private bool playOnce = true;
    private GameObject impactExplosion;

    public float hitpoints;

    private void Start()
    {
        debris = GetComponent<AudioSource>();
        if (transform.GetChild(0).gameObject != null)
        {
            impactExplosion = transform.GetChild(0).gameObject;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        {
            if(playOnce && impactExplosion != null)
            {
                playOnce = false;
                debris.Play();
                impactExplosion.SetActive(true);
            }
        }
    }

    //private void FixedUpdate()
  //  {
   //     if(hitpoints <= 0)
   //     {
   //         Destroy(transform.gameObject);
    //    }
  //  }
}
