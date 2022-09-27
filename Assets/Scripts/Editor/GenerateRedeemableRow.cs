using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets
{

    public class GenerateRedeemableRow : EditorWindow
    {
        [MenuItem("Ravenfall/Tools/Items/Generate Redeemable Row")]
        public static void ShowWindow()
        {
            var editor = EditorWindow.GetWindow<GenerateRedeemableRow>();
            editor.itemManager = new JsonBasedItemRepository();
            editor.loadedItems = editor.itemManager.GetItems();
        }

        private string currencyId;
        private string redeemableId;
        private string cost;
        private JsonBasedItemRepository itemManager;
        private List<Item> loadedItems;

        public void OnGUI()
        {
            if (loadedItems == null) return;


            GUILayout.Label("Redeemable ID or Name");
            redeemableId = GUILayout.TextField(redeemableId);

            GUILayout.Label("Currency ID or Name");
            currencyId = GUILayout.TextField(currencyId);

            GUILayout.Label("Cost");
            cost = GUILayout.TextField(cost);

            if (GUILayout.Button("Generate"))
            {
                if (!System.Guid.TryParse(redeemableId, out var redeemableIdGuid))
                {
                    var item = loadedItems.FirstOrDefault(x => x.Name.Equals(redeemableId, StringComparison.OrdinalIgnoreCase));
                    if (item == null) return;
                    redeemableIdGuid = item.Id;
                }

                if (!System.Guid.TryParse(currencyId, out var currencyIdGuid))
                {
                    var item = loadedItems.FirstOrDefault(x => x.Name.Equals(currencyId, StringComparison.OrdinalIgnoreCase));
                    if (item == null) return;
                    currencyIdGuid = item.Id;
                }

                var rowId = System.Guid.NewGuid().ToString();

                var str = string.Join("\t", new string[] {
                    rowId,
                    redeemableIdGuid.ToString(),
                    currencyIdGuid.ToString(),
                    cost,
                    "1"
                });

                GUIUtility.systemCopyBuffer = str;
                UnityEngine.Debug.Log(str);
            }
        }
    }
}
