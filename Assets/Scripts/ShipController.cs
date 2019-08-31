using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public Transform earth;
    private Transform rocket;
    private Rigidbody2D rocketRB;
    public float gravitationalconstant = 0.02f;
    private float boostPower = 0.8f;
    public int massEarth = 100;
    public float startSpeedX = 0;
    public float startSpeedY = 0;
    public int massRocket = 1;
    private Vector2 spawndirection;

    private LineRenderer velocityArrowLR;
    private Transform velocityArrowEnd;
    public GameObject VelocityArrow;
    public float PercentHead;

    private LineRenderer ForceArrowLR;
    public GameObject ForceArrow;
    private Transform ForceArrowEnd;
    private bool applyGravity = true;

    public GameObject Booster;
    private bool isTracking = true;
    public float rotationspeed = 5f;

    public RectTransform Fuelbar;
    private float FuelPercentage = 1.0f;
    public float FuelConsumptionRate = 0.1f;

    public GameObject fracturedRocketModel;
    private GameObject rocketModel;
    private bool hasCrashed = false;
    private bool hasLanded = false;
    private bool refueling = false;
    private bool performOnesForCrash = true;
    public List<GameObject> fractures;
    public List<Rigidbody2D> fracturesRB;

    private Transform mainCamera;
    private Camera rocketCamera;
    private float zoomLevel = 5f;

    void Start()
    {
        rocket = GameObject.Find("Rocket").transform;
        rocketRB = rocket.GetComponent<Rigidbody2D>();
        earth = GameObject.Find("Earth").transform;
        spawndirection = new Vector2(startSpeedX, startSpeedY);
        rocketRB.velocity = spawndirection;

        velocityArrowLR = VelocityArrow.GetComponent<LineRenderer>();
        velocityArrowEnd = VelocityArrow.transform;
        ForceArrowEnd = ForceArrow.transform;
        ForceArrowLR = ForceArrow.GetComponent<LineRenderer>();

        ForceArrowLR.sortingLayerName = "Foreground";
        velocityArrowLR.sortingLayerName = "Foreground";

        rocketModel = GameObject.Find("Tiny Rocket");

        mainCamera = GameObject.FindWithTag("MainCamera").transform;
        mainCamera.position = new Vector3(rocket.transform.position.x, rocket.transform.position.y, -10);
        rocketCamera = mainCamera.gameObject.GetComponent<Camera>();
    }

    void Update()
    {
        CollisionTracker();
        ShipBooster();
        Forces();
        Speed();
        Crash();
        Rockettracking();
        HandleZoom();
        Refuel();

    }

    void Forces()
    {
        EarthGravity(earth, massEarth, rocket, rocketRB);   
    }

    void ShipBooster()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        if (isTracking && FuelPercentage > 0.0f && hasCrashed == false && hasLanded == false)
        {
            Vector2 facedirection = new Vector2(mousePosition.x - rocket.position.x, mousePosition.y - rocket.position.y);
            float angle = Mathf.Atan2(facedirection.y, facedirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle-90, Vector3.forward);
            rocket.rotation = Quaternion.Slerp(rocket.rotation, rotation, rotationspeed * Time.deltaTime);
        }
               
        if (Input.GetKey(KeyCode.Mouse0) && FuelPercentage > 0.0f && hasCrashed == false)
        {
            Booster.SetActive(true);
            Vector3 BoostForce = rocket.up.normalized * boostPower;
            rocketRB.AddForce(BoostForce);
            UseFuel();
        }
        else
        {
            Booster.SetActive(false);
        }
    }

    void Speed()
    {
        Vector3 rocketVelocityVector = new Vector3(rocketRB.velocity.x, rocketRB.velocity.y, 0);

        if (rocketVelocityVector.magnitude < 0.1f || hasCrashed || hasLanded)
        {
            VelocityArrow.SetActive(false);
        }
        else
        {
            DrawArrow(VelocityArrow, velocityArrowEnd, rocketVelocityVector, velocityArrowLR);
        }
    }

    public void DrawArrow(GameObject ArrowGameObject, Transform ArrowEnd, Vector3 ArrowVector, LineRenderer ArrowLR)
    {
        ArrowGameObject.SetActive(true);
        ArrowEnd.position = rocket.position + ArrowVector;

        float AdaptiveSize = (PercentHead / Vector3.Distance(rocket.position, ArrowEnd.position));

        ArrowLR.widthCurve = new AnimationCurve(
         new Keyframe(0, 0.05f)
         , new Keyframe(0.999f - AdaptiveSize, 0.05f)
         , new Keyframe(1 - AdaptiveSize, 0.2f)
         , new Keyframe(1, 0f));

        Vector3[] points = new Vector3[] { rocket.position, rocket.position + ArrowVector * (0.999f - AdaptiveSize), rocket.position + ArrowVector * (1 - AdaptiveSize), ArrowEnd.position };
        ArrowLR.positionCount = points.Length;
        ArrowLR.SetPositions(points);
    }

    private void UseFuel()
    {        
        FuelPercentage -= FuelConsumptionRate * Time.deltaTime;
        Fuelbar.sizeDelta = new Vector2(FuelPercentage*120f, Fuelbar.sizeDelta.y);
        FuelPercentage = Mathf.Clamp(FuelPercentage, 0.0f, 1.0f);
    }

    private void Refuel()
    {
        if (refueling)
        {
            FuelPercentage += FuelConsumptionRate * Time.deltaTime;
            Fuelbar.sizeDelta = new Vector2(FuelPercentage * 120f, Fuelbar.sizeDelta.y);
            FuelPercentage = Mathf.Clamp(FuelPercentage, 0.0f, 1.0f);
        }
    }

    private void Crash()
    {
        if ((Input.GetKey("b") && performOnesForCrash) || (hasCrashed && performOnesForCrash))
        {
            performOnesForCrash = false;
            fracturedRocketModel.SetActive(true);
            fracturedRocketModel.transform.position = rocketModel.transform.position;
            fracturedRocketModel.transform.rotation = rocketModel.transform.rotation;
            fracturedRocketModel.transform.position = rocket.position;
            rocketModel.SetActive(false);

            StartCoroutine(Explosion());

            foreach (Transform fracture in fracturedRocketModel.transform)
            {
                if (fracture.tag == "Fracture")
                {
                    fractures.Add(fracture.gameObject);
                    fracturesRB.Add(fracture.gameObject.GetComponent<Rigidbody2D>());

                }                
            }
            for (int i = 0; i < fractures.Count; i++)
            {
                fractures[i].transform.parent = earth;
                fracturesRB[i].velocity = rocketRB.velocity;
            }
        }
        if (hasCrashed)
        {
            for (int i = 0; i < fractures.Count; i++)
            {
                EarthGravity(earth, massEarth, fractures[i].transform, fracturesRB[i]);
            }
        }
    }

    private void EarthGravity(Transform gravitySource, float gravitySourceMass, Transform gravityTargetObject, Rigidbody2D targetRigidbody)
    {
        Vector3 heading = gravitySource.position - gravityTargetObject.position;
        float distance = heading.magnitude;
        Vector3 gravityDirection = heading / distance;
        float gravityForce = gravitationalconstant * ((gravitySourceMass) / (distance));
        Vector3 gravityForceVector = (gravityDirection * gravityForce);

        targetRigidbody.AddForce(gravityForceVector);

        if (applyGravity && hasCrashed == false && hasLanded == false)
        {
            ForceArrow.SetActive(true);
            DrawArrow(ForceArrow, ForceArrowEnd, gravityForceVector*4, ForceArrowLR);
        }
        else
        {
            ForceArrow.SetActive(false);
        }
    }


    private void HandleZoom()
    {
        float zoomChangeAmount = 80f;
        if (Input.mouseScrollDelta.y > 0)
        {
            zoomLevel -= zoomChangeAmount * Time.deltaTime * 0.1f;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            zoomLevel += zoomChangeAmount * Time.deltaTime * 0.1f;
        }
        zoomLevel = Mathf.Clamp(zoomLevel, 1f, 10);
        rocketCamera.orthographicSize = zoomLevel;
    }

    private void Rockettracking()
    {
        if (hasCrashed == false)
        {
            mainCamera.position = new Vector3(rocket.position.x, rocket.position.y, -10);
        }

    }

    IEnumerator Explosion()
    {
        yield return new WaitForSeconds(2f);
        rocket.gameObject.SetActive(false);
    }

    private void CollisionTracker()
    {
        hasCrashed = rocketModel.GetComponent<CollisionDetector>().hasCrashed;
        hasLanded = rocketModel.GetComponent<CollisionDetector>().hasLanded;
        refueling = rocketModel.GetComponent<CollisionDetector>().refueling;
    }
}
