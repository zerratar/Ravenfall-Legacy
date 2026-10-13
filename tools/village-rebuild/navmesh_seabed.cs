// Adds (or updates) a Not Walkable volume under WORLD that covers the sea floor, so the navmesh stops
// at the water line. Without it the whole ocean floor at y -30 is walkable, and a teleport over water
// can drop a player onto it with no way back to land. Run navmesh_full.cs afterwards to rebake.
// The volume spans the whole terrain from 0.7 m under the water surface (y -3.31) down to just below
// the sea floor (y -30). The dungeons sit at y -56 and below and stay outside it.
var sb = new System.Text.StringBuilder("SEABED|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("SEABED|refused in play mode"); return null; }
Unity.AI.Navigation.NavMeshSurface surface = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(UnityEngine.FindObjectsInactive.Exclude))
    if (s.navMeshData != null && s.collectObjects == Unity.AI.Navigation.CollectObjects.Children) surface = s;
const float top = -4f, bottom = -31f;
// refuse if any dungeon reaches into the band (inactive renderers report empty bounds at the origin, so skip those)
foreach (var d in UnityEngine.Object.FindObjectsByType<DungeonController>(UnityEngine.FindObjectsInactive.Include))
    foreach (var r in d.GetComponentsInChildren<UnityEngine.Renderer>(true))
        if (r.gameObject.activeInHierarchy && r.enabled && r.bounds.size.sqrMagnitude > 0f && r.bounds.max.y > bottom) { UnityEngine.Debug.Log("SEABED|refused: " + d.name + "/" + r.name + " reaches y " + r.bounds.max.y.ToString("F1")); return null; }
var terrain = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Terrain>();
var tp = terrain.transform.position; var ts = terrain.terrainData.size;
var center = new UnityEngine.Vector3(tp.x + ts.x * 0.5f, (top + bottom) * 0.5f, tp.z + ts.z * 0.5f);
var size = new UnityEngine.Vector3(ts.x + 40f, top - bottom, ts.z + 40f);
var existing = surface.transform.Find("Sea Floor Not Walkable");
var go = existing ? existing.gameObject : new UnityEngine.GameObject("Sea Floor Not Walkable");
if (!existing) UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Sea floor volume");
go.transform.SetParent(surface.transform, false);
go.transform.position = center;
go.transform.rotation = UnityEngine.Quaternion.identity;
var ls = surface.transform.lossyScale;
go.transform.localScale = new UnityEngine.Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
var vol = go.GetComponent<Unity.AI.Navigation.NavMeshModifierVolume>() ?? go.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
vol.center = UnityEngine.Vector3.zero;
vol.size = size;
vol.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
UnityEditor.EditorUtility.SetDirty(go);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
sb.Append("volume under ").Append(surface.name).Append(" world centre ").Append(go.transform.position.ToString("F1")).Append(" size ").Append(size.ToString("F0"))
  .Append(" area ").Append(vol.area).Append(" lossy scale ").Append(go.transform.lossyScale.ToString("F2")).Append(" surface rot ").Append(surface.transform.rotation.eulerAngles.ToString("F0"));
UnityEngine.Debug.Log(sb.ToString());
return null;
