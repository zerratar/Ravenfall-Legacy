using UnityEngine;

/// <summary>
/// The parts boats are assembled from (docs/own-boats.md, "Customizing boats"): Synty's Pirate Realm small ship in
/// every variant, its sails, flags and plank woods. Made by tools/sailing/make-pirate-realm-boats.cs, referenced from
/// the scene through OwnBoatPrefabs. Index 0 is variant 1.
/// </summary>
[CreateAssetMenu(menuName = "Ravenfall/Boat Parts")]
public sealed class BoatParts : ScriptableObject
{
    [Header("The small ship (the Sloop)")]
    public GameObject[] Hulls;
    public GameObject[] Fronts;
    public GameObject[] Middles;
    public GameObject[] Backs;
    public GameObject[] Figureheads;
    public GameObject[] Rams;
    /// <summary>
    /// The same for every look, taken from the kit's preset as it sets them: the mast with its booms and sails, the
    /// bowsprit, rigging, helm and grate, placed against the hull's origin.
    /// </summary>
    public GameObject Rig;

    [Header("Looks by material")]
    public Material[] Sails;
    public Material[] Flags;
    public Material[] Woods;

    [Header("The flag and, on the small boats, its pole")]
    public GameObject Flag;
    public GameObject FlagPole;
}
