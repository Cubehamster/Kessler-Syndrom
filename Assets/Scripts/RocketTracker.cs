using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketTracker : MonoBehaviour
{
    private GameObject rocket;
    private Camera rocketCamera;
    private float zoomLevel = 6f;




    void Start()
    {
        rocket = GameObject.FindWithTag("Player");
        transform.position = new Vector3(rocket.transform.position.x, rocket.transform.position.y, -10);
        rocketCamera = GetComponent<Camera>();
    }

    void Update()
    {
        Rockettracking();
        HandleZoom();
    }

    private void HandleZoom()
    {
        float zoomChangeAmount = 80f;
        if(Input.mouseScrollDelta.y > 0) {
            zoomLevel -= zoomChangeAmount * Time.deltaTime * 0.1f;
            Debug.Log("hello");
        }
        if (Input.mouseScrollDelta.y <0)
        {
            zoomLevel += zoomChangeAmount * Time.deltaTime *0.1f;
        }
        zoomLevel = Mathf.Clamp(zoomLevel, -0.5f, 7f);
        rocketCamera.orthographicSize = zoomLevel;
    }

    private void Rockettracking()
    {
        transform.position = new Vector3(rocket.transform.position.x, rocket.transform.position.y, -10);
    }
}
