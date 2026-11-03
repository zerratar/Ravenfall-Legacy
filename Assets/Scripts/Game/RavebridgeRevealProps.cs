using UnityEngine;

/// <summary>
/// What Ravebridge's rising from the sea is drawn and heard with (RavebridgeReveal), on the prefab
/// Resources/Ravebridge/Reveal Props: the soft circle for the mist and the dust, the bright one for the spray, and the
/// Environment Sounds pack's rumbles, rocks, water and thunder.
/// </summary>
public sealed class RavebridgeRevealProps : MonoBehaviour
{
    public Material Mist;
    public Material Spray;
    public Material Dust;

    [Header("Sounds")]
    public AudioClip Rumble;
    public AudioClip DeepRumble;
    public AudioClip Cascade;
    public AudioClip Waves;
    public AudioClip Boom;
    public AudioClip Splash;
    public AudioClip[] Debris;

    private static RavebridgeRevealProps loaded;

    public static RavebridgeRevealProps Get()
    {
        if (!loaded)
        {
            var prefab = Resources.Load<GameObject>("Ravebridge/Reveal Props");
            loaded = prefab ? prefab.GetComponent<RavebridgeRevealProps>() : null;
        }
        return loaded;
    }
}
