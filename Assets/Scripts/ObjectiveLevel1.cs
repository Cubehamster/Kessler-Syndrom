using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveLevel1 : MonoBehaviour
{
    LevelManager level;
    public Transform WayPointTarget_1;
    public Transform WayPointTarget_2;
    bool WayPoint_1 = false;
    bool WayPoint_2 = false;
    bool safetyCheck = false;
    bool checking = false;
    bool levelCompleted = false;
    public TextMeshPro scoreText;

    // Start is called before the first frame update
    void Awake()
    {
        level = Object.FindObjectOfType<LevelManager>();
        WayPointTarget_2.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(gameObject.GetComponent<ShipController>().rocketModel != null)
        {
            if (WayPointTarget_1 != null)
            {  
                if (!WayPoint_1)
                {
                    scoreText.text = "Fly to Waypoint";
                }
       
                if ((WayPointTarget_1.position - gameObject.GetComponent<ShipController>().rocketModel.transform.position).magnitude < 0.5f)
                {
                    WayPointTarget_1.gameObject.SetActive(false);
                    WayPointTarget_2.gameObject.SetActive(true);
                    WayPoint_1 = true;
                    if (!WayPoint_2)
                    {
                        scoreText.text = "Land Safely";
                    }             
                }
            }

            if (WayPointTarget_2 != null)
            {
                
                if ((WayPointTarget_2.position - gameObject.GetComponent<ShipController>().rocketModel.transform.position).magnitude < 0.5f)
                {
                    if (gameObject.GetComponent<ShipController>().refueling  && !checking && gameObject.GetComponent<ShipController>().hasLanded && WayPoint_1)
                    {
                        StartCoroutine(SafetyCheck());
                        
                    }
                    if (safetyCheck)
                    {
                        WayPointTarget_2.gameObject.SetActive(false);
                        WayPoint_2 = true;
                        if(WayPoint_1 && WayPoint_2)
                        {
                            levelCompleted = true;
                            scoreText.text = "Mission Completed";
                        }
                    }
                   
                }
            }
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
            }

        }

        if (level != null)
        {

        }

        
    }

    IEnumerator SafetyCheck()
    {
        checking = true;
        if (gameObject.GetComponent<ShipController>().refueling && gameObject.GetComponent<ShipController>().hasLanded)
        {
            yield return new WaitForSeconds(3f);       
            safetyCheck = true;
            checking = false;
        }
        else
        {
            checking = false;
            safetyCheck = false;
        }
    }
}
