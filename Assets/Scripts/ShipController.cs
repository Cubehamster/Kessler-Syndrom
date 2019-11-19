using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public GameObject rocketPrefab;

    private Transform astroids;
    private Transform earth;
    private Transform rocket;
    private Rigidbody2D rocketRB;

    private float boostPower = 1f;
    private bool boosterEnabled = true;

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
    private float FuelConsumptionRate = -0.03f;
    private float RefuelingRate = 0.1f;

    private GameObject fracturedRocketModel;
    [SerializeField] private GameObject rocketModel;
    private bool hasCrashed = false;
    private bool hasLanded = false;
    private bool refueling = false;
    private bool performOnesForCrash = true;
    public List<GameObject> fractures;
    public List<GameObject> debries;
    public List<Rigidbody2D> fracturesRB;
    public List<Rigidbody2D> debriesRB;

    private Transform mainCamera;
    private Camera rocketCamera;
    private float zoomLevel = 5f;
    private float zoomDamp = 5.0f;

    private GameObject laser;
    private GameObject beamHit;
    private GameObject laserSystem;
    private float laserFuelCost = -0.03f;
    private float laserDmg = 2.0f;

    private GameObject forcefieldCollider;
    private bool forceFieldRestart = false;

    private Vector3 mousePosition;
    private float startTime;
    private float cameraRefocusSpeed = 0.125f;
    private LayerMask raycastLayer;

    void Start()
    {
        mainCamera = GameObject.FindWithTag("MainCamera").transform;
        rocketCamera = mainCamera.gameObject.GetComponent<Camera>();

        FindPlanets();
        SpawnLocation();
        LoadRocketSpawningParameters(0f, 0.0f, true, "Rocket");

        forcefieldCollider = GameObject.Find("ForcefieldCollider");
        CameraTracking(earth);
        StartLevel();
        raycastLayer = LayerMask.GetMask("Debries", "Fractures", "ForceField");
    }

    void Update()
    {
        LaserControls();    
        CollisionTracker();
        Refuel();        
        HandleZoom();
        Speed();
        Respawn();
    }

    private void FixedUpdate()
    {
        ShipBooster();
        DestroyFractures();
        Rockettracking();
        DebriesTracker();
        Crash();
        Forces();
        ForceFieldController();
    }

    void Forces()
    {
        if (rocketExists)
        {
            OrbitalBodyyGravity(earth, massEarth, rocket, rocketRB);
        }
        for (int i = 0; i < fractures.Count; i++)
        {
            OrbitalBodyyGravity(earth, massEarth, fractures[i].transform, fracturesRB[i]);
        }
        for (int i = 0; i < debries.Count; i++)
        {
            OrbitalBodyyGravity(earth, massEarth, debries[i].transform, debriesRB[i]);
        }
    }

    void ShipBooster()
    {
        if (rocketExists)
        {
            mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            if (isTracking && FuelPercentage > 0.0f && hasCrashed == false && hasLanded == false && boosterEnabled)
            {
                Vector2 facedirection = new Vector2(mousePosition.x - rocket.position.x, mousePosition.y - rocket.position.y);
                float angle = Mathf.Atan2(facedirection.y, facedirection.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
                rocket.rotation = Quaternion.Slerp(rocket.rotation, rotation, rotationspeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.Mouse0) && FuelPercentage > 0.0f && hasCrashed == false && boosterEnabled)
            {
                Booster.SetActive(true);
                Vector3 BoostForce = rocket.up.normalized * boostPower * rocketRB.mass;
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

            if (VelocityArrow)
            {
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
            forceFieldRestart = true;
            boosterEnabled = false;

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
                    fractures[i].gameObject.GetComponent<CollisionImpactSound>().hitpoints = fracturesRB[i].mass;
                    StartCoroutine(CrashChangeLayer(i, fractures, debries, fracturesRB, debriesRB));
                    fractures[i].transform.parent = earth;
                }
            }
        }
    }

    IEnumerator CrashChangeLayer(int i, List<GameObject> listFrom, List<GameObject> listTo, List<Rigidbody2D> listFromRB, List<Rigidbody2D> listToRB)
    {
        yield return new WaitForSeconds(1f);
        listFrom[0].tag = "Debries";
        listFrom[0].layer = 12;
        listTo.Add(fractures[0].gameObject);
        listToRB.Add(fracturesRB[0]);
        listFromRB.RemoveAt(0);
        listFrom.RemoveAt(0);
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
            if (ForceArrow)
            {
                if (hasCrashed == false && hasLanded == false)
                {
                    ForceArrow.SetActive(true);
                    DrawArrow(ForceArrow, ForceArrowEnd, gravityForceVector/rocketRB.mass * 4, ForceArrowLR);
                }
                else
                {
                    ForceArrow.SetActive(false);
                }
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
            CameraTracking(rocket);
        }
        else if (!rocketExists && performOnesForCrash)
        {
            StartCoroutine(CameraSwitchEarth());
        }

    }

    IEnumerator CameraSwitchEarth()
    {
        yield return new WaitForSeconds(3f);
        CameraTracking(earth);
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
            StartCoroutine(RespawnControlDelay());
        }
    }

    IEnumerator RespawnControlDelay()
    {
        yield return new WaitForSeconds(2f);
        boosterEnabled = true;
    }

    IEnumerator Explosion()
    {
        Destroy(ForceArrow);
        Destroy(VelocityArrow);
        yield return new WaitForSeconds(3f);
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
            rocket.GetComponent<PointEffector2D>().forceMagnitude = rocketRB.mass * 0.15f;
            rocket.GetComponent<PointEffector2D>().forceVariation = rocketRB.mass * 0.15f;
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
            beamHit = GameObject.Find("BeamHit");
            beamHit.SetActive(false);
            laserSystem = GameObject.Find("LaserSystem");
            laserSystem.SetActive(false);
        }
    }


    private void SpawnLocation()
    {
        spawnLocation = GameObject.Find("SpawnLocation").transform;
    }

    private void CameraTracking(Transform target)
    {
        Vector3 targetCamera = new Vector3(target.transform.position.x, target.transform.position.y, -10);
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.position, targetCamera, cameraRefocusSpeed);
        mainCamera.position = smoothedPosition;
    }

    private void FindPlanets()
    {
        earth = GameObject.Find("Earth").transform;
        astroids = GameObject.Find("Astroids").transform;
    }

    private void Laserbeam()
    {
        if (rocketExists)
        {
            Vector3 laserSpawn = laser.transform.position;
            Vector2 laserAim = new Vector2(mousePosition.x - laserSpawn.x, mousePosition.y - laserSpawn.y);
            Vector3 laserFire = new Vector3(mousePosition.x, mousePosition.y, laserSpawn.z);

            Debug.DrawRay(laserSpawn, laserAim, Color.blue);

            LineRenderer LaserLineRenderer = laser.GetComponent<LineRenderer>();

            LaserLineRenderer.SetPosition(0, laserSpawn);
            RaycastHit2D hit = Physics2D.Raycast(laserSpawn, Vector3.Normalize(laserAim), 10f, raycastLayer);
            if (hit)
            {
                if (hit.collider)
                {
                    beamHit.SetActive(true);
                    beamHit.transform.position = hit.point;
                    LaserLineRenderer.SetPosition(1, hit.point);
                    if (hit.collider.tag == "Debries")
                    {
                        hit.collider.gameObject.GetComponent<CollisionImpactSound>().hitpoints -= laserDmg * Time.deltaTime;
                    }
                }
            }
            else
            {
                LaserLineRenderer.SetPosition(1, laserSpawn + Vector3.Normalize(laserFire - laserSpawn)*4f);
                beamHit.SetActive(false);
            }
        }

    }

    private void LaserControls()
    {
        if (rocketExists)
        {
            if (Input.GetKey(KeyCode.Mouse1))
            {
                laserSystem.SetActive(true);
                Laserbeam();
                FuelChange(laserFuelCost);
            }
            else
            {
                laserSystem.SetActive(false);
            }
        }
    }

    private void ForceFieldController()
    {
        if (forceFieldRestart)
        {
            StartCoroutine(ForceFieldCycle());
        }
        else
        {
            forcefieldCollider.SetActive(true);
        }
    }

    IEnumerator ForceFieldCycle()
    {
        forcefieldCollider.GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(3f);
        forcefieldCollider.GetComponent<Collider2D>().enabled = true;
        forceFieldRestart = false;
    }
    
    private void DebriesTracker()
    {
        for (int i = 0; i < debries.Count; i++)
        {
            if (debries[i].GetComponent<CollisionImpactSound>().hitpoints <= 0 && debries[i].GetComponent<CollisionImpactSound>().playOnce)
            {
                debries[i].layer = 13;
                debries[i].GetComponent<CollisionImpactSound>().playOnce = false;

                if (debries[i].GetComponent<CollisionImpactSound>().impactExplosion != null)
                {
                    debries[i].GetComponent<CollisionImpactSound>().debris.Play();
                    debries[i].layer = 13;

                    debries[i].GetComponent<MeshRenderer>().enabled = false;
                }
                for (int n = 0; n < debries[i].transform.childCount; n++)
                {
                    debries[i].transform.GetChild(n).gameObject.SetActive(true);
                }
                for (int n = 0; n < debries[i].transform.childCount; n++)
                {
                    debries[i].transform.GetChild(n).gameObject.SetActive(true);
                    foreach (Transform fracture in debries[i].transform)
                    {
                        if (debries[i].transform.GetChild(0).gameObject.tag == "Fracture")
                        {
                            debries[i].transform.GetChild(0).gameObject.GetComponent<Rigidbody2D>().velocity = debriesRB[i].velocity;
                            debries[i].transform.GetChild(0).GetComponent<CollisionImpactSound>().hitpoints = debries[i].transform.GetChild(0).gameObject.GetComponent<Rigidbody2D>().mass * 40f;
                            fractures.Add(debries[i].transform.GetChild(0).gameObject);
                            fracturesRB.Add(debries[i].transform.GetChild(0).gameObject.GetComponent<Rigidbody2D>());
                            StartCoroutine(CrashChangeLayer(i, fractures, debries, fracturesRB, debriesRB));
                            debries[i].transform.GetChild(0).gameObject.transform.parent = astroids;
                        }
                    }
                }
                debries[i].gameObject.GetComponent<CollisionImpactSound>().triggerDestruction = true;
            }
        }
    }

    private void DestroyFractures()
    {
        for (int i = 0; i < debries.Count; i++)
        {
            if (debries[i].gameObject.GetComponent<CollisionImpactSound>().destructionComplete == true)
            {
                Destroy(debries[i]);
                debriesRB.RemoveAt(i);
                debries.RemoveAt(i);
            }
        }
    }

    void StartLevel()
    {
        foreach (Transform debrie in astroids.transform)
        {
            if (debrie.tag == "Debries")
            {
                debries.Add(debrie.gameObject);
                debriesRB.Add(debrie.gameObject.GetComponent<Rigidbody2D>());
                for (int i = 0; i < debries.Count; i++)
                {
                    debries[i].gameObject.GetComponent<CollisionImpactSound>().hitpoints = debriesRB[i].mass*40f;

                    Vector2 heading = earth.position - debries[i].transform.position;
                    float distance = heading.magnitude;
                    Vector2 gravityDirection = heading / distance;
                    float gravityForce = debriesRB[i].mass * gravitationalconstant * massEarth / distance;

                    float randomAngleOffset = Random.Range(-20f, 20f);
                    float randomAnglularVelocity = Random.Range(-15f, 15f);
                    debriesRB[i].velocity = Mathf.Sqrt((gravityForce * distance) / debriesRB[i].mass)*(Quaternion.Euler(0, 0, -90f + randomAngleOffset) * heading.normalized);
                    debriesRB[i].angularVelocity = randomAnglularVelocity;
                }
            }
        }
    }
}
