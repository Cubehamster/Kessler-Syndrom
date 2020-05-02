using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public int gameState = 0;
    public bool loaded = false;

    private void Update()
    {
        if (!loaded)
        {
            if (gameState == 0)
            {
                gameState = 1;
                UnityEngine.SceneManagement.SceneManager.LoadScene("level_1");
                loaded = true;
            }
        }
    }
}

