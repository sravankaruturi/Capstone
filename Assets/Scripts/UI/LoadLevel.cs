﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Load the main level, once.
/// </summary>
public class LoadLevel : MonoBehaviour
{
    [SerializeField] private GameObject cam;
    [SerializeField] private AudioSource BGSound;

    private bool loading;
    private bool loaded;
    private bool once;

    private int counter;

    // Use this for initialization
    void Start()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        loading = false;
        loaded = false;
        once = false;
        counter = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!loading)
        {
            counter++;

            if (counter > 240)
            {
                loading = true;

                StartCoroutine(LoadYourAsyncScene());
            }
        }

        if(loaded && !once)
        {
            once = true;
            BGSound.Stop();
            cam.SetActive(false);
            SceneManager.UnloadSceneAsync("LoadingM");
        }
    }

    /// <summary>
    /// Load function from Unity Documentation
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadYourAsyncScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainLevel", LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainLevel"));
        loaded = true;
    }
}