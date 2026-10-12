// Rebuilds only the WORLD navmesh tiles that cover the town, then saves the navmesh asset.
var sb = new System.Text.StringBuilder("NVM|");
if (UnityEngine.Application.isPlaying) { UnityEngine.Debug.Log("NVM|refused"); return null; }
Unity.AI.Navigation.NavMeshSurface surface = null;
foreach (var s in UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(UnityEngine.FindObjectsInactive.Exclude))
    if (s.navMeshData != null && s.collectObjects == Unity.AI.Navigation.CollectObjects.Children) surface = s;
var tr = surface.transform;
sb.Append("surface=").Append(surface.name).Append(" pos=").Append(tr.position).Append(" rot=").Append(tr.rotation.eulerAngles).Append(" scale=").Append(tr.lossyScale).Append(" || ");
var collect = typeof(Unity.AI.Navigation.NavMeshSurface).GetMethod("CollectSources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var watch = System.Diagnostics.Stopwatch.StartNew();
var sources = (System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildSource>)collect.Invoke(surface, null);
sb.Append("sources=").Append(sources.Count).Append(" in ").Append(watch.ElapsedMilliseconds).Append("ms || ");
var hall = new UnityEngine.Vector3(-18.64f, 0f, -69.72f);
// the surface keeps only tiles inside the bounds it is given, so pass the full world bounds the way the package does
var calc = typeof(Unity.AI.Navigation.NavMeshSurface).GetMethod("CalculateWorldBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var local = (UnityEngine.Bounds)calc.Invoke(surface, new object[] { sources });
sb.Append("bounds=").Append(local.center.ToString("F0")).Append(" size ").Append(local.size.ToString("F0")).Append(" || ");
watch.Restart();
var ok = UnityEngine.AI.NavMeshBuilder.UpdateNavMeshData(surface.navMeshData, surface.GetBuildSettings(), sources, local);
sb.Append("update ok=").Append(ok).Append(" in ").Append(watch.ElapsedMilliseconds).Append("ms || ");
UnityEditor.EditorUtility.SetDirty(surface.navMeshData);
UnityEditor.AssetDatabase.SaveAssets();
surface.RemoveData(); surface.AddData();
// sanity: the plaza and a few plots should be on the navmesh
var plaza = hall + new UnityEngine.Vector3(-5f, 8f, -40f);
UnityEngine.AI.NavMeshHit hit;
sb.Append("plaza onMesh=").Append(UnityEngine.AI.NavMesh.SamplePosition(plaza, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas)).Append(" ");
var onMesh = 0; var total = 0;
foreach (var slot in UnityEngine.Object.FindObjectsByType<TownHouseSlot>(UnityEngine.FindObjectsInactive.Include)) { total++; if (UnityEngine.AI.NavMesh.SamplePosition(slot.transform.position, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) onMesh++; }
sb.Append("plots on mesh ").Append(onMesh).Append("/").Append(total);
foreach (var isl in UnityEngine.Object.FindObjectsByType<IslandController>(UnityEngine.FindObjectsInactive.Exclude)) sb.Append(" | ").Append(isl.name).Append("=").Append(UnityEngine.AI.NavMesh.SamplePosition(isl.transform.position, out hit, 40f, UnityEngine.AI.NavMesh.AllAreas));
UnityEngine.Debug.Log(sb.ToString());
