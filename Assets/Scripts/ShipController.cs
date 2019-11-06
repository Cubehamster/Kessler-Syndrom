using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public GameObject rocketPrefab;

    private Transform earth;
    private Transform rocket;
    private Rigidbody2D rocketRB;

    private float boostPower = 1f;

    private float gravitationalconstant = 0.008f;
    private float massEarth = 250.0f;

    private bool rocketExists = false;
    private Transform spawnLocation;
    private Vector2 spawndirection;    

    private LineRenderer velocityArrowLR;
    private Transform velocityArrowEnd;
    private GameObject VelocityArrow;
    private float PercentHead = 0.2f;

    private LineRenderer ForceArrowLR;
    private GameObject ForceArrow;
    private Transform ForceArrowEnd;

    private GameObject Booster;
    private Quaternion rotation;
    private bool isTracking = true;
    private float rotationspeed = 5f;

    public RectTransform Fuelbar;
    private float FuelPercentage = 1.0f;
    private float FuelConsumptionRate = -0.05f;
    private float RefuelingRate = 0.1f;

    private GameObject fracturedRocketModel;
    [SerializeField] private GameObject rocketModel;
    private bool hasCrashed = false;
    private bool hasLanded = false;
    private bool refueling = false;
    private bool performOnesForCrash = true;
    public List<GameObject> fractures;
    public List<Rigidbody2D> fracturesRB;

    private Transform mainCamera;
    private Camera rocketCamera;
    private float zoomLevel = 5f;
    private float zoomDamp = 5.0f;
    private float trackingDamp = 5.0f;

    [SerializeField]  private GameObject laser;

    private Vector3 mousePosition;

    void Start()
    {
        FindPlanets();
        SpawnLocation();
        LoadRocketSpawningParameters(0f, 0.0f, true, "Rocket");
        CameraTracking(earth);
    }

    void Update()
    {
        CollisionTracker();
        Refuel();
        Rockettracking();
        Crash();
        HandleZoom();
        Respawn();
        ShipBooster();
        Speed();
        Forces();
        Laserbeam();
    }

    void Forces()
    {
        if (rocketExists)
        {
            OrbitalBodyyGravity(earth, massEarth, rocket, rocketRB);
        }
    }

    void ShipBooster()
    {
        if (rocketExists)
        {
            mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            if (isTracking && FuelPercentage > 0.0f && hasCrashed == false && hasLanded == false)
            {
                Vector2 facedirection = new Vector2(mousePosition.x - rocket.position.x, mousePosition.y - rocket.position.y);
                float angle = Mathf.Atan2(facedirection.y, facedirection.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
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
    }

    void Speed()
    {
        if (rocketExists)
        {
            Vector3 rocketVelocityVector = new Vector3(rocketRB.velocity.x, rocketRB.velocity.y, 0);

            if (hasCrashed || hasLanded)
            {
                VelocityArrow.SetActive(false);
            }
            else
            {
                DrawArrow(VelocityArrow, velocityArrowEnd, rocketVelocityVector, velocityArrowLR);
            }
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
        FuelChange(FuelConsumptionRate);
    }

    private void Refuel()
    {
        if (refueling && rocketExists)
        {
            FuelChange(RefuelingRate);
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
                if(fractures[i].transform.parent != earth)
                {
                    fracturesRB[i].velocity = rocketRB.velocity;
                    fractures[i].transform.parent = earth;
                }
            }
        }
        for (int i = 0; i < fractures.Count; i++)
        {
            OrbitalBodyyGravity(earth, massEarth, fractures[i].transform, fracturesRB[i]);
        }
    }

    private void OrbitalBodyyGravity(Transform gravitySource, float gravitySourceMass, Transform gravityTargetObject, Rigidbody2D targetRigidbody)
    {
        Vector3 heading = gravitySource.position - gravityTargetObject.position;
        float distance = heading.magnitude;
        Vector3 gravityDirection = heading / distance;
        float gravityForce = targetRigidbody.mass * gravitationalconstant * gravitySourceMass / distance;
        Vector3 gravityForceVector = (gravityDirection * gravityForce);

        targetRigidbody.AddForce(gravityForceVector);

        if (rocketExists & gravityTargetObject == rocket)
        {
            if (hasCrashed == false && hasLanded == false)
            {
                ForceArrow.SetActive(true);
                DrawArrow(ForceArrow, ForceArrowEnd, gravityForceVector * 4, ForceArrowLR);
            }
            else
            {
                ForceArrow.SetActive(false);
            }
        }
    }
    
    private void HandleZoom()
    {
        float zoomChangeAmount = 80f;
        if (Input.mouseScrollDelta.y > 0)
        {
            zoomLevel -= zoomChangeAmount * (1f + zoomLevel / 10f) * (1f + zoomLevel / 10f) * Time.deltaTime * 0.2f;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            zoomLevel += zoomChangeAmount * (1f + zoomLevel / 10f) * (1f + zoomLevel / 10f) * Time.deltaTime * 0.2f;
        }
        zoomLevel = Mathf.Clamp(zoomLevel, 1f, 200f);
        rocketCamera.orthographicSize = Mathf.Lerp(rocketCamera.orthographicSize, zoomLevel, zoomDamp * Time.deltaTime);
    }

    private void Rockettracking()
    {
        if (hasCrashed == false && rocketExists)
        {
            mainCamera.position = new Vector3(rocket.position.x, rocket.position.y, -10);
        }

    }

    private void FuelChange(float FuelRate)
    {
        FuelPercentage += FuelRate * Time.deltaTime;
        Fuelbar.sizeDelta = new Vector2(FuelPercentage * 120f, Fuelbar.sizeDelta.y);
        FuelPercentage = Mathf.Clamp(FuelPercentage, 0.0f, 1.0f);
    }

    private void Respawn()
    {
        if (Input.GetKey("r") && rocketExists == false)
        {
            GameObject Rocket = Instantiate(rocketPrefab, spawnLocation.position, spawnLocation.rotation);
            Rocket.transform.parent = earth;
            hasCrashed = false;
            performOnesForCrash = true;
            LoadRocketSpawningParameters(0f, 0f, true, "Rocket(Clone)");
            CameraTracking(rocket);
            FuelPercentage = 1.0f;
        }
    }

    IEnumerator Explosion()
    {
        yield return new WaitForSeconds(3f);
        CameraTracking(earth);
        rocketExists = false;
        Destroy(rocket.gameObject);
    }

    private void CollisionTracker()
    {
        if (rocketExists)
        {
            hasCrashed = rocketModel.GetComponent<CollisionDetector>().hasCrashed;
            hasLanded = rocketModel.GetComponent<CollisionDetector>().hasLanded;
            refueling = rocketModel.GetComponent<CollisionDetector>().refueling;
        }
    }

    private void LoadRocketSpawningParameters(float startSpeedX, float startSpeedY, bool rocketExistsAtStart, string rocketName)
    {
        rocketExists = rocketExistsAtStart;
        if (rocketExists)
        {
            rocket = GameObject.Find(rocketName).transform;
            rocketRB = rocket.GetComponent<Rigidbody2D>();
            Booster = GameObject.Find("Booster");
            rocketModel = GameObject.Find("Tiny Rocket");
            fracturedRocketModel = GameObject.Find("Tiny Rocket(fractured)");
            fracturedRocketModel.SetActive(false);
            spawndirection = new Vector2(startSpeedX, startSpeedY);
            rocketRB.velocity = spawndirection;

            VelocityArrow = GameObject.Find("VelocityArrow");
            velocityArrowLR = VelocityArrow.GetComponent<LineRenderer>();
            velocityArrowEnd = VelocityArrow.transform;

            ForceArrow = GameObject.Find("ForceArrow");
            ForceArrowEnd = ForceArrow.transform;
            ForceArrowLR = ForceArrow.GetComponent<LineRenderer>();

            laser = GameObject.Find("LaserSpawn");
        }
    }


    private void SpawnLocation()
    {
        spawnLocation = GameObject.Find("SpawnLocation").transform;
    }

    private void CameraTracking(Transform target)
    {
        mainCamera = GameObject.FindWithTag("MainCamera").transform;
        mainCamera.position = new Vector3(target.transform.position.x, target.transform.position.y, -10);       
        rocketCamera = mainCamera.gameObject.GetComponent<Camera>();
    }

    private void FindPlanets()
    {
        earth = GameObject.Find("Earth").transform;
    }

    private void Laserbeam()
    {
        if (rocketExists)
        {
            Vector3 laserSpawn = laser.transform.position;
            Vector3 laserAim = new Vector3(mousePosition.x - laserSpawn.x, mousePosition.y - laserSpawn.y, laserSpawn.z);

            Debug.DrawRay(laserSpawn, laserAim, Color.blue);

            LineRenderer LaserLineRenderer = laser.GetComponent<LineRenderer>();

            LaserLineRenderer.SetPosition(0, laserSpawn);
            RaycastHit hit;
            if (Physics.Raycast(laserSpawn, laserAim, out hit))
            {
                if (hit.collider)
                {
                    LaserLineRenderer.SetPosition(1, hit.point);
                    Debug.Log("Hitting");
                }
            }
            else LaserLineRenderer.SetPosition(1, mousePosition);
        }

    }
}
