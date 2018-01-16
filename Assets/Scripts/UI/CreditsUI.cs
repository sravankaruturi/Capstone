﻿using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Credits UI
/// </summary>
public class CreditsUI : MonoBehaviour
{

    /// <summary>
    /// Go back to the Main Menu
    /// </summary>
    public void MenuButton()
    {
        SceneManager.LoadSceneAsync("Title");
    }
}