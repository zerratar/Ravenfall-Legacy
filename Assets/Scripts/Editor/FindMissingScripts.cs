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
        [MenuItem("Ravenfall/Tools/Generate Redeemable Row")]
        public static void ShowWindow()
        {
            var editor = EditorWindow.GetWindow<GenerateRedeemableRow>();
            editor.itemManager = new JsonBasedItemRepository(@"C:\git\Ravenfall-Legacy-new\Data\Repositories\items.json");
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
    public class FindMissingScripts : EditorWindow
    {
        [MenuItem("Ravenfall/Tools/FindMissingScripts")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(FindMissingScripts));
        }

        public void OnGUI()
        {
            if (GUILayout.Button("Find Missing Scripts in selected prefabs"))
            {
                FindInSelected();
            }
        }
        private static void FindInSelected()
        {
            GameObject[] go = Selection.gameObjects;
            int go_count = 0, components_count = 0, missing_count = 0;
            foreach (GameObject g in go)
            {
                go_count++;
                Component[] components = g.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    components_count++;
                    if (components[i] == null)
                    {
                        missing_count++;
                        string s = g.name;
                        Transform t = g.transform;
                        while (t.parent != null)
                        {
                            s = t.parent.name + "/" + s;
                            t = t.parent;
                        }
                        Debug.Log(s + " has an empty script attached in position: " + i, g);
                    }
                }
            }

            Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
        }
    }
}
