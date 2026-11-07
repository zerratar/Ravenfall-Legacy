// Run with: unity command eval_file --file tools/combat/apply-enemy-definitions.cs   (MainWorld open, edit mode)
//
// Combat v2 enemy definitions (docs/combat-v2-design.md, section 6). Reads tools/combat/enemy-definitions.json (made
// by make-enemy-definitions.py), creates or updates one EnemyDefinitionAsset per kind in Assets/World/Enemies, points
// every MainWorld enemy at its kind by file ID, gives the dungeon and raid boss prefabs their scaled definitions, and
// marks the scene dirty. The definitions' levels then replace the scene's when an enemy wakes; the 136 island enemies whose levels the
// Unity 6.7 upgrade dropped get them back. Rerunnable: existing assets are updated in place, so references hold.
//
// Save the scene afterwards in its own call (it takes longer than the eval's 5 s, and finishes anyway):
//   unity command eval "UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());"
// The result is also written to Temp/apply-enemy-definitions.txt.
var root = System.IO.Directory.GetCurrentDirectory();
var json = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "tools/combat/enemy-definitions.json")));
var resultFile = System.IO.Path.Combine(root, "Temp/apply-enemy-definitions.txt");
var log = new System.Text.StringBuilder();
    try
    {
        const string folder = "Assets/World/Enemies";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets/World", "Enemies");

        var serializer = new Newtonsoft.Json.JsonSerializer();
        serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        var assets = new System.Collections.Generic.Dictionary<string, EnemyDefinitionAsset>();
        foreach (var item in (Newtonsoft.Json.Linq.JArray)json["definitions"])
        {
            var definition = item.ToObject<RavenNest.Combat.EnemyDefinition>(serializer);
            var path = folder + "/" + definition.Id + ".asset";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(path);
            if (!asset)
            {
                asset = UnityEngine.ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
                asset.Definition = definition;
                UnityEditor.AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                asset.Definition = definition;
                UnityEditor.EditorUtility.SetDirty(asset);
            }
            assets[definition.Id] = asset;
        }
        UnityEditor.AssetDatabase.SaveAssets();
        log.AppendLine(assets.Count + " definitions in " + folder);

        // scene enemies, by the EnemyController's file ID
        var assign = (Newtonsoft.Json.Linq.JObject)json["assign"];
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var enemies = UnityEngine.Resources.FindObjectsOfTypeAll<EnemyController>()
            .Where(e => e && e.gameObject.scene == scene).ToList();
        int set = 0, missing = 0;
        var unknown = new System.Collections.Generic.List<string>();
        foreach (var e in enemies)
        {
            var id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(e).targetObjectId.ToString();
            var kind = (string)assign[id];
            if (kind == null) { unknown.Add(e.name); continue; }
            if (!assets.TryGetValue(kind, out var asset)) { missing++; continue; }
            if (e.Definition != asset)
            {
                UnityEditor.Undo.RecordObject(e, "Enemy definition");
                e.Definition = asset;
                UnityEditor.EditorUtility.SetDirty(e);
            }
            set++;
        }
        log.AppendLine(set + " of " + enemies.Count + " scene enemies point at their kind; " + missing + " kinds missing; not in the list: " + string.Join(", ", unknown));

        // the bosses' prefabs: scaled by their encounter, but style, element and resistances come from the definition
        foreach (var (prefabPath, kind) in new[] { ("Assets/Prefabs/DungeonBoss.prefab", "dungeon-boss"), ("Assets/Prefabs/RaidBoss.prefab", "raid-boss") })
        {
            var contents = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
            var boss = contents.GetComponentInChildren<EnemyController>(true);
            if (boss && assets.TryGetValue(kind, out var asset))
            {
                boss.Definition = asset;
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                log.AppendLine(prefabPath + " -> " + kind);
            }
            UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
        }
    }
    catch (System.Exception ex)
    {
        log.AppendLine("failed: " + ex);
    }
    System.IO.File.WriteAllText(resultFile, log.ToString());
return log.ToString();
