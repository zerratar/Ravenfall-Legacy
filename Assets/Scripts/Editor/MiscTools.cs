using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    public class MiscTools
    {
        [MenuItem("Ravenfall/Tools/Hierarchy/Group Selection")]
        static void GroupSelection()
        {
            var go = new GameObject();
            go.name = "New Group";

            var sel = Selection.gameObjects;

            Transform parent = null;
            
            //var parentMatchCount = 0;

            var lowestDepth = int.MaxValue;
            foreach (var o in sel)
            {
                var pos = o.transform.position;
                var rot = o.transform.rotation;
                o.transform.SetParent(go.transform);
                o.transform.position = pos;
                o.transform.rotation = rot;

                var parentDepth = GetParentDepth(o.transform.parent);
                if (parentDepth < lowestDepth)
                {
                    parent = o.transform.parent;
                }
            }

            if (parent)
            {
                go.transform.SetParent(parent);
            }
        }

        [MenuItem("Ravenfall/Tools/Scripts/Remove Missing Script Entries")]
        public static void RemoveMissingScriptEntries()
        {
            foreach (Transform t in Selection.transforms)
            {
                Debug.Log(t.GetComponents(typeof(Component)).Length);
                foreach (Component c in t.GetComponents(typeof(Component)))
                {
                    if (c == null)
                    {
                        Debug.Log("NULL");
                        // throw caution to the wind and destroy anyway!!! AHAHHAHAHAH!!!
                        GameObject.DestroyImmediate(c);
                        // awwww nothing happened.  still there.
                    }
                    else
                        Debug.Log(c.GetType());
                }
            }
        }

        [MenuItem("Ravenfall/Tools/Scripts/Log Missing Script Entries")]
        public static void LogMissingScriptEntries()
        {
            foreach (Transform t in Selection.transforms)
            {
                Debug.Log(t.GetComponents(typeof(Component)).Length);
                foreach (Component c in t.GetComponents(typeof(Component)))
                {
                    if (c == null)
                    {
                        Debug.Log(c?.name);
                        // throw caution to the wind and destroy anyway!!! AHAHHAHAHAH!!!
                        //GameObject.DestroyImmediate(c);
                        // awwww nothing happened.  still there.
                    }
                    //else
                    //    Debug.Log(c.GetType());
                }
            }
        }

        [MenuItem("Ravenfall/Tools/Scripts/Select Missing Scripts")]
        static void SelectMissing(MenuCommand command)
        {
            Transform[] ts = GameObject.FindObjectsOfType<Transform>();
            List<GameObject> selection = new List<GameObject>();
            foreach (Transform t in ts)
            {
                Component[] cs = t.gameObject.GetComponents<Component>();
                foreach (Component c in cs)
                {
                    if (c == null)
                    {
                        selection.Add(t.gameObject);
                        UnityEngine.Debug.Log(t.gameObject + " has missing scripts.");
                    }
                }
            }
            Selection.objects = selection.ToArray();
        }

        private static int GetParentDepth(Transform t)
        {
            var index = 0;
            if (!t)
            {
                return index;
            }

            Transform p;
            do
            {
                p = t.parent;
                if (p && p != null)
                {
                    ++index;
                    continue;
                }
                break;
            } while (true);
            return index;
        }
    }
}
