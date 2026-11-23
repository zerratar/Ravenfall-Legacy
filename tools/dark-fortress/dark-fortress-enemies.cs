// Run with: unity command eval_file --file tools/dark-fortress/dark-fortress-enemies.cs (edit mode, MainWorld open)
//
// Makes the Dark Fortress enemies (docs/dark-fortress-design.md): one prefab per kind in Assets/World/Enemies/DarkFortress
// and one EnemyDefinitionAsset per kind in Assets/World/Enemies (the tower floors find their templates by the
// definition's id, RavenNest.Combat.Tower's Model). Each is MainWorld's Ravebridge skeleton (its EnemyController,
// movement, colliders, NavMeshAgent and the Humanoid Enemy animator) with the skeleton's bodies taken out and a Synty
// POLYGON Dark Fortress character put in: the character's meshes and bones, and its avatar on the animator. The Dark
// Fortress characters use the same Synty skeleton as the game's (CharactersAvatar), the Behemoth and Colossus a bigger
// one (BR_CharactersAvatar); both play the humanoid enemy animations.
//
// Nothing is added to the open scene: the copy is made in a preview scene and destroyed after saving. The pack is a
// local import (Assets/Synty is not versioned), so the prefabs only work on a machine that has it, like the tower scenes.
// Rerun after changing the table; the tower builders (tower-crypt-build.cs, tower-marsh-build.cs) and
// dark-fortress-templates.cs put the prefabs into the floors' templates.
var log = new System.Text.StringBuilder();
var folder = "Assets/World/Enemies/DarkFortress";
var chars = "Assets/Synty/PolygonDarkFortress/Prefabs/Characters/";
// id | name shown in the editor | character prefab | family (EnemyFamily) | style (0 melee, 2 magic) | attack range | health bar offset
var table = new[]
{
    "df-undead|Risen Dead|SM_Chr_Undead_01|2|0|3.5|0",
    "df-undead-soldier|Risen Soldier|SM_Chr_Undead_02|2|0|3.5|0",
    "df-corpse|Shambling Corpse|SM_Chr_Corpse_01|2|0|3.5|0",
    "df-plague|Plague Walker|SM_Chr_Plague_01|2|0|3.5|0",
    "df-wraith|Crypt Wraith|SM_Chr_Wraith_01|5|2|3.5|0.2",
    "df-grave-knight|Grave Knight|SM_Chr_Knight_01|2|0|3.5|0.1",
    "df-behemoth|Behemoth|SM_Chr_Behemoth_01|2|0|3.5|0.2",
    // round 2 (2026-11-23): elites and the band 1 bosses
    "df-inquisitor|Inquisitor|SM_Chr_Inquisitor_01|0|2|3.5|0.1",
    "df-barbarian|Barbarian|SM_Chr_Barbarian_01|0|0|3.5|0.1",
    "df-apothecary|Apothecary|SM_Chr_Apothecary_01|0|2|3.5|0.1",
    "df-necromancer|Necromancer|SM_Chr_Necromancer_01|0|2|3.5|0.2",
    "df-colossus|Colossus|SM_Chr_Colossus_01|2|0|3.5|0.2",
    "df-plague-lord|Plague Lord|SM_Chr_Plague_Lord_01|2|2|3.5|0.3",
};
var main = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all = UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
EnemyController source = null;
foreach (var e in all)
    if (e.gameObject.scene == main && e.Definition && e.Definition.Definition != null && e.Definition.Definition.Id == "ravebridge-skeleton" && !e.IsRaidBoss && !e.IsDungeonBoss) { source = e; break; }
if (!source) return "no ravebridge-skeleton in the open scene";
var sourceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>("Assets/World/Enemies/ravebridge-skeleton.asset");
if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try
{
    foreach (var row in table)
    {
        var c = row.Split('|');
        var id = c[0];
        var charPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(chars + c[2] + ".prefab");
        if (!charPrefab) { log.AppendLine("no character " + c[2]); continue; }

        // the definition: the skeleton's numbers (a tower floor replaces them with its own), this kind's id
        var assetPath = "Assets/World/Enemies/" + id + ".asset";
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDefinitionAsset>(assetPath);
        if (!asset)
        {
            asset = UnityEngine.ScriptableObject.CreateInstance<EnemyDefinitionAsset>();
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
        }
        var json = UnityEngine.JsonUtility.ToJson(sourceAsset.Definition);
        UnityEngine.JsonUtility.FromJsonOverwrite(json, asset.Definition);
        asset.Definition.Id = id;
        asset.Definition.Name = c[1];
        asset.Definition.Area = "Tower, Dark Fortress (" + c[1] + ")";
        asset.Definition.Family = (RavenNest.Combat.EnemyFamily)int.Parse(c[3]);
        asset.Definition.Style = (RavenNest.Combat.AttackStyle)int.Parse(c[4]);
        UnityEditor.EditorUtility.SetDirty(asset);

        // the body: the skeleton enemy without its bodies, with the character's
        var holder = new UnityEngine.GameObject("holder");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, preview);
        var copy = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(source.gameObject, holder.transform);
        copy.transform.SetParent(null);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, preview);
        UnityEngine.Object.DestroyImmediate(holder);
        copy.name = id;
        copy.transform.SetPositionAndRotation(UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
        copy.transform.localScale = UnityEngine.Vector3.one;
        for (var i = copy.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
        var body = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(charPrefab, copy.transform);
        var bodyAnimator = body.GetComponent<UnityEngine.Animator>();
        var avatar = bodyAnimator ? bodyAnimator.avatar : null;
        while (body.transform.childCount > 0)
        {
            var ch = body.transform.GetChild(0);
            ch.SetParent(copy.transform, false);
        }
        UnityEngine.Object.DestroyImmediate(body);
        foreach (var r in copy.GetComponentsInChildren<UnityEngine.Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (r is UnityEngine.SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
        }
        var animator = copy.GetComponent<UnityEngine.Animator>();
        animator.avatar = avatar;
        animator.applyRootMotion = false;

        var enemy = copy.GetComponent<EnemyController>();
        enemy.Definition = asset;
        enemy.Island = null;
        var so = new UnityEditor.SerializedObject(enemy);
        var range = so.FindProperty("attackRange"); if (range != null) range.floatValue = float.Parse(c[5], System.Globalization.CultureInfo.InvariantCulture);
        var bar = so.FindProperty("healthBarOffset"); if (bar != null) bar.floatValue = float.Parse(c[6], System.Globalization.CultureInfo.InvariantCulture);
        so.ApplyModifiedPropertiesWithoutUndo();

        var path = folder + "/" + id + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(copy, path);
        UnityEngine.Object.DestroyImmediate(copy);
        log.AppendLine("saved " + path + " avatar=" + (avatar ? avatar.name : "none"));
    }
    UnityEditor.AssetDatabase.SaveAssets();
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
}
return log.ToString();
