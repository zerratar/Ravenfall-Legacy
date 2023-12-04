using Newtonsoft.Json;
using RavenNest.Models;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;

public class RedeemableDisplayCollection : MonoBehaviour
{
    public RedeemableSeason Season;
    public RedeemableDisplay[] Displays;
    private Item[] items;
    private RedeemableItemCollection redeemables;

    [Button("Update Redeemables")]
    private void UpdateRedeemables()
    {
        if (Displays == null || Displays.Length == 0)
        {
            Displays = GetComponentsInChildren<RedeemableDisplay>().OrderBy(x => x.name).ToArray();
        }

        EnsureRedeemableRepository();
        EnsureItemRepository();

        foreach(var d in Displays)
        {
            d.ClearRedeemableItem();
        }

        System.Guid currencyId = GetCurrencyId();
        var now = System.DateTime.Now;
        foreach (var r in redeemables.OrderBy(x => x.Cost))
        {
            var item = items.FirstOrDefault(x => x.Id == r.ItemId);
            if (item == null || r.CurrencyItemId != currencyId)
            {
                continue;
            }

            var display = Displays.FirstOrDefault(x => x.Type == item.Type && x.LastChanged < now);
            if (display != null)
            {
                display.LastChanged = now;
                display.SetRedeemableItem(item, r);
            }
        }

        //var redeemableItems = redeemables
        //    .Where(x => x.CurrencyItemId == currencyId)
        //    .OrderBy(x => x.Cost).ThenBy(x => items.FirstOrDefault(y => y.Id == x.ItemId).Name)
        //    .ToArray();
    }

    private Guid GetCurrencyId()
    {
        var item = items.FirstOrDefault(x => x.Name == Season + " Token");
        if (item != null)
        {
            return item.Id;
        }

        return Guid.Empty;
    }

    private void EnsureRedeemableRepository()
    {
        var redeemableRepo = @"C:\git\Ravenfall Legacy\Data\Repositories\redeemable.json";

        System.Net.WebClient cl = new System.Net.WebClient();
        try
        {
            cl.DownloadFile("https://localhost:5001/api/items/redeemable", redeemableRepo);
            Shinobytes.Debug.Log("Downloaded new items repo from dev");
        }
        catch
        {
            cl.DownloadFile("https://www.ravenfall.stream/api/items/redeemable", redeemableRepo);
            Shinobytes.Debug.Log("Downloaded new items repo from production");
        }

        var json = System.IO.File.ReadAllText(redeemableRepo);
        redeemables = JsonConvert.DeserializeObject<RedeemableItemCollection>(json);
    }

    private void EnsureItemRepository()
    {
        var itemsRepo = @"C:\git\Ravenfall Legacy\Data\Repositories\items.json";

        System.Net.WebClient cl = new System.Net.WebClient();
        try
        {
            cl.DownloadFile("https://localhost:5001/api/items", itemsRepo);
            Shinobytes.Debug.Log("Downloaded new items repo from dev");
        }
        catch
        {
            cl.DownloadFile("https://www.ravenfall.stream/api/items", itemsRepo);
            Shinobytes.Debug.Log("Downloaded new items repo from production");
        }

        var json = System.IO.File.ReadAllText(itemsRepo);

        //var json = System.IO.File.ReadAllText(@"C:\git\Ravenfall Legacy\Data\Repositories\items.json");
        items = JsonConvert.DeserializeObject<Item[]>(json);
    }
}
