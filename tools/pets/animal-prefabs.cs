// The animal pets' prefabs (docs/battle-pets-design.md, "The animals"): one per model key of the animal species in
// RavenNest.Combat's PetSpeciesList ("pet:Pets/Animals/Tiger" is Assets/Resources/Pets/Animals/Tiger.prefab), made from
// the prefab of the same name in polyperfect's Low Poly Animated Animals. Assets/Resources and the pack are not in
// git, so this script is how the prefabs are made again on another machine:
//
//     unity command eval_file --file tools/pets/animal-prefabs.cs
//     (wait for %TEMP%/animal-prefabs.txt to end with "done")
//
// Each prefab keeps the model, its renderers and the Animator with the pack's controller, at scale one (the game
// scales a pet to its stage's height). The pack's scripts (missing in this project), sounds, navigation and colliders
// are taken off, so a build does not pull the pack's sounds in through Resources. The dogs' old materials point to a
// shader that is not in the project: they get the pack's URP ones; the orange cat's prefab had the plain default
// material (a grey cat) and gets the pack's orange. The hen and the reindeer get a controller of their own beside
// their prefab (Hen.controller; Reindeer.controller and Reindeer_Idle.anim), see below for why. Nothing in the pack is
// changed, and no scene is touched: every prefab is built in a preview scene, one per editor frame. 33 files in all.
var pack = "Assets/Low Poly Animated Animals/- Prefabs/Animals/";
var urp = "Assets/polyperfect/Low Poly Animated Animals/Materials/Animals/";
var old = "Assets/Low Poly Animated Animals/- Materials/animals/";
var meshes = "Assets/Low Poly Animated Animals/- Meshes/Animals/";
var outFolder = "Assets/Resources/Pets/Animals";
var report = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "animal-prefabs.txt");

// prefab name (the model key) and the material to put on it instead of its own, when its own is broken or unset
var animals = new (string Name, string Material)[]
{
    ("Panda", null), ("Boar", null),
    ("Horse_Brown", null), ("Horse_Grey", null), ("Horse_Black", null),
    ("Dog_Chihuahua", urp + "Dog_Chihauhau.mat"), ("Dog_GoldenRetriever", urp + "Dog_GoldenRetriever.mat"), ("Dog_GreatDane", urp + "Dog_GreatDane_Brown.mat"),
    ("Capybara", null), ("Sheep", null), ("Sheep_Wool", null),
    ("Tiger", null), ("Eagle", null), ("Goose", null), ("Parrot", null), ("Rat", null),
    ("Lion_Female", null), ("Lion_Male", null),
    ("Spider_Green", null), ("Spider_Yellow", null), ("Spider_Red", null),
    ("Penguin", null), ("Reindeer", null),
    ("Cat_Black", null), ("Cat_Orange", old + "Cat_Orange.mat"), ("Cat_White", null),
    ("Pig", null), ("Chick", null), ("Hen", null), ("Rooster", null),
};

if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/Pets")) return "Assets/Resources/Pets is missing";
if (!UnityEditor.AssetDatabase.IsValidFolder(outFolder)) UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/Pets", "Animals");

var log = new System.Text.StringBuilder();
var index = 0;
System.IO.File.WriteAllText(report, "started\n");

// A controller beside the prefab with the clips named, from the model's own; one of them may be a copy that leaves
// out its first frames (saved beside the prefab under the clip's name, looping).
UnityEditor.Animations.AnimatorController OwnController(string name, string model, string[] clipNames, string trim, int trimFrames)
{
    var path = outFolder + "/" + name + ".controller";
    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)) UnityEditor.AssetDatabase.DeleteAsset(path);
    var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
    var found = new System.Collections.Generic.Dictionary<string, UnityEngine.AnimationClip>();
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(model))
        if (o is UnityEngine.AnimationClip c && !c.name.StartsWith("__preview")) found[c.name] = c;
    foreach (var clipName in clipNames)
    {
        if (!found.TryGetValue(clipName, out var clip)) { log.AppendLine("MISSING clip " + clipName + " in " + model); continue; }
        if (clipName == trim && trimFrames > 0)
        {
            var from = trimFrames / clip.frameRate;
            var copy = new UnityEngine.AnimationClip { frameRate = clip.frameRate };
            foreach (var binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
            {
                var curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);
                var keys = new System.Collections.Generic.List<UnityEngine.Keyframe>();
                foreach (var k in curve.keys)
                {
                    if (k.time < from - 0.0005f) continue;
                    var moved = k;
                    moved.time = UnityEngine.Mathf.Max(0f, k.time - from);
                    keys.Add(moved);
                }
                if (keys.Count == 0 || keys[0].time > 0.0005f) keys.Insert(0, new UnityEngine.Keyframe(0f, curve.Evaluate(from)));
                UnityEditor.AnimationUtility.SetEditorCurve(copy, binding, new UnityEngine.AnimationCurve(keys.ToArray()));
            }
            var settings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.startTime = 0f;
            settings.stopTime = clip.length - from;
            UnityEditor.AnimationUtility.SetAnimationClipSettings(copy, settings);
            var clipPath = outFolder + "/" + clipName + ".anim";
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(clipPath)) UnityEditor.AssetDatabase.DeleteAsset(clipPath);
            UnityEditor.AssetDatabase.CreateAsset(copy, clipPath);
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
            log.AppendLine("   " + clipPath + ": " + clipName + " without its first " + trimFrames + " frames, " + clip.length.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s");
        }
        controller.AddMotion(clip);
    }
    UnityEditor.EditorUtility.SetDirty(controller);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(controller);
    return controller;
}

