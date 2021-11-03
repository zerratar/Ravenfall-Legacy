using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class OnsenController : MonoBehaviour
{
    [SerializeField] private Transform entryPointTransform;

    [SerializeField] private Transform sittingSpots;
    [SerializeField] private Transform swimmingSpots;
    [SerializeField] private Transform meditatingSpots;

    [SerializeField] private TextMeshPro detailsLabel;

    private IslandController island;

    private List<Transform> transforms;
    public IslandController Island => island;
    public Vector3 EntryPoint => entryPointTransform.position;

    void Start()
    {
        this.island = GetComponentInParent<IslandController>();

        this.transforms = new List<Transform>();
        transforms.AddRange(sittingSpots.GetComponentsInChildren<Transform>().Where(x => x.childCount == 0).ToArray());
        transforms.AddRange(swimmingSpots.GetComponentsInChildren<Transform>().Where(x => x.childCount == 0).ToArray());
        transforms.AddRange(meditatingSpots.GetComponentsInChildren<Transform>().Where(x => x.childCount == 0).ToArray());

        UpdateDetailsLabel();
    }

    public OnsenPosition GetNextAvailableSpot()
    {
        var availableCount = GetAvailableSpotCount();
        if (availableCount == 0)
        {
            return null;
        }

        var spot = transforms.Where(x => x.childCount == 0).OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (spot)
        {
            return new OnsenPosition
            {
                Target = spot,
                Type = GetSpotTypeByName(spot.name)
            };
        }

        return null;
    }

    private OnsenPositionType GetSpotTypeByName(string name)
    {
        switch (name)
        {
            case "Swim": return OnsenPositionType.Swimming;
            case "Meditate": return OnsenPositionType.Meditating;
            default: return OnsenPositionType.Sitting;
        }
    }

    public void UpdateDetailsLabel()
    {
        if (!detailsLabel)
        {
            return;
        }

        var usedCount = GetUsedSpotCount();
        var totalCount = GetSpotCount();
        if (usedCount >= totalCount)
        {
            detailsLabel.text = "FULL";
        }
        else
        {
            detailsLabel.text = usedCount + "/" + totalCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetUsedSpotCount()
    {
        return transforms.Count(x => x.childCount > 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetAvailableSpotCount()
    {
        return transforms.Count(x => x.childCount == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSpotCount()
    {
        return transforms.Count;
    }
}

public class OnsenPosition
{
    public Transform Target;
    public OnsenPositionType Type;
}

public enum OnsenPositionType
{
    Sitting,
    Swimming,
    Meditating
}
