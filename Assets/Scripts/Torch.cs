using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private ParticleSystem fire;
    private GameObject fobj;
    private bool wasActivated;
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (!dayNightCycle) dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (!fire) fire = GetComponentInChildren<ParticleSystem>();
        if (fire)
        {
            fobj = fire.gameObject;
        }


        if (fobj == null)
        {
            this.gameObject.SetActive(false);
            return;
        }

        wasActivated = fobj.activeSelf;
    }

    // Update is called once per frame
    void Update()
    {
        if (wasActivated && gameManager.PotatoMode)
        {
            fobj.SetActive(false);
            wasActivated = false;
            return;
        }

        var isNight = dayNightCycle.IsNight;
        if (isNight != wasActivated)
        {
            fobj.SetActive(isNight);
            wasActivated = isNight;
        }
    }
}
