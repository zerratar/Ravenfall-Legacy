using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject savingUI;

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager)
            gameManager = GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!savingUI) return;
        if (!gameManager) return;

        savingUI.SetActive(gameManager.IsSaving);
    }
}
