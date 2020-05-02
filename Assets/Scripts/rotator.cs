using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotator : MonoBehaviour
{
    private float zRotation = 0;
    private float scalemultiplier = 1;
    private Vector3 scale;

    public int speed = -20;

    private void Awake()
    {
        scale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        scalemultiplier = Mathf.Sin(Time.time*2) * 0.2f + 1;
        zRotation += speed * Time.deltaTime % 360;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.x, zRotation);
        transform.localScale = scale * scalemultiplier;
    }
}
