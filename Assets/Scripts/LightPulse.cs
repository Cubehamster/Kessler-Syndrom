using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightPulse : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<Light>().intensity = 2 * Mathf.Sin(Time.time*5) + 2;
    }
}
