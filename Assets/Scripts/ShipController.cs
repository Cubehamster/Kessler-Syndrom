using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    //players rocket prefab
    public GameObject rocketPrefab;

    //reference markers
    private Transform astroids;
    private Transform earth;

    //rocket markers
    private Transform rocket;
    private Rigidbody2D rocketRB;

    //boost parameters
    private float boostPower = 1f;
    [HideInInspector] public bool boosterEnabled = false;

    //orbital mechanic constants
    private float gravitationalconstant = 0.008f;
    private float massEarth = 250.0f;

    //rocket spawning parameters
    [HideInInspector] public bool rocketExists = false;
    private Transform spawnLocation;
    private Vector2 spawndirection;

    //velocity arror parameters
    private LineRenderer velocityArrowLR;
    private Transform velocityArrowEnd;
    private GameObject VelocityArrow;

    //force arrow parameters
    private LineRenderer ForceArrowLR;
    private GameObject ForceArrow;
    private Transform ForceArrowEnd;

    //arrowheadsize
    private float PercentHead = 0.2f;

    //orientation controls
    private GameObject Booster;
    private Quaternion rotation;
    private bool isTracking = true;
    private float rotationspeed = 5f;

    //rocketfuel parameters
    public RectTransform Fuelbar;
    public float FuelPercentage = 1.0f;
    private float FuelConsumptionRate = -0.03f;
    private float RefuelingRate = 0.3f;

    //crash parameters
    private GameObject fracturedRocketModel;
    [SerializeField] public GameObject rocketModel;
    [HideInInspector] public bool hasCrashed = false;
    [HideInInspector] public bool hasLanded = true;
    [HideInInspector] public bool refueling = false;
    [HideInInspector] public bool performOnesForCrash = true;

    //debries lists
    public List<GameObject> fractures;
    public List<GameObject> debries;
    public List<Rigidbody2D> fracturesRB;
    public List<Rigidbody2D> debriesRB;

    //Objectives and Score
    public List<GameObject> objectives;
    public GameObject score;
    public TextMeshPro scoreText;
    public TextMeshPro tutorialText;

    //camera parameters
    private Transform mainCamera;
    private Camera rocketCamera;
    [SerializeField] public float zoomLevel = 5f;
    private float zoomDamp = 3.0f;
    private float cameraRefocusSpeed = 0.08f;

    //laser parameters
    private GameObject laser;
    private GameObject beamHit;
    private GameObject laserSystem;
    private float laserFuelCost = -0.03f;
    private float laserDmg = 5f;
    private float laserLength = 5.0f;
    private LayerMask raycastLayer;

    //forcefield parameters
    private GameObject forcefieldCollider;
    private bool forceFieldRestart = false;

    //mousecontrol marker
    private Vector3 mousePosition;

    //debries health parameters
    private float hpSizePower = 0.6f;
    private float hpRecoverRate = 0.02f;

    public Light landingLight;
    [HideInInspector] public bool missionStart = false;
    [HideInInspector] public bool missionCompleted = true;

    public bool selfDestruct = false;
    public bool respawning = false;

    void Awake()
    {
        missionStart = false;
        missionCompleted = false;

        //find important objects in scene
        mainCamera = GameObject.FindWithTag("MainCamera").transform;
        forcefieldCollider = GameObject.Find("ForcefieldCollider");

        //find orbital parents needed for lists
        FindPlanets();

        //get cameracomponent
        rocketCamera = mainCamera.gameObject.GetComponent<Camera>();

        //set spawnlocation
        SpawnLocation();

        //load starting rocketparameters
        LoadRocketSpawningParameters(0f, 0.0f, true, "Rocket");

        //set camera to track earth inintially
        CameraTracking(earth);

        //initialize all the astroids start velocity, angular velocity, rigidbody mass and hitpoints
        StartLevel();

        //get access to score
        scoreText = score.GetComponent<TextMeshPro>();

        //setup mask for laser
        raycastLayer = LayerMask.GetMask("Debries", "Fractures", "ForceField", "Default");
    }

    void LateUpdate()
    {
        LandingLight();
        LaserControls();
        CollisionTracker();
        Refuel();
        Speed();
        Respawn();
        ObjectiveTracker();
        HandleZoom();
    }

    private void FixedUpdate()
    {
        RocketParent();
        ShipBooster();
        DestroyFractures();
        DebriesTracker();
        Crash();
        Forces();
        ForceFieldController();
        Rockettracking();
        TutorialText();
    }

    //Does all the orbital mechanics for the rocket, fractures and debries
    void Forces()
    {
        if (rocketExists)
        {
            OrbitalBodyGravity(earth, massEarth, rocket, rocketRB);
            AtmosphericDrag(rocket.transform, rocketRB);
        }
        for (int i = 0; i < fractures.Count; i++)
        {
            OrbitalBodyGravity(earth, massEarth, fractures[i].transform, fracturesRB[i]);
            AtmosphericDrag(fractures[i].transform, fracturesRB[i]);
        }
        for (int i = 0; i < debries.Count; i++)
        {
            OrbitalBodyGravity(earth, massEarth, debries[i].transform, debriesRB[i]);
            AtmosphericDrag(debries[i].transform, debriesRB[i]);
        }
    }

    //handles mouselook and force booster when there is fuel
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

    //draws a velocity vector based on rocket speed
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

    //draw function used for the drawing of arrows
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

    //fuel drain when boosting
    private void UseFuel()
    {
        FuelChange(FuelConsumptionRate);
    }

    //fuel gain when refueling
    private void Refuel()
    {
        if (refueling && rocketExists)
        {
            FuelChange(RefuelingRate);
        }
    }

    //handles crash and suicide
    private void Crash()
    {
        if ((selfDestruct && performOnesForCrash && FuelPercentage == 0) || (hasCrashed && performOnesForCrash))
        {
            performOnesForCrash = false;
            forceFieldRestart = true;
            boosterEnabled = false;
            hasCrashed = true;

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
                if (fractures[i].transform.parent != earth)
                {
                    fracturesRB[i].velocity = rocketRB.velocity;
                    fractures[i].gameObject.GetComponent<CollisionImpactSound>().hitpoints = Mathf.Pow((fracturesRB[i].mass * 80f), hpSizePower);
                    fractures[i].gameObject.GetComponent<CollisionImpactSound>().hpSizePower = hpSizePower;
                    StartCoroutine(CrashChangeLayer(i, fractures, debries, fracturesRB, debriesRB));
                    fractures[i].transform.parent = earth;
                }
            }
        }
    }

    //switches fractures how can't collide with eachother during an explosion to the debries layer where things can collide without eachother
    IEnumerator CrashChangeLayer(int i, List<GameObject> listFrom, List<GameObject> listTo, List<Rigidbody2D> listFromRB, List<Rigidbody2D> listToRB)
    {
        yield return new WaitForSeconds(0.6f);
        if (listFrom[0] != null)
        {
            listFrom[0].tag = "Debries";
            listFrom[0].layer = 12;
            listTo.Add(fractures[0].gameObject);
            listToRB.Add(fracturesRB[0]);
            listFromRB.RemoveAt(0);
            listFrom.RemoveAt(0);
        }
    }

    //function that handles the orbital gravity of given targets
    private void OrbitalBodyGravity(Transform gravitySource, float gravitySourceMass, Transform gravityTargetObject, Rigidbody2D targetRigidbody)
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
                    DrawArrow(ForceArrow, ForceArrowEnd, gravityForceVector / rocketRB.mass * 4, ForceArrowLR);
                }
                else
                {
                    ForceArrow.SetActive(false);
                }
            }

        }
    }

    //handles the camerazoom of the orthagonal camera
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
        zoomLevel = Mathf.Clamp(zoomLevel, 0.5f, 40f);
        rocketCamera.orthographicSize = Mathf.Lerp(rocketCamera.orthographicSize, zoomLevel, zoomDamp * Time.deltaTime);
    }

    //keeps track of what the camera should be looking at
    private void Rockettracking()
    {
        if (!hasCrashed && rocketExists && missionStart && !missionCompleted)
            CameraTracking(rocket);
        else if((!rocketExists && performOnesForCrash))    
            StartCoroutine(CameraSwitchEarth());
        else if(rocketExists && missionStart && missionCompleted)
            StartCoroutine(EndSwitchEarth());

    }

    //switches camera to earth after 3 seconds
    IEnumerator CameraSwitchEarth()
    {
        yield return new WaitForSeconds(3f);
        CameraTracking(earth);
    }

    IEnumerator EndSwitchEarth()
    {
        yield return new WaitForSeconds(0.5f);
        missionStart = false;
        CameraTracking(earth);
    }

    //function that handles fuelchange based on ability
    private void FuelChange(float FuelRate)
    {
        FuelPercentage += FuelRate * Time.deltaTime;
        Fuelbar.sizeDelta = new Vector2(FuelPercentage * 120f, Fuelbar.sizeDelta.y);
        FuelPercentage = Mathf.Clamp(FuelPercentage, 0.0f, 1.0f);
    }

    //initializes a respawn when pressing r while dead
    private void Respawn()
    {
        if (!boosterEnabled && rocketExists)
        {
            rocketModel.GetComponent<CollisionDetector>().hasLanded = true;
        }
        if (Input.GetKey(KeyCode.Mouse1) && !rocketExists && hasCrashed && !respawning)
        {
            respawning = true;               
            StartCoroutine(RespawnCoroutine());
        }
    }

    //gives a 2 second delay after resapwn
    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        GameObject Rocket = Instantiate(rocketPrefab, spawnLocation.position, spawnLocation.rotation);
        Rocket.transform.parent = earth;
        hasCrashed = false;
        performOnesForCrash = true;
        LoadRocketSpawningParameters(0f, 0f, true, "Rocket(Clone)");
        FuelPercentage = 1.0f;
        yield return new WaitForSeconds(2f);
        boosterEnabled = true;
        selfDestruct = false;
        respawning = false;
    }

    //manages the child objects of a rocket during and after crash
    IEnumerator Explosion()
    {
        Destroy(ForceArrow);
        Destroy(VelocityArrow);
        yield return new WaitForSeconds(1.5f);
        Destroy(rocket.gameObject);
        rocketExists = false;
        
    }

    //checks the condition of the rocket in the collisiondetector script that is on the rocket
    private void CollisionTracker()
    {
        if (rocketExists && !selfDestruct)
        {
            hasCrashed = rocketModel.GetComponent<CollisionDetector>().hasCrashed;
            hasLanded = rocketModel.GetComponent<CollisionDetector>().hasLanded;
            refueling = rocketModel.GetComponent<CollisionDetector>().refueling;
        }
    }

    //function that controls the spawning condition of the rocket
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

    //finds the spawnlocation
    private void SpawnLocation()
    {
        spawnLocation = GameObject.Find("SpawnLocation").transform;
    }

    //handles the camera tracking smoothly for a given target
    private void CameraTracking(Transform target)
    {
        Vector3 velocity = Vector3.zero;
        Vector3 targetCamera = new Vector3(target.transform.position.x, target.transform.position.y, -10);
        Vector3 smoothedPosition = Vector3.SmoothDamp(mainCamera.position, targetCamera, ref velocity, cameraRefocusSpeed);
        //Vector3 smoothedPosition = Vector3.Lerp(mainCamera.position, targetCamera, cameraRefocusSpeed);
        mainCamera.position = smoothedPosition;
    }

    //Find references
    private void FindPlanets()
    {
        earth = GameObject.Find("Earth").transform;
        astroids = GameObject.Find("Astroids").transform;
    }

    //controls the players raycast laserbeam
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
            RaycastHit2D hit = Physics2D.Raycast(laserSpawn, Vector3.Normalize(laserAim), laserLength, raycastLayer);
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
                LaserLineRenderer.SetPosition(1, laserSpawn + Vector3.Normalize(laserFire - laserSpawn) * laserLength);
                beamHit.SetActive(false);
            }
        }

    }

    //determines how the laser is controlled
    private void LaserControls()
    {
        if (rocketExists)
        {
            if (Input.GetKey(KeyCode.Mouse1) && FuelPercentage > 0 && FuelPercentage < 1 && !refueling && !hasCrashed)
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

    //controls when or when not the forcefield stops debries from passing through
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

    //a short cycledelay for the forcefield
    IEnumerator ForceFieldCycle()
    {
        forcefieldCollider.GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(3f);
        forcefieldCollider.GetComponent<Collider2D>().enabled = true;
        forceFieldRestart = false;
    }

    //handles all the debries (so astroid and rocketfractures)
    private void DebriesTracker()
    {
        for (int i = 0; i < debries.Count; i++)
        {
            if (debries[i].GetComponent<CollisionImpactSound>().hitpoints < Mathf.Pow((debries[i].GetComponent<Rigidbody2D>().mass * 80f), hpSizePower) && debries[i].GetComponent<CollisionImpactSound>().hitpoints > 0)
            {
                debries[i].GetComponent<CollisionImpactSound>().hitpoints += Time.deltaTime * hpRecoverRate;
            }
            if (debries[i].GetComponent<CollisionImpactSound>().hitpoints <= 0 && debries[i].GetComponent<CollisionImpactSound>().playOnce)
            {
                debries[i].layer = 13;
                debries[i].GetComponent<CollisionImpactSound>().playOnce = false;

                if (debries[i].GetComponent<CollisionImpactSound>().impactExplosion != null)
                {
                    debries[i].GetComponent<CollisionImpactSound>().debris.Play();
                    debries[i].layer = 13;
                    if (debries[i].GetComponent<MeshRenderer>() != null)
                    {
                        debries[i].GetComponent<MeshRenderer>().enabled = false;
                    }
                    else {
                        debries[i].GetComponentInChildren<MeshRenderer>().enabled = false;
                    }

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
                            debries[i].transform.GetChild(0).GetComponent<CollisionImpactSound>().hitpoints = Mathf.Pow((debries[i].transform.GetChild(0).gameObject.GetComponent<Rigidbody2D>().mass * 80f), hpSizePower);
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

    //function that handles debries destruction
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

    //initializes the startconditions of all the astroids in the scene
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
                    debries[i].gameObject.GetComponent<CollisionImpactSound>().hitpoints = Mathf.Pow((debriesRB[i].mass * 80f), hpSizePower);
                    debries[i].gameObject.GetComponent<CollisionImpactSound>().hpSizePower = hpSizePower;

                    Vector2 heading = earth.position - debries[i].transform.position;
                    float distance = heading.magnitude;
                    Vector2 gravityDirection = heading / distance;
                    float gravityForce = debriesRB[i].mass * gravitationalconstant * massEarth / distance;

                    float randomAngleOffset = Random.Range(-20f, 20f);
                    float randomAnglularVelocity = Random.Range(-18f, 18f);
                    debriesRB[i].velocity = Mathf.Sqrt((gravityForce * distance) / debriesRB[i].mass) * (Quaternion.Euler(0, 0, -90f + randomAngleOffset) * heading.normalized);
                    debriesRB[i].angularVelocity = randomAnglularVelocity;
                }
            }
        }
    }

    void RocketParent()
    {
        if (rocketExists)
        {
            if ((rocket.position - earth.position).magnitude > 15f)
            {
                rocket.parent = transform;
            }
            else
            {
                rocket.parent = earth;
            }
        }

    }

    void ObjectiveTracker()
    {
        //scoreText.text = $"Debries: {objectives.Count}";
    }

    public void OnTriggerEnter2D(Collider2D col)
    {
        objectives.Add(col.gameObject);
    }
    public void OnTriggerExit2D(Collider2D col)
    {
        objectives.Remove(col.gameObject);
    }

    public void LandingLight()
    {
        landingLight.GetComponent<Light>().intensity = 2 * Mathf.Sin(Time.time * 5) + 2;
        if (boosterEnabled && FuelPercentage > 0.99f)
        {
            landingLight.GetComponent<Light>().color = new Color(0, 1, 0);
        }
        else if (boosterEnabled && FuelPercentage < 0.99f)
        {
            landingLight.GetComponent<Light>().color = new Color(1, 0.5f, 0);
        }
        else
        {
            landingLight.GetComponent<Light>().color = new Color(1, 0, 0);
        }
    }

    private void TutorialText()
    {
        if(!hasCrashed && tutorialText != null && boosterEnabled)
        {
            tutorialText.gameObject.transform.position = new Vector3(rocket.position.x, rocket.position.y + 0.8f, tutorialText.gameObject.transform.position.z);
        }
    }

    private void AtmosphericDrag(Transform objectPos, Rigidbody2D objectRB)
    {
        float height;

        height = (objectPos.transform.position - earth.transform.position).magnitude;
        if (height < 5)
        {
            objectRB.drag = -0.33f * height + 1.66f;
            if(objectPos.tag == "Debries" || objectPos.tag == "Fracture")
            objectPos.gameObject.GetComponent<CollisionImpactSound>().hitpoints -= objectRB.drag * objectRB.velocity.magnitude * 0.25f * Mathf.Sqrt(objectRB.mass);
        }
        else
        {
            objectRB.drag = 0;
        }


    }
}