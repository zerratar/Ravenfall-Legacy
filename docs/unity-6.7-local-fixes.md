# Unity 6.7 upgrade - fixes that live outside version control

Ravenfall Legacy targets **Unity 6000.7.0a4**. Several fixes required by that upgrade land in
files that `.gitignore` excludes (third-party paid assets, generated collider data, and the URP
pipeline assets). They are **not in the repository** and must be reapplied by hand on a fresh
checkout, or the project will not compile.

This document records each one so it is reproducible without redistributing licensed source.

> If the editor reports compile errors on a fresh clone, work through section 1 first. Because
> `DamageNumbersPro` has its own assembly definition, its errors block every downstream assembly -
> so a handful of errors there hides everything else in `Assembly-CSharp`.

---

## 1. Third-party asset patches (required to compile)

Unity 6.7 makes `UnityEngine.Object.GetInstanceID()` obsolete **as an error** (CS0619) and replaces
it with `GetEntityId()`, returning the new `UnityEngine.EntityId` struct. The implicit
`int` ⇄ `EntityId` conversion is also obsolete, so anything *storing* an id must change type too.

These packages are licensed Asset Store products, so only the changed lines are recorded.

### DamageNumbersPro

`Assets/DamageNumbersPro/Scripts/Internal/DamageNumber.cs`

Unity's automatic API updater half-migrated this file: it changed the pool dictionary's *field*
to `EntityId` but left every user of it as `int`. Finish the migration:

| Location | Before | After |
|---|---|---|
| field `poolingID` | `int poolingID;` | `EntityId poolingID;` |
| `Spawn()` local | `int instanceID = GetInstanceID();` | `EntityId instanceID = GetEntityId();` |
| `DestroyDNP()` | `new Dictionary<int, HashSet<DamageNumber>>()` | `new Dictionary<EntityId, HashSet<DamageNumber>>()` |
| `SetPoolingID()` | `new Dictionary<int, HashSet<DamageNumber>>()` | `new Dictionary<EntityId, HashSet<DamageNumber>>()` |
| `PoolAvailable()` signature | `bool PoolAvailable(int id)` | `bool PoolAvailable(EntityId id)` |
| `SetPoolingID()` signature | `void SetPoolingID(int id)` | `void SetPoolingID(EntityId id)` |
| `SetFollowedTarget()` | `followedTransform.GetInstanceID()` | `followedTransform.GetEntityId()` |

`Assets/DamageNumbersPro/Scripts/DamageNumberGUI.cs` - in `OnStart()`:
`transform.parent.GetInstanceID()` → `transform.parent.GetEntityId()`

`Assets/DamageNumbersPro/Demo/Scripts/DNP_Camera.cs`:
`enemy.GetInstanceID()` → `enemy.GetEntityId()`

`Assets/DamageNumbersPro/Scripts/Internal/Editor/DNPEditorInternal.cs` - two occurrences
(`NewTextMeshPro` and `NewTextGUI`). TextMeshPro's `enableWordWrapping` is obsolete-as-error:
`tmp.enableWordWrapping = false;` → `tmp.textWrappingMode = TextWrappingModes.NoWrap;`

### MT Assets - Skinned Mesh Combiner

`Assets/MT Assets/Skinned Mesh Combiner/Scripts/SkinnedMeshCombiner.cs` - two occurrences, both
`texturesToMerge[i].GetInstanceID().ToString()` → `texturesToMerge[i].GetEntityId().ToString()`

### Fantasy Adventure Environment

`Assets/Fantasy Adventure Environment/Scripts/SubstanceBaker.cs` - the `int` overload of
`GetAssetPath` is gone; pass the object instead:
`AssetDatabase.GetAssetPath(substance.GetInstanceID())` → `AssetDatabase.GetAssetPath(substance)`

### PolyFew

`Assets/PolyFew/Scripts/Editor/PolyfewMenu.cs` - same change:
`AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID())` →
`AssetDatabase.GetAssetPath(Selection.activeObject)`

---

## 2. URP pipeline assets - disable Dynamic Batching

Excluded by the blanket `*.asset` rule in `.gitignore` (line 133), so this setting is per-machine.

