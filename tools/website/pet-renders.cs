// Renders every battle pet species (and the metalon's three stages) to PNGs in the scratchpad pets/ folder, in edit
// mode: the model copied without scripts, posed on a frame of its idle clip, transparent background. Writes done.txt.
var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ravenfall-pet-renders"); // then: python tools/website/pet-renders.py
System.IO.Directory.CreateDirectory(outDir);
var log = new System.Text.StringBuilder();
foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
    if (root && (UnityEngine.Mathf.Abs(root.transform.position.y - 2000) < 0.01f || root.name == "pet render holder")) UnityEngine.Object.DestroyImmediate(root);
RuntimePreviewGenerator.MarkTextureNonReadable = false;
var holder = new UnityEngine.GameObject("pet render holder");
holder.SetActive(false);
UnityEngine.GameObject FindEnemy(string name)
{
    foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include))
    {
        if (!e) continue;
        var d = e.CombatDefinition;
        if (d != null && string.Equals(d.Name, name, System.StringComparison.OrdinalIgnoreCase) && PetModels.KindName(e.name) == name) return e.gameObject;
        if (PetModels.KindName(e.name) == name) return e.gameObject;
    }
    foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyController>(UnityEngine.FindObjectsInactive.Include))
        if (e && e.CombatDefinition != null && string.Equals(e.CombatDefinition.Name, name, System.StringComparison.OrdinalIgnoreCase)) return e.gameObject;
    return null;
}
void Render(string key, string file)
{
    UnityEngine.GameObject source = key.StartsWith("enemy:") ? FindEnemy(key.Substring(6)) : UnityEngine.Resources.Load<UnityEngine.GameObject>(key.StartsWith("pet:") ? key.Substring(4) : key);
    if (!source) { log.AppendLine("MISSING " + key); return; }
    var copy = UnityEngine.Object.Instantiate(source, holder.transform);
    copy.transform.localPosition = UnityEngine.Vector3.zero;
    copy.transform.localRotation = UnityEngine.Quaternion.identity;
    var animator = copy.GetComponentInChildren<UnityEngine.Animator>(true);
    var clips = animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.animationClips : new UnityEngine.AnimationClip[0];
    foreach (var mb in copy.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (mb) UnityEngine.Object.DestroyImmediate(mb);
    foreach (var c in copy.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) UnityEngine.Object.DestroyImmediate(c);
    foreach (var c in copy.GetComponentsInChildren<UnityEngine.Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
    foreach (var c in copy.GetComponentsInChildren<UnityEngine.Canvas>(true)) if (c) UnityEngine.Object.DestroyImmediate(c.gameObject == copy ? (UnityEngine.Object)c : c.gameObject);
    foreach (var c in copy.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) if (c) UnityEngine.Object.DestroyImmediate(c.gameObject == copy ? (UnityEngine.Object)c : c.gameObject);
    foreach (var c in copy.GetComponentsInChildren<UnityEngine.Light>(true)) c.enabled = false;
    var renderers = copy.GetComponentsInChildren<UnityEngine.Renderer>(true);
    if (!renderers.Any(r => r.enabled)) foreach (var r in renderers) r.enabled = true;
    copy.transform.SetParent(null, true);
    copy.transform.position = new UnityEngine.Vector3(0, 2000, 0);
    copy.transform.rotation = UnityEngine.Quaternion.Euler(0, key == "enemy:Bunny" || key == "enemy:Cow" ? 20 : 200, 0);
    copy.SetActive(true);
    // a frame of its idle clip (bind pose otherwise: T-poses and folded wings)
    var idle = clips.Where(c => c && c.name.ToLowerInvariant().Contains("idle")).OrderBy(c => c.name.Length).FirstOrDefault() ?? clips.FirstOrDefault();
    if (idle && animator) idle.SampleAnimation(animator.gameObject, idle.length * 0.3f);
    RuntimePreviewGenerator.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
    RuntimePreviewGenerator.OrthographicMode = false;
    RuntimePreviewGenerator.Padding = 0.06f;
    RuntimePreviewGenerator.PreviewDirection = new UnityEngine.Vector3(-0.6f, -0.35f, -1f);
    var tex = RuntimePreviewGenerator.GenerateModelPreview(copy.transform, 1024, 1024, false, true);
    try { if (tex) System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file + ".png"), tex.EncodeToPNG()); } catch (System.Exception exc) { log.AppendLine("encode " + file + ": " + exc.Message); }
    log.AppendLine((tex ? "ok " : "FAILED ") + file + " <- " + key + " idle " + (idle ? idle.name : "-"));
    UnityEngine.Object.DestroyImmediate(copy);
}
try
{
    foreach (var s in RavenNest.Combat.BattlePets.Species)
    {
        if (s.Source == RavenNest.Combat.PetSource.Companion) Render(s.ItemPrefabs[0], s.Id);
        else
        {
            Render(s.ModelAt(0), s.Id);
            if (s.StageModels.Length > 1) for (var st = 1; st < s.StageModels.Length; ++st) Render(s.ModelAt(st), s.Id + "-" + st);
        }
    }
}
catch (System.Exception exc) { log.AppendLine("ERROR " + exc); }
UnityEngine.Object.DestroyImmediate(holder);
System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "done.txt"), log.ToString());
return "started";
