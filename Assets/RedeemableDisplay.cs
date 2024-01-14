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

        cost.gameObject.SetActive(false);
        cost.text = "";
    }

    public void SetRedeemableItem(Item item, RavenNest.Models.RedeemableItem details, float maxRedeemableHeight = 2f)
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

        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer && renderer.bounds.size.y > maxRedeemableHeight)
        {
            // scale so we fit the max redeemable height
            var currentScale = obj.transform.localScale;
            var scale = maxRedeemableHeight / renderer.bounds.size.y;
            var newScale = new Vector3(scale * currentScale.x, scale * currentScale.y, scale * currentScale.z);
            obj.transform.localScale = newScale;
        }

        cost.text = string.Format("<size=12>{0}\n<color=#FF00FF><size=10>Costs {1} Tokens", item.Name, details.Cost);
        cost.gameObject.SetActive(true);

        // make sure the position of this object is center of the cost
        var targetPosition = cost.bounds.center + cost.rectTransform.position;

        // we have to ensure all individual objects maintain their position so they dont fly around.
        var display_pos = display.position;
        var displayPosition_pos = displayPosition.position;
        var cost_pos = cost.rectTransform.position;

        // set the new position of this object to targetPosition
        this.transform.position = targetPosition;

        // reset the position of all the other objects
        display.position = display_pos;
        displayPosition.position = displayPosition_pos;
        cost.rectTransform.position = cost_pos;
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
