using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private ParticleSystem fire;
    [SerializeField] private GameManager gameManager;

    private GameObject fobj;
    private bool wasActivated;


    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }


    [Button("Assign Dependencies")]
    public void AssignDependencies()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!dayNightCycle) dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        if (!fire) fire = GetComponentInChildren<ParticleSystem>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!dayNightCycle) dayNightCycle = FindAnyObjectByType<DayNightCycle>();
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
        //if (wasActivated && gameManager.PotatoMode)
        //{
        //    fobj.SetActive(false);
        //    wasActivated = false;
        //    return;
        //}
        
        var isNight = dayNightCycle.IsNight;
        if (isNight != wasActivated)
        {
            fobj.SetActive(isNight);
            wasActivated = isNight;
        }
    }
}
