using UnityEngine;

/// <summary>
/// The own boats' prefabs (Assets/World/Sailing/Boats, made by tools/sailing/make-boat-prefabs.cs), referenced from the
/// scene so builds carry them; Resources is not versioned. Set up by tools/sailing/add-boat-prefabs.cs.
/// </summary>
public sealed class OwnBoatPrefabs : MonoBehaviour
{
    [SerializeField] private GameObject rowboat;
    [SerializeField] private GameObject fishingBoat;
    [SerializeField] private GameObject sloop;

    private static OwnBoatPrefabs instance;

    private void Awake() => instance = this;

    public static GameObject Get(BoatItems.Tier tier)
    {
        if (!instance) instance = FindAnyObjectByType<OwnBoatPrefabs>();
        if (!instance) return null;
        return tier switch
        {
            BoatItems.Tier.Sloop => instance.sloop,
            BoatItems.Tier.FishingBoat => instance.fishingBoat,
            _ => instance.rowboat,
        };
    }
}
