using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    private GameObject DDOL;
    public Animator FadeOutAnimator;

    public void Awake()
    {
        DDOL = GameObject.Find("__app");
        FadeOutAnimator = DDOL.GetComponentInChildren<Animator>();
    }

    public void LoadTutorial()
    {
        Time.timeScale = 1f;
        StartCoroutine(SwitchScene(1));
    }

    public void LoadSandboxMode()
    {
        Time.timeScale = 1f;
        StartCoroutine(SwitchScene(3));
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    IEnumerator SwitchScene(int level)
    {
        FadeOutAnimator.SetBool("FadeOut", true);
        Cursor.visible = false;
        yield return new WaitForSeconds(1f);
        DDOL.GetComponents<LevelManager>()[0].level = level;
        DDOL.GetComponents<LevelManager>()[0].isLoaded = false;
        FadeOutAnimator.gameObject.SetActive(false);
        FadeOutAnimator.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
