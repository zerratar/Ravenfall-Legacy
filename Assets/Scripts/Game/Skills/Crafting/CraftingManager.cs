using System;
using System.Linq;
using RavenNest.Models;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private IoCContainer ioc;
    [SerializeField] private GameManager game;

    void Start()
    {
        if (!settings) settings = GetComponent<GameSettings>();
        if (!game) game = GetComponent<GameManager>();
        if (!ioc) ioc = GetComponent<IoCContainer>();
    }
}