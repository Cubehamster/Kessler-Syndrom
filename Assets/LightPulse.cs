using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightPulse : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<Light>().intensity = Mathf.Sin(Time.time*5) + 1;
    }
}
