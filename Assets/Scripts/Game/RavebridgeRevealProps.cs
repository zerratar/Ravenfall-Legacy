using UnityEngine;

/// <summary>
/// What Ravebridge's rising from the sea is drawn with (RavebridgeReveal), on the prefab Resources/Ravebridge/Reveal Props:
/// the soft circle for the mist and the dust, the bright one for the spray.
/// </summary>
public sealed class RavebridgeRevealProps : MonoBehaviour
{
    public Material Mist;
    public Material Spray;
    public Material Dust;

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
