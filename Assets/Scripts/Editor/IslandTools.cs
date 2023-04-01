using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    public class IslandTools
    {

        [MenuItem("Ravenfall/Tools/Update Signpost Lv Requirements")]
        public static void UpdateSignposts()
        {
            foreach (var sp in GameObject.FindObjectsOfType<Signpost>())
            {
                sp.Bruteforce();
            }
        }
    }
}
