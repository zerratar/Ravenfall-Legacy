using Newtonsoft.Json;
using RavenNest.Models;
using Sirenix.OdinInspector;
using System;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class RedeemableDisplayCollection : MonoBehaviour
{
    public float MaxRedeemableHeight = 2f;
    public RedeemableSeason Season;
    public RedeemableDisplay[] Displays;
    private Item[] items;
    private RedeemableItemCollection redeemables;
    private DateTime lastRedeemableDownload;
    private DateTime lastItemDownload;

#if UNITY_EDITOR
    [Button("Update Redeemables")]
    private async void UpdateRedeemables()
    {
        if (Displays == null || Displays.Length == 0)
        {
            Displays = GetComponentsInChildren<RedeemableDisplay>().OrderBy(x => x.name).ToArray();
        }

        var now = DateTime.Now;
        var forceRefreshRedeemables = redeemables == null || now - lastRedeemableDownload > TimeSpan.FromMinutes(5);
        EditorUtility.DisplayProgressBar("Adjusting Redeemable Displays", "Download redeemable items...", 0);
        await Task.Run(() => EnsureRedeemableRepository(forceRefreshRedeemables));

        var forceRefreshItems = items == null || now - lastItemDownload > TimeSpan.FromMinutes(5);
        EditorUtility.DisplayProgressBar("Adjusting Redeemable Displays", "Download items...", 0.25f);
        await Task.Run(() => EnsureItemRepository(forceRefreshItems));

        EditorUtility.DisplayProgressBar("Adjusting Redeemable Displays", "Clearing displays...", 0.3f);
        foreach (var d in Displays)
        {
            d.ClearRedeemableItem();
        }

        System.Guid currencyId = GetCurrencyId();

        var i = 0f;
        var max = redeemables.Count;

        var p = Mathf.Lerp(0.3f, 1f, i / max);
        EditorUtility.DisplayProgressBar("Adjusting Redeemable Displays", "Update redeemable (" + (i + 1) + "/" + max + ")", p);

        foreach (var r in redeemables.OrderBy(x => x.Cost))
        {
            var j = i++;
            p = Mathf.Lerp(0.3f, 1f, j / max);
            var item = items.FirstOrDefault(x => x.Id == r.ItemId);
            if (item == null || r.CurrencyItemId != currencyId)
            {
                continue;
            }

            var display = Displays.FirstOrDefault(x => x.Type == item.Type && x.LastChanged < now);
            if (display != null)
            {
                display.LastChanged = now;
                display.SetRedeemableItem(item, r, MaxRedeemableHeight);
            }

            EditorUtility.DisplayProgressBar("Adjusting Redeemable Displays", "Update redeemable (" + (i + 1) + "/" + max + ")", p);
        }

        EditorUtility.ClearProgressBar();
        //var redeemableItems = redeemables
        //    .Where(x => x.CurrencyItemId == currencyId)
        //    .OrderBy(x => x.Cost).ThenBy(x => items.FirstOrDefault(y => y.Id == x.ItemId).Name)
        //    .ToArray();
    }
#endif
    private Guid GetCurrencyId()
    {
        var item = items.FirstOrDefault(x => x.Name == Season + " Token");
        if (item != null)
        {
            return item.Id;
        }

        return Guid.Empty;
    }

    private void EnsureRedeemableRepository(bool forceRefresh = true)
    {
        var redeemableRepo = @"G:\Ravenfall\Projects\Ravenfall Legacy\Data\Repositories\redeemable.json";

        if (forceRefresh || !System.IO.File.Exists(redeemableRepo))
        {
            System.Net.WebClient cl = new System.Net.WebClient();
            lastRedeemableDownload = DateTime.Now;
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
        }

        var json = System.IO.File.ReadAllText(redeemableRepo);
        redeemables = JsonConvert.DeserializeObject<RedeemableItemCollection>(json);
    }

    private void EnsureItemRepository(bool forceRefresh = true)
    {
        var itemsRepo = @"G:\Ravenfall\Projects\Ravenfall Legacy\Data\Repositories\items.json";
        if (forceRefresh || !System.IO.File.Exists(itemsRepo))
        {
            System.Net.WebClient cl = new System.Net.WebClient();
            lastItemDownload = DateTime.Now;
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
        }

        var json = System.IO.File.ReadAllText(itemsRepo);

        //var json = System.IO.File.ReadAllText(@"G:\Ravenfall\Projects\Ravenfall Legacy\Data\Repositories\items.json");
        items = JsonConvert.DeserializeObject<Item[]>(json);
    }
}
