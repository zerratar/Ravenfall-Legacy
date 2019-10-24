using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingHandler : MonoBehaviour
{
    [SerializeField] private GameObject loadingView;
    [SerializeField] private GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.RavenNest == null) return;
        loadingView.SetActive(!gameManager.IsLoaded && gameManager.RavenNest.Authenticated);
    }
}
