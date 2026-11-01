using UnityEngine;

/// <summary>
/// The parts boats are assembled from (docs/own-boats.md, "Customizing boats"): Synty's Pirate Realm ships in three
/// sizes, each in every variant, and the sails, flags and plank woods they share. Made by
/// tools/sailing/make-pirate-realm-boats.cs, referenced from the scene through OwnBoatPrefabs. Index 0 is variant 1.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Boat Parts")]
public sealed class BoatParts : ScriptableObject
{
    [System.Serializable]
    public sealed class Ship
    {
        public GameObject[] Hulls;
        public GameObject[] Fronts;
        public GameObject[] Middles;
        public GameObject[] Backs;
        public GameObject[] Figureheads;
        public GameObject[] Rams;
        /// <summary>
        /// The same for every look, taken from the kit's preset as it sets them: the masts with their booms and sails,
        /// the bowsprit, rigging, ratlines, helm and grates, placed against the hull's origin.
        /// </summary>
        public GameObject Rig;
    }

    /// <summary>The Sloop, the Brigantine and the Galleon.</summary>
    public Ship Small;
    public Ship Medium;
    public Ship Large;

    public Ship For(BoatItems.Tier tier) => tier switch
    {
        BoatItems.Tier.Galleon => Large,
        BoatItems.Tier.Brigantine => Medium,
        BoatItems.Tier.Sloop => Small,
        _ => null,
    };

    [Header("Looks by material")]
    public Material[] Sails;
    public Material[] Flags;
    public Material[] Woods;

    [Header("The flag and, on the small boats, its pole")]
    public GameObject Flag;
    public GameObject FlagPole;
}
