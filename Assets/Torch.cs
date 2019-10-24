using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private ParticleSystem fire;

    // Start is called before the first frame update
    void Start()
    {
        if (!dayNightCycle) dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (!fire) fire = GetComponentInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        fire.gameObject.SetActive(dayNightCycle.IsNight);
    }
}
