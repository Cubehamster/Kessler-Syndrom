﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDOL : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("DDOL " + gameObject.name);

        UnityEngine.SceneManagement.SceneManager.LoadScene("level_1");
    }
}
