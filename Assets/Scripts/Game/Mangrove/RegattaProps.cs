using UnityEngine;

/// <summary>
/// What the regatta puts on the water and the effects it plays (docs/banana-island.md, "The regatta"): the mystery
/// bottles, the powder kegs, the banana peels, the reefs and the wreckage, the cannonball and the Kraken's tentacles, the splashes, blasts and sparkles, the start's horn and the Kraken's roar. One prefab in Resources ("Mangrove/Regatta Props", made by
/// tools/mangrove/regatta-props.cs) holds them all.
/// </summary>
public sealed class RegattaProps : MonoBehaviour
{
    public GameObject Bottle;
    public GameObject Keg;
    public GameObject Peel;
    public GameObject Reef;
    public GameObject Wreck;
    public GameObject Cannonball;
    public GameObject Tentacle;

    public GameObject Splash;
    public GameObject Blast;
    public GameObject CannonShot;
    public GameObject Sparkle;
    public GameObject Bubbles;

    public AudioClip Horn;
    public AudioClip Roar;

    private static RegattaProps loaded;
    private static bool tried;

    public static RegattaProps Get()
    {
        if (loaded || tried) return loaded;
        tried = true;
        loaded = Resources.Load<RegattaProps>("Mangrove/Regatta Props");
        if (!loaded) Shinobytes.Debug.LogWarning("The regatta's props (Resources/Mangrove/Regatta Props) are missing.");
        return loaded;
    }
}
