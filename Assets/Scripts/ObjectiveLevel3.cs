using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveLevel3 : MonoBehaviour
{
    LevelManager level;
    public Transform WayPointTarget_1;
    public Transform WayPointTarget_2;
    bool WayPoint_1 = false;
    bool WayPoint_2 = false;
    bool safetyCheck = false;
    bool checking = false;
    bool levelCompleted = false;
    bool playOnce = false;
    bool playOnce2 = false;
    public TextMeshPro missionAccomplished;
    public TextMeshPro tutorialText;

    [SerializeField] private float alpha;
    [SerializeField] private float alpha2;
    [SerializeField] private Material missionAccomplishTextMat;
    [SerializeField] private Material tutorialTextMat;
    bool textFade = false;
    bool zoomCheck = false;
    bool boostCheck = false;

    bool textFade2 = false;
    bool startMission = false;

    float selfdestructTImer = 3.49f;

    // Start is called before the first frame update
    void Awake()
    {
        startMission = false;
        playOnce = false;
        playOnce2 = false;
        alpha = 0;
        alpha2 = 0;
        level = Object.FindObjectOfType<LevelManager>();
        WayPointTarget_2.gameObject.SetActive(false);


        missionAccomplishTextMat = missionAccomplished.GetComponent<Renderer>().material;
        missionAccomplishTextMat.EnableKeyword("_FaceColor");
        tutorialTextMat = tutorialText.GetComponent<Renderer>().material;
        tutorialTextMat.EnableKeyword("_FaceColor");

        StartCoroutine(LevelText(missionAccomplished));
    }

    // Update is called once per frame
    void Update()
    {
        if (textFade)
        {
            alpha = Mathf.Lerp(alpha, 1, 0.035f);
            missionAccomplishTextMat.SetColor("_FaceColor", new Color(1f, 1f, 1f, alpha));
        }
        else
        {
            alpha = Mathf.Lerp(alpha, 0, 0.035f);
            missionAccomplishTextMat.SetColor("_FaceColor", new Color(1f, 1f, 1f, alpha));
        }
        if (textFade2)
        {
            alpha2 = Mathf.Lerp(alpha2, 1, 0.025f);
            tutorialTextMat.SetColor("_FaceColor", new Color(1f, 1f, 1f, alpha2));
        }
        else
        {
            alpha2 = Mathf.Lerp(alpha2, 0, 0.035f);
            tutorialTextMat.SetColor("_FaceColor", new Color(1f, 1f, 1f, alpha2));
        }

        if (!playOnce2 && !gameObject.GetComponent<ShipController>().refueling && !gameObject.GetComponent<ShipController>().hasCrashed && startMission)
        {
            playOnce2 = true;
            StartCoroutine(FadeToolTip());
        }

        if (gameObject.GetComponent<ShipController>().rocketExists && startMission)
        {
            FadeToolTip();
        }
        else
        {
            if (!levelCompleted)
            {
                WayPointTarget_1.gameObject.SetActive(true);
                WayPoint_2 = false;
                WayPointTarget_2.gameObject.SetActive(false);
                WayPoint_1 = false;
                checking = false;
                safetyCheck = false;
                playOnce = false;
            }
        }


        if (!zoomCheck && startMission)
        {
            zoomCheck = true;
            StartCoroutine(ScrollCheck(missionAccomplished));
        }

        if (gameObject.GetComponent<ShipController>().FuelPercentage == 0 && !gameObject.GetComponent<ShipController>().hasCrashed && !gameObject.GetComponent<ShipController>().selfDestruct)
        {
            if (Input.GetKey(KeyCode.Mouse0) && Input.GetKey(KeyCode.Mouse1))
            {
                selfdestructTImer -= Time.deltaTime;
                tutorialText.text = Mathf.RoundToInt(selfdestructTImer).ToString();
                if (selfdestructTImer < 0.5f)
                {
                    textFade2 = false;
                    gameObject.GetComponent<ShipController>().selfDestruct = true;
                }
            }
            else if (!gameObject.GetComponent<ShipController>().selfDestruct)
            {
                selfdestructTImer = 3.45f;
                tutorialText.text = "(Hold Right + Left Mouse) = Selfdestruct";
                textFade2 = true;
            }
        }

        if (gameObject.GetComponent<ShipController>().hasCrashed && !Input.GetKey(KeyCode.Mouse0))
        {
            tutorialText.text = "(Hold Right Mouse) = Respawn";
            textFade2 = true;
            textFade = false;
        }
        else if (gameObject.GetComponent<ShipController>().respawning)
        {
            textFade2 = false;  
        }
    }

    IEnumerator BoostCheck()
    {
        yield return new WaitForSeconds(1f);
        boostCheck = true;
    }

    IEnumerator SafetyCheck()
    {
        checking = true;
        if (gameObject.GetComponent<ShipController>().refueling && gameObject.GetComponent<ShipController>().hasLanded)
        {
            yield return new WaitForSeconds(0.2f);
            safetyCheck = true;
            checking = false;
        }
        else
        {
            checking = false;
            safetyCheck = false;
        }
    }

    IEnumerator DisableDelayed(GameObject target)
    {
        target.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(0.5f);
        target.SetActive(false);
        playOnce = false;
    }

    IEnumerator LevelText(TextMeshPro titleText)
    {
        titleText.text = "Sandbox Mode";
        textFade = true;

        yield return new WaitForSeconds(3);
        textFade = false;
        startMission = true;
    }

    IEnumerator ScrollCheck(TextMeshPro titleText)
    {
        yield return new WaitForSeconds(1.8f);
        gameObject.GetComponent<ShipController>().missionStart = true;
        yield return new WaitForSeconds(1.8f);
        textFade2 = true;
        gameObject.GetComponent<ShipController>().boosterEnabled = true;
        tutorialText.text = "Have Fun!";
    }

    IEnumerator ChangeText(TextMeshPro titleText, string newText)
    {
        textFade = false;
        yield return new WaitForSeconds(1.5f);
        textFade = true;
        titleText.text = newText;

    }

    IEnumerator FadeToolTip()
    {
        yield return new WaitForSeconds(1f);
        textFade2 = false;
    }
}
