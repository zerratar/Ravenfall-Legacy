using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    public int LoadSceneIndex;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(LoadSceneIndex);
        }

    }
}
