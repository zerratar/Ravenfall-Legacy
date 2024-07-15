using System;
using UnityEngine;

public class ManualPlayerController : MonoBehaviour
{
    private PlayerController player;
    private PlayerAnimationController animations;

    public bool Active { get; internal set; }

    // Start is called before the first frame update
    void Start()
    {
        this.player = GetComponent<PlayerController>();
        this.animations = player.Animations;
    }

    // Update is called once per frame
    public void Poll()
    {
        if (!Active || !player)
            return;


    }
}