void Build((string Name, string Material) animal)
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pack + animal.Name + ".prefab");
    if (!source) { log.AppendLine("MISSING " + animal.Name + ": no " + pack + animal.Name + ".prefab"); return; }
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    try
    {
        var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, scene);
        UnityEditor.PrefabUtility.UnpackPrefabInstance(go, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
        go.name = animal.Name;
        go.transform.localPosition = UnityEngine.Vector3.zero;
        go.transform.localRotation = UnityEngine.Quaternion.identity;
        go.transform.localScale = UnityEngine.Vector3.one;
        go.SetActive(true);
        go.tag = "Untagged";
        go.layer = 0;

        // the pack's scripts (missing here) first, then whatever else is not the model
        var removed = 0;
        foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            removed += UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            t.gameObject.layer = 0;
        }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (c) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.Collider>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.Rigidbody>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.AudioSource>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.ParticleSystem>(true)) { UnityEngine.Object.DestroyImmediate(c.gameObject == go ? (UnityEngine.Object)c : c.gameObject); removed++; }
        foreach (var c in go.GetComponentsInChildren<UnityEngine.Light>(true)) { UnityEngine.Object.DestroyImmediate(c); removed++; }

        var wanted = animal.Material != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(animal.Material) : null;
        if (animal.Material != null && !wanted) log.AppendLine("MISSING material " + animal.Material);
        var renderers = new System.Collections.Generic.List<string>();
        foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>(true))
        {
            if (wanted)
            {
                var mats = r.sharedMaterials;
                foreach (var m in mats) log.AppendLine("   " + animal.Name + " had " + (m ? m.name + " [" + (m.shader ? m.shader.name : "no shader") + "]" : "no material"));
                for (var i = 0; i < mats.Length; ++i) mats[i] = wanted;
                r.sharedMaterials = mats;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (r is UnityEngine.SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
            var names = new System.Collections.Generic.List<string>();
            foreach (var m in r.sharedMaterials)
                names.Add(!m ? "NO MATERIAL" : m.name + " [" + (m.shader ? m.shader.name : "no shader") + (m.shader && (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported) ? " BROKEN" : "") + "]");
            renderers.Add(r.name + (r.gameObject.activeInHierarchy && r.enabled ? "" : " (off)") + ": " + string.Join(", ", names));
        }

        var animator = go.GetComponentInChildren<UnityEngine.Animator>(true);
        // The pack's hen plays through an override of the rooster's controller that has no death clip (it plays the
        // attack instead). Its model has one, so the hen gets a controller of its own beside its prefab.
        if (animal.Name == "Hen" && animator)
            animator.runtimeAnimatorController = OwnController("Hen", meshes + "Hen/SKM_Hen_Animations.fbx",
                new[] { "Hen_Idle_2", "Hen_Walk", "Hen_Run", "Hen_Attack", "Hen_Death", "Hen_Sleep", "Hen_Eat" }, null, 0);
        // The pack cut the reindeer's idle one frame early: it starts on the last frame of its death (lying on its
        // side, tongue out) and takes two more to stand. Its controller here plays a copy without those three frames.
        if (animal.Name == "Reindeer" && animator)
            animator.runtimeAnimatorController = OwnController("Reindeer", meshes + "Reindeer/SKM_Reindeer_Animations.fbx",
                new[] { "Reindeer_Idle", "Reindeer_Run", "Reindeer_Attack", "Reindeer_Death" }, "Reindeer_Idle", 3);
        var clips = animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.animationClips : new UnityEngine.AnimationClip[0];
        if (animator) { animator.applyRootMotion = false; animator.cullingMode = UnityEngine.AnimatorCullingMode.CullUpdateTransforms; }
        var clipNames = new System.Collections.Generic.List<string>();
        foreach (var c in clips) if (c && !clipNames.Contains(c.name)) clipNames.Add(c.name);

        var path = outFolder + "/" + animal.Name + ".prefab";
        var saved = UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, path, out var ok);
        log.AppendLine((ok && saved ? "ok " : "FAILED ") + path + " | took off " + removed
            + " | controller " + (animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.name : "NONE")
            + " | " + string.Join("; ", renderers) + " | clips: " + string.Join(", ", clipNames));
    }
    catch (System.Exception e) { log.AppendLine("ERROR " + animal.Name + ": " + e.Message); }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
}

UnityEditor.EditorApplication.CallbackFunction step = null;
step = () =>
{
    if (index < animals.Length)
    {
        Build(animals[index++]);
        System.IO.File.WriteAllText(report, log.ToString());
        return;
    }
    UnityEditor.EditorApplication.update -= step;
    log.AppendLine("done");
    System.IO.File.WriteAllText(report, log.ToString());
};
UnityEditor.EditorApplication.update += step;
return "building " + animals.Length + " prefabs, report in " + report;
