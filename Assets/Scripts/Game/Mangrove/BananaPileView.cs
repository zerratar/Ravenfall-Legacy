using UnityEngine;

/// <summary>
/// The monkeys' banana pile as a heap of bunches by the chief (docs/banana-island.md): its children are laid out from
/// the bottom of the heap up (tools/mangrove/mangrove-dress.cs), and as many show as the pile is full (BananaPile), so
/// the heap grows when viewers feed the monkeys and shrinks as they eat or the pirates carry it off.
/// </summary>
public sealed class BananaPileView : MonoBehaviour
{
    private float nextCheck;
    private int shown = -1;

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 2f;

        var count = transform.childCount;
        // a few bunches stay even when the pile is near empty: the monkeys hide some
        var want = Mathf.Clamp(Mathf.CeilToInt(BananaPile.Amount / BananaPile.Most * count), Mathf.Min(3, count), count);
        if (want == shown) return;
        shown = want;
        for (var i = 0; i < count; ++i) transform.GetChild(i).gameObject.SetActive(i < want);
    }
}
