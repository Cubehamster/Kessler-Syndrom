using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthTracker : MonoBehaviour
{
    public Transform earth;
       
    // Update is called once per frame
    void Awake()
    {
        transform.position = new Vector3(earth.position.x, earth.position.y, -5f);
    }
}
