using RavenNest.Models;
using Sirenix.OdinInspector;
using UnityEngine;

public class RedeemableDisplay : MonoBehaviour
{
    [SerializeField] private Transform display;
    [SerializeField] private Transform displayPosition;
    [SerializeField] private TMPro.TextMeshPro cost;

    public System.DateTime LastChanged;
    public RavenNest.Models.ItemType Type;

    public void ClearRedeemableItem()
    {
        if (displayPosition.childCount > 0)
        {
            var c = displayPosition.GetChild(0);
            GameObject.DestroyImmediate(c.gameObject);
        }

        cost.text = "";
    }

    public void SetRedeemableItem(Item item, RavenNest.Models.RedeemableItem details)
    {
        if (displayPosition.childCount > 0)
        {
            var c = displayPosition.GetChild(0);
            GameObject.DestroyImmediate(c.gameObject);
            //DestroyImmediately(c.gameObject);
        }

        var itemObj = UnityEngine.Resources.Load<GameObject>(item.GenericPrefab);
        var obj = Instantiate(itemObj);
        obj.transform.SetParent(displayPosition, false);
        obj.transform.localRotation = Quaternion.identity;
        cost.text = string.Format("<size=12>{0}\n<color=#880088><size=10>Costs {1} Tokens", item.Name, details.Cost);
    }

    [Button("Create Redeemable Spot")]
    private void CreateRedeemableSpot()
    {
        if (!cost)
        {
            cost = GetComponentInChildren<TMPro.TextMeshPro>(true);
        }

        if (!display)
        {
            display = this.transform.Find("Display");
        }

        if (displayPosition)
        {
            AdjustDisplayPosition();
            return;
        }

        var go = new GameObject("DisplayPosition");
        go.transform.SetParent(this.transform);
        displayPosition = go.transform;
        //go.transform.SetPositionAndRotation()
        AdjustDisplayPosition();
    }

    private void AdjustDisplayPosition()
    {
        var meshRenderer = display.GetComponent<MeshRenderer>();
        var bounds = meshRenderer.bounds;

        //bounds.max.y

        var pos = display.position;
        pos.y = bounds.max.y;
        pos.x = bounds.center.x;
        pos.z = bounds.center.z;
        //pos.y += (display.localScale.y * yOffset);
        displayPosition.SetPositionAndRotation(pos, display.rotation);
    }
}