Unity 6.7 removed Dynamic Batching. While it is still enabled, the renderer logs an error **every
frame**:

```
Dynamic Batching has been removed and no longer has any effect.
Use SRP Batcher or GPU Instancing instead.
```

Turn it off in `URP_HighQuality.asset`, `URP_MediumQuality.asset` and `URP_LowQuality.asset`
(Inspector → Advanced → Dynamic Batching). Note the C# property `supportsDynamicBatching` is
itself obsolete-as-error, so scripted fixes must go through `SerializedObject`:

```csharp
var so   = new SerializedObject(urpAsset);
var prop = so.FindProperty("m_SupportsDynamicBatching");
prop.boolValue = false;
so.ApplyModifiedProperties();
```

---

## 3. Generated convex collider assets

`Assets/Polygon*/**/*_Convex*.asset` - excluded by `.gitignore` (`Assets/Polygon*/*`).

**425 of ~5,154** of these files were malformed: a `MonoBehaviour` block was missing its YAML
document header, so the file began (or continued, mid-file) with bare fields:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
  m_PrefabInstance: {fileID: 0}      # <- no "--- !u!114 &11400000" / "MonoBehaviour:" above
```

Unity 6.7's parser rejects this, producing hundreds of console errors:

- `Failed to parse data as no preceding class type header ... at line 8` - leading case
- `Invalid Script reference on non-host type 'Mesh' ... at line 172` - mid-file case, where the
  orphaned `m_Script` gets attributed to the preceding `Mesh` document

**Fix:** insert the two missing header lines immediately before the headerless block:

```yaml
--- !u!114 &11400000
MonoBehaviour:
```

This restores the same shape as the ~2,259 files that were already well-formed. Do **not** delete
the block instead - that changes the asset's main object and produces
`Main Object Name ... does not match filename` warnings.

Mesh `fileID`s must be preserved; prefabs reference the collision meshes directly, e.g.
`m_Mesh: {fileID: 3509967525720898572, guid: ..., type: 2}`. After repair, verify with:

```csharp
// expect resolved == total, null == 0
foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true))
    if (mc.sharedMesh == null) Debug.LogError(path);
```

The MonoBehaviour's `m_Script` GUID (`5b71ad40e238046238f9b0c6f33c3791`) refers to a collider
generator that is no longer in the project. That is pre-existing and harmless - the meshes are
referenced directly by `fileID`, not through the script.

---

## 4. Known unresolved - Odin Inspector

`Assets/Plugins/Sirenix` (excluded by `Assets/Plugins/*`). The installed build dates from
mid-2024 and is **not compatible with Unity 6.7**. At editor startup it throws:

```
Exception while executing InitializeOnLoad for GUITimeHelper.Init
MissingFieldException: Field not found:
  System.Action`1<UnityEngine.UIElements.IMGUIContainer>
  UnityEngine.UIElements.UIElementsUtility.s_BeginContainerCallback

Exception while executing InitializeOnLoad for SdfIcons.FixBug
NotImplementedException: The method or operation is not implemented.
```

Both fire on **every domain reload**, not only at editor startup, so they reappear after each
recompile. Odin reflects into Unity internals that 6.7 changed or removed. It ships as
precompiled DLLs, so this cannot be patched locally - it needs an updated build from Sirenix.

**Impact is limited to the editor.** The project uses Odin only for inspector decoration
(`[Button]` ×39, `[TabGroup]` ×17, `[ReadOnly]` ×9 across 28 files) and **no**
`SerializedMonoBehaviour` / `SerializedScriptableObject`, so there is no Odin runtime
serialization dependency. Builds and runtime data are unaffected; only inspector convenience
degrades.

---

## Verifying a fresh setup

```bash
# compile headlessly; expect exit 0 and no "error CS" lines
Unity.exe -batchmode -quit -nographics -projectPath . -logFile compile.log
```

With the Unity CLI and the `com.unity.pipeline` package, a live editor can be checked directly:

```bash
unity pipeline list     # "Safe Mode" column must be empty - it is set when compilation fails
unity status            # state must be "ready"
unity command console --level error --tail 50
```
