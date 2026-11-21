using MTAssets;
using RavenNest.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using Shinobytes.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public partial class SyntyPlayerAppearance : MonoBehaviour
{
    private readonly Dictionary<ItemType, Transform> equipmentSlots = new Dictionary<ItemType, Transform>();
    private readonly Dictionary<ItemType, GameObject> equippedObjects = new Dictionary<ItemType, GameObject>();
    private readonly Dictionary<ItemType, ItemController> equippedItems = new Dictionary<ItemType, ItemController>();

    public bool useMeshCombiner = true;

    [Header("Editor")]
    [SerializeField] private bool PhotoMode;
    [SerializeField] private ItemManager itemManager;

    [Header("Optimizer")]
    [SerializeField] private SkinnedMeshCombiner meshCombiner;

    /* Genderless Models */
    [Header("Generic Model Objects")]
    [SerializeField] private GameObject[] hats;
    [SerializeField] private GameObject[] masks;
    [SerializeField] private GameObject[] headCoverings;
    [SerializeField] private GameObject[] capes;
    [SerializeField] private GameObject[] hairs;
    [SerializeField] private GameObject[] headAttachments;
    [SerializeField] private GameObject[] ears;
    [SerializeField] private GameObject[] shoulderPadsRight;
    [SerializeField] private GameObject[] shoulderPadsLeft;
    [SerializeField] private GameObject[] elbowsRight;
    [SerializeField] private GameObject[] elbowsLeft;
    //[SerializeField] private GameObject[] hipsAttachments;

    // Two sets of parts the prefab has but none of the lists above names: what is worn on the belt
    // (Chr_HipsAttachment_01..12: pouches, bottles, a scroll, a horn) and on the back (Chr_BackAttachment_01..10:
    // packs, a pelt, cloaks; 11 to 15 are the capes). Found by where they are in the prefab, next to the capes, the
    // first time an item wears one (FindLooseAttachments); never null, so everything that walks the part lists
    // can walk these. An item's model ids name them by number less one, as every attachment: belt 0 is
    // Chr_HipsAttachment_01.
    private GameObject[] beltAttachments = Array.Empty<GameObject>();
    private GameObject[] backAttachments = Array.Empty<GameObject>();
    private bool looseAttachmentsFound;
    [SerializeField] private GameObject[] kneeAttachmentsRight;
    [SerializeField] private GameObject[] kneeAttachmentsLeft;

    /* Male Models */
    [Header("Male Model Objects")]
    [SerializeField] private GameObject[] maleHeads;
    [SerializeField] private GameObject[] maleHelmets;
    [SerializeField] private GameObject[] maleEyebrows;
    [SerializeField] private GameObject[] maleFacialHairs;
    [SerializeField] private GameObject[] maleTorso;
    [SerializeField] private GameObject[] maleArmUpperRight;
    [SerializeField] private GameObject[] maleArmUpperLeft;
    [SerializeField] private GameObject[] maleArmLowerRight;
    [SerializeField] private GameObject[] maleArmLowerLeft;
    [SerializeField] private GameObject[] maleHandsRight;
    [SerializeField] private GameObject[] maleHandsLeft;
    [SerializeField] private GameObject[] maleHips;
    [SerializeField] private GameObject[] maleLegsRight;
    [SerializeField] private GameObject[] maleLegsLeft;

    /* Female Models */
    [Header("Female Model Objects")]
    [SerializeField] private GameObject[] femaleHeads;
    [SerializeField] private GameObject[] femaleHelmets;
    [SerializeField] private GameObject[] femaleEyebrows;
    [SerializeField] private GameObject[] femaleFacialHairs;
    [SerializeField] private GameObject[] femaleTorso;
    [SerializeField] private GameObject[] femaleArmUpperRight;
    [SerializeField] private GameObject[] femaleArmUpperLeft;
    [SerializeField] private GameObject[] femaleArmLowerRight;
    [SerializeField] private GameObject[] femaleArmLowerLeft;
    [SerializeField] private GameObject[] femaleHandsRight;
    [SerializeField] private GameObject[] femaleHandsLeft;
    [SerializeField] private GameObject[] femaleHips;
    [SerializeField] private GameObject[] femaleLegsRight;
    [SerializeField] private GameObject[] femaleLegsLeft;

    //[Header("Item Material Setup")]
    //[SerializeField] private Material[] itemMaterials;

    private readonly List<Material> capeLogoMaterials = new List<Material>();

    [Header("Character Setup")]
    public Gender Gender;
    public int Hair;
    public int Head = 0;
    public int Eyebrows = 0;
    public int FacialHair = 0;

    public int Shoulder = -1;
    public int Elbow = -1;
    public int Kneepad = -1;
    public int Helmet = -1;

    public int Mask = -1;
    public int Hat = -1;
    public int HeadCovering = -1;

    public int Torso = 0;
    public int ArmUpper = 0;
    public int ArmLower = 0;
    public int Hands = 0;
    public int Hips = 0;
    public int Legs = 0;

    public int Cape = -1;

    public int[] HeadAttachments = new int[0];

    public Color SkinColor;
    public Color HairColor;
    public Color BeardColor;
    public Color StubbleColor;
    public Color WarPaintColor;
    public Color ScarColor;
    public Color EyeColor;

    public bool HelmetVisible;

    /// <summary>
    /// Whether the player's cape shows, their own or their clan's (!toggle cape; the server keeps the choice as
    /// SyntyAppearance.CapeHidden). <see cref="Cape"/> still says which cape they have. While it is hidden the
    /// armor's own back piece shows, if the chest has one. Change it with <see cref="SetCapeVisible"/>.
    /// </summary>
    [NonSerialized] public bool CapeVisible = true;

    /// <summary>The player has a cape to show or hide: their own or their clan's.</summary>
    public bool HasCape => PartAt(capes, Cape);

    /// <summary>The armor's own back piece (a cloak, a pack, a pelt) is on.</summary>
    public bool BackPieceShows
    {
        get
        {
            foreach (var part in backAttachments)
            {
                if (part && part.activeSelf) return true;
            }
            return false;
        }
    }

    //public Color EyebrowsColor;
    //public int HelmetAttachment;
    //public int HipsAttachment;

    private GameObject equippedHelmet;
    // what the worn head piece hides while it shows (hair, brows, beard, ears, head), and the attachments on it
    private HeadMask equippedHeadHides;
    private readonly List<GameObject> equippedHeadAttachments = new List<GameObject>();
    // the hair style shown instead of the player's own under a hat it would poke through, or -1
    private int hairUnderHat = -1;
    public GameManager gameManager;
    public PlayerController player;

    public Transform MainHandTransform => equipmentSlots[ItemType.TwoHandedSword];

    public Transform OffHandTransform => equipmentSlots[ItemType.Shield];

    public GameObject FullBodySkinMesh { get; set; }

    /// <summary>
    /// Where this player's parts get their renderers when put on: set when the player was made by
    /// <see cref="PlayerParts.Instantiate"/>, whose players carry their unused parts without renderers. Null for a
    /// player made from the full prefab, whose parts all have theirs.
    /// </summary>
    public PlayerParts.Source PartsSource { get; set; }

    /// <summary>A part's renderer, given back to it first if it was stripped.</summary>
    private SkinnedMeshRenderer RendererOf(GameObject part) => PartsSource != null ? PlayerParts.RendererOf(part, this) : (part ? part.GetComponent<SkinnedMeshRenderer>() : null);

    /// <summary>Every part that is on has its renderer, so the mesh combiner finds it, however it was switched on.</summary>
    private void EnsureActivePartRenderers()
    {
        if (PartsSource == null) return;
        foreach (var field in ModelFields)
        {
            if (field.GetValue(this) is not GameObject[] parts) continue;
            foreach (var part in parts)
            {
                if (part && part.activeSelf) RendererOf(part);
            }
        }
    }

    private Dictionary<string, GameObject[]> modelObjects;
    private readonly static List<FieldInfo> AppearanceFields;
    private readonly static Dictionary<string, FieldInfo> FieldsLookup;
    private readonly static FieldInfo[] Fields;
    private readonly static PropertyInfo[] SyntyProperties;
    private readonly static IReadOnlyList<FieldInfo> ModelFields;

    static SyntyPlayerAppearance()
    {
        Fields = typeof(SyntyPlayerAppearance).GetFields(BindingFlags.Public | BindingFlags.Instance);
        ModelFields = typeof(SyntyPlayerAppearance).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).AsList(x => x.FieldType == typeof(GameObject[]));

        FieldsLookup = Fields.ToDictionary(x => x.Name, x => x);
        SyntyProperties = typeof(SyntyAppearance).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        AppearanceFields = Fields.AsList(x => x.FieldType == typeof(int) || x.FieldType == typeof(int[]));
    }

    public GameObject[] GetHeadAttachments()
    {
        return this.headAttachments;
    }

    void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        if (!itemManager) itemManager = gameManager?.Items ?? FindAnyObjectByType<ItemManager>();
        if (!meshCombiner) meshCombiner = GetComponent<SkinnedMeshCombiner>();
        UpdateBoneTransforms();
    }

    public void Equip(ItemController item)
    {
        if (item.Category == ItemCategory.Armor && string.IsNullOrEmpty(item.GenericPrefabPath))
        {
            EquipArmor(item);
            return;
        }

        Transform targetParent;
        if (item.Type == ItemType.Pet)
        {
            targetParent = transform;
        }
        else
        {
            if (equippedItems.TryGetValue(item.Type, out var currentItem))
            {
                // the item already in the slot is put on again whenever the appearance is rebuilt, and must not go
                if (currentItem != null && currentItem && currentItem != item && currentItem.gameObject)
                    Destroy(currentItem.gameObject);
            }
            else if (equippedObjects.TryGetValue(item.Type, out var currentObject))
            {
                if (currentItem != null && currentItem && currentItem.gameObject)
                    Destroy(currentObject);
            }

            if (equipmentSlots.Count == 0) UpdateBoneTransforms();

            equipmentSlots.TryGetValue(item.Type, out targetParent);

            //if (!equipmentSlots.TryGetValue(item.Type, out targetParent))
            //{
            //    Debug.LogWarning($"Trying to equip an item but target attachment bone could not be found. {item.Type}");
            //}
        }

        if (targetParent != null && targetParent)
        {
            // UGFLY HACK TO FIX AMULET TRANSFORMS
            if (item.Type == ItemType.Amulet && item.gameObject.transform.childCount > 0)
            {
                var amulet = item.gameObject.transform.GetChild(0);
                amulet.localPosition = new Vector3(1.1495f, -0.095946f, 0);
                amulet.localRotation = Quaternion.Euler(83.663f, 314.554f, 44.606f);
                amulet.localScale = new Vector3(0.7563273f, 0.8765048f, 0.8765048f);
            }

            equippedItems[item.Type] = item;
            equippedObjects[item.Type] = item.gameObject;
            item.gameObject.transform.SetParent(targetParent);
        }
    }

    public void Equip(ItemType slot, GameObject item)
    {
        if (equippedObjects.TryGetValue(slot, out var currentObject))
        {
            currentObject.SetActive(false);
        }

        if (equippedItems.TryGetValue(slot, out _))
        {
            equippedItems.Remove(slot);
        }

        if (equipmentSlots.Count == 0) UpdateBoneTransforms();
        item.transform.SetParent(equipmentSlots[slot]);
        equippedObjects[slot] = item;
    }

    public void SetAppearance(SyntyAppearance appearance, Action onReady, bool prepareForCamera, bool updateAppearance = true)
    {
        if (!player)
        {
            player = GetComponent<PlayerController>();
        }

        if (updateAppearance)
        {
            ResetAppearance();
        }

        LoadAppearance(appearance);
        // the player's choice to hide their cape comes with every appearance the server sends (here and not in the
        // generated LoadAppearance, so generating that again does not lose it)
        CapeVisible = !appearance.CapeHidden;
        // the server always says cape 0 for clan members (older clients only have that one ready for a logo);
        // the shape is the clan's or the role's
        Cape = ClanCape(Cape);

        if (updateAppearance)
        {
            UpdateAppearance();

            if (useMeshCombiner)
                Optimize(null, prepareForCamera);
        }

        onReady?.Invoke();
        UpdateClanCape();
    }

    // dressing the player again for another cape shape, so that call does not start another
    private bool redressing;
    // the cape texture last asked for: a late download for an older one is dropped
    private string capeRequest;

    /// <summary>
    /// Puts the clan's cape on: its shape for this member's role, and its own cape image, or the logo on a cloth
    /// when it has none. See docs/clan-images-design.md.
    /// </summary>
    public void UpdateClanCape()
    {
        if (!player || !player.clanHandler || !player.clanHandler.InClan || player.clanHandler.ClanInfo == null || gameManager == null)
            return;

        // a hidden cape is not drawn, so another shape needs no dressing: it is the one that shows when the cape
        // does again. The cape's image is still fetched and set, so it is there then.
        if (!CapeVisible) Cape = ClanCape(Cape);

        // another shape is another mesh, and capes are part of the combined mesh: dressing again, which comes
        // back here with the right one
        if (Cape >= 0 && !redressing)
        {
            var cape = ClanCape(Cape);
            if (cape != Cape)
            {
                redressing = true;
                try { SetAppearance(ToSyntyAppearanceData(), () => player.Inventory.EquipAll(), true); }
                finally { redressing = false; }
                return;
            }
        }

        var clan = player.clanHandler.ClanInfo;
        var look = gameManager.ClanLooks.Get(clan.Id);
        var logos = gameManager.PlayerLogo;

        if (look?.CapeImageId != null && !gameManager.LogoCensor)
        {
            var url = logos.ClanCapeUrl(clan.Id, look.CapeImageId.Value);
            var request = capeRequest = url;
            logos.GetLogo(clan.OwnerUserId, url, image =>
            {
                // a late download (a retry): not for a player who left, or whose cape changed since
                if (!this || !player || !player.clanHandler.InClan || capeRequest != request) return;
                SetCapeLogo(image);
            });
            return;
        }

        // the logo address stays the same when the owner uploads a new one: its id makes it a new address
        var logoUrl = player.clanHandler.Logo;
        if (look?.LogoImageId != null && !string.IsNullOrEmpty(logoUrl))
            logoUrl += (logoUrl.Contains("?") ? "&v=" : "?v=") + look.LogoImageId.Value.ToString("N");

        var logoRequest = capeRequest = "default:" + logoUrl;
        logos.GetLogo(clan.OwnerUserId, logoUrl, logo =>
        {
            if (!this || !player || !player.clanHandler.InClan || capeRequest != logoRequest) return;
            SetCapeTexture(logos.DefaultCape(logo));
        });
    }

    /// <summary>Hides the player's cape, or shows it again (!toggle cape).</summary>
    public void ToggleCapeVisibility()
    {
        SetCapeVisible(!CapeVisible);
    }

    /// <summary>
    /// Shows or hides the player's cape, their own or their clan's, and dresses the player again: capes and back
    /// pieces are part of the combined mesh, and the chest's back piece takes the cape's place while it is hidden.
    /// </summary>
    public void SetCapeVisible(bool visible)
    {
        if (CapeVisible == visible) return;
        CapeVisible = visible;

        // everything off and on again, as when armor comes off: the cape or not (UpdateAppearance), then the worn
        // items, whose chest puts its back piece on or leaves it off (EquipBack), then one new combined mesh
        var equipment = player && player.Equipment ? player.Equipment : GetComponent<PlayerEquipment>();
        if (equipment) equipment.UpdateAppearance();
        else UpdateAppearance();

        // back with the clan's shape and image as they are now
        if (visible) UpdateClanCape();
    }

    /// <summary>The cape mesh for a clan member: the clan's or the role's shape. Anyone else keeps what they have.</summary>
    private int ClanCape(int cape)
    {
        if (cape < 0 || !player || !player.clanHandler || !player.clanHandler.InClan || player.clanHandler.ClanInfo == null || gameManager == null)
            return cape;
        var wanted = gameManager.ClanLooks.CapeFor(player.clanHandler.ClanInfo.Id, player.clanHandler.Role?.Id);
        return wanted >= 0 && wanted < capes.Length ? wanted : cape;
    }

    private Color GetColorFromHex(string value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '#')
        {
            value = "#" + value;
        }

        ColorUtility.TryParseHtmlString(value, out var color);
        return color;
    }

    public bool TryUpdate(int[] values)
    {
        return false;
    }

    public void Unequip(ItemController item)
    {
        UnEquip(item.gameObject);
    }


    //public void ResetAppearance()
    //{
    //    if (modelObjects == null)
    //    {
    //        foreach (var model in GetAll())
    //        {
    //            model?.SetActive(false);
    //        }
    //    }
    //    else
    //    {
    //        // slightly quicker fallback. The foreach still allocates 
    //        foreach (var item in modelObjects)
    //        {
    //            for (var i = 0; i < item.Value.Length; ++i)
    //            {
    //                var model = item.Value[i];
    //                if (model)
    //                {
    //                    model.SetActive(false);
    //                }
    //            }
    //        }
    //    }

    //    if (useMeshCombiner && (meshCombiner?.isMeshesCombineds ?? false))
    //        meshCombiner?.UndoCombineMeshes();
    //}

    // One recombine at a time. Equipping a set asks for one per item, and recombines that overlapped raced the
    // combiner: it merged twice, nested one combined mesh in another and left meshes behind. A recombine asked for
    // while one runs is done once, right after it.
    private bool optimizing;
    private float optimizingSince;
    private bool optimizeAgain;
    private bool optimizeAgainCamera;
    private Action optimizeAgainAfterUndo;

    public void Optimize(Action afterUndo = null, bool ensureObserverCamera = true)
    {
        if (!useMeshCombiner) return;
        // a recombine stuck for long (its coroutine stopped some other way) does not hold the next one up
        if (optimizing && Time.unscaledTime - optimizingSince < 10f)
        {
            optimizeAgain = true;
            optimizeAgainCamera |= ensureObserverCamera;
            if (afterUndo != null) optimizeAgainAfterUndo += afterUndo;
            return;
        }
        optimizing = true;
        optimizingSince = Time.unscaledTime;
        StartCoroutine(OptimizeAppearance(afterUndo, ensureObserverCamera));
    }

    private void OnDisable()
    {
        // coroutines stop with the object
        optimizing = false;
    }

    private IEnumerator OptimizeAppearance(Action afterUndo, bool ensureObserverCamera)
    {
        yield return new WaitForSeconds(0.1f);

        int meshLayer = -1;
        Transform cm = null;
        var replaced = new List<Mesh>();
        if (useMeshCombiner)
        {
            cm = GetCombinedMesh();
            // the combiner only undoes a merge it thinks it has; one left behind otherwise goes here
            if (cm && !meshCombiner.isMeshesCombineds) cm = null;
            // the combiner destroys its object but not the mesh it made, about 1.4 MB a player each time; and any
            // stray combined objects from before go with it
            foreach (var stray in CombinedObjects())
            {
                replaced.AddRange(CombinedMeshesOf(stray));
                if (stray != cm) Destroy(stray.gameObject);
            }
            if (cm)
            {
                meshLayer = meshCombiner.gameObject.layer;
                meshCombiner.UndoCombineMeshes();
            }
        }

        afterUndo?.Invoke();

        yield return new WaitForFixedUpdate();

        if (useMeshCombiner && meshCombiner)
        {
            meshCombiner.meshesToIgnore.Clear();
            var petControllers = gameObject.transform.GetComponentsInChildren<PetController>();
            foreach (var pet in petControllers)
            {
                var renderer = pet.GetComponentInChildren<SkinnedMeshRenderer>();
                if (renderer && renderer.sharedMesh)
                {
                    meshCombiner.meshesToIgnore.Add(renderer);
                }
            }

            yield return new WaitForFixedUpdate();

            EnsureActivePartRenderers();
            meshCombiner.CombineMeshes();
            var waited = 0f;
            while (!meshCombiner.isMeshesCombineds && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            cm = GetCombinedMesh();
            if (cm)
            {
                cm.transform.localPosition = Vector3.zero;
                foreach (var mesh in CombinedMeshesOf(cm)) Slim(mesh);
            }
        }

        foreach (var mesh in replaced) if (mesh) Destroy(mesh);

        if (ensureObserverCamera && gameManager && gameManager.Camera)
        {
            gameManager.Camera.EnsureObserverCamera();
        }

        optimizing = false;
        if (optimizeAgain)
        {
            var camera = optimizeAgainCamera;
            var after = optimizeAgainAfterUndo;
            optimizeAgain = false;
            optimizeAgainCamera = false;
            optimizeAgainAfterUndo = null;
            Optimize(after, camera);
        }
    }

    /// <summary>The combiner's objects under the player: normally one, more if an old merge was left behind.</summary>
    private List<Transform> CombinedObjects()
    {
        var found = new List<Transform>();
        for (var i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            if (child && child.name.IndexOf("combined mesh", StringComparison.OrdinalIgnoreCase) >= 0) found.Add(child);
        }
        return found;
    }

    /// <summary>
    /// Only what the character shader reads: it samples every texture with the first UV set and has no normal map,
    /// so the tangents and the further UV sets the combiner copies from the parts (filled with zeros where a part had
    /// none) go. Then the copy kept for the CPU goes too, since skinning runs on the GPU.
    /// </summary>
    private static void Slim(Mesh mesh)
    {
        if (!mesh || !mesh.isReadable) return;
        mesh.tangents = null;
        mesh.uv2 = null;
        mesh.uv3 = null;
        mesh.uv4 = null;
        mesh.UploadMeshData(true);
    }

    /// <summary>The meshes the combiner made under its object, to free when they are replaced or the player goes.</summary>
    public static List<Mesh> CombinedMeshesOf(Transform combined)
    {
        var meshes = new List<Mesh>();
        if (!combined) return meshes;
        foreach (var renderer in combined.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh && !meshes.Contains(renderer.sharedMesh)) meshes.Add(renderer.sharedMesh);
        }
        return meshes;
    }

    private void OnDestroy()
    {
        // what Unity does not free with the object: the combined mesh, and the material copies the part colours
        // were set on (Renderer.material makes one per renderer)
        foreach (var mesh in CombinedMeshesOf(GetCombinedMesh())) if (mesh) Destroy(mesh);
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material && material.name.EndsWith("(Instance)")) Destroy(material);
            }
        }
    }

    public Transform GetCombinedMesh()
    {
        var cm = transform.Find("Combined Mesh");
        if (!cm)
        {
            for (var ci = 0; ci < transform.childCount; ++ci)
            {
                var c = transform.GetChild(ci);
                if (c && c.name.ToLower().Contains("combined mesh"))
                {
                    cm = c;
                    break;
                }
            }
        }
        return cm;
    }

    public void SetCapeLogo(Sprite capeLogo)
    {
        if (capeLogoMaterials == null)
        {
            return;
        }

        foreach (var capeLogoMaterial in capeLogoMaterials)
        {
            if (capeLogoMaterial == null || !capeLogoMaterial)
            {
                continue;
            }

            capeLogoMaterial.SetTexture("_Texture", capeLogo?.texture);
        }
    }

    /// <summary>A whole cape image (1:2, the back of the cape), such as the default one made from the logo.</summary>
    public void SetCapeTexture(Texture texture)
    {
        foreach (var capeLogoMaterial in capeLogoMaterials)
        {
            if (capeLogoMaterial) capeLogoMaterial.SetTexture("_Texture", texture);
        }
    }

    internal HashSet<string> FromActive()
    {
        var models = GetAllModels();
        var fields = Fields.AsList(x => x.FieldType == typeof(int) || x.FieldType == typeof(int[]));
        var usedMaterials = new HashSet<string>();

        fields.ForEach(field =>
        {
            IEnumerable<KeyValuePair<string, GameObject[]>> items = null;

            if (field.Name == nameof(Hat))
            {
                items = models.Where(x => x.Key.Equals("hats", StringComparison.OrdinalIgnoreCase));
            }
            else if (field.Name == nameof(Mask))
            {
                items = models.Where(x => x.Key.Equals("masks", StringComparison.OrdinalIgnoreCase));
            }
            else if (field.Name == nameof(HeadCovering))
            {
                items = models.Where(x => x.Key.Equals("headcoverings", StringComparison.OrdinalIgnoreCase));
            }
            else if (field.Name == nameof(Head))
            {
                items = models.Where(x => x.Key.StartsWith(Gender.ToString() + field.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                items = models.Where(x =>
                    x.Key.StartsWith(field.Name, StringComparison.OrdinalIgnoreCase) ||
                    x.Key.StartsWith(Gender.ToString() + field.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (field.FieldType == typeof(int[]))
            {
                var value = new List<int>();
                foreach (var item in items)
                {
                    var isActive = item.Value.Where(x => x.activeSelf);
                    foreach (var activeItem in isActive)
                    {
                        value.Add(Array.IndexOf(item.Value, activeItem));

                        var material = activeItem.GetComponent<SkinnedMeshRenderer>()?.material
                            ?? activeItem.GetComponentInChildren<SkinnedMeshRenderer>()?.material
                            ?? activeItem.GetComponent<MeshRenderer>()?.material;

                        if (material)
                        {
                            usedMaterials.Add(material.name.Replace("(Instance)", "").Trim());
                        }
                    }
                }
                field.SetValue(this, value.ToArray());
                //Shinobytes.Debug.LogWarning("Setting Appearance Data from Active objects not supporting arrays yet. (" + field.Name + ")");
            }
            else
            {
                foreach (var item in items)
                {
                    var isActive = item.Value.Where(x => x.activeSelf);
                    var activeItem = isActive.FirstOrDefault();
                    if (activeItem)
                    {
                        var material = activeItem.GetComponent<SkinnedMeshRenderer>()?.material
                            ?? activeItem.GetComponentInChildren<SkinnedMeshRenderer>()?.material
                            ?? activeItem.GetComponent<MeshRenderer>()?.material;

                        if (material)
                        {
                            usedMaterials.Add(material.name.Replace("(Instance)", "").Trim());
                        }

                        field.SetValue(this, Array.IndexOf(item.Value, activeItem));
                    }
                }
            }
        });


        HelmetVisible = Helmet < 0;
        ToggleHelmVisibility();
        return usedMaterials;
    }

    public void UpdateAppearance()
    {
        ResetAppearance();

        // every part is off now: there is no head piece until the items are put on again (one taken off used to
        // come back, and the head go, at the next helmet toggle)
        equippedHelmet = null;
        equippedHeadHides = HeadMask.None;
        equippedHeadAttachments.Clear();
        hairUnderHat = -1;

        // the generated reset does not know the belt and back parts
        SwitchOff(beltAttachments);
        SwitchOff(backAttachments);

        InitAppearance();

        // a hidden cape (!toggle cape): the one InitAppearance put on comes off again. Its material stays in
        // capeLogoMaterials, so the clan's image is on it when the cape shows again.
        if (!CapeVisible) SwitchOff(capes);
    }

    /// <summary>
    /// Fills <see cref="beltAttachments"/> and <see cref="backAttachments"/> from the prefab's own groups: the back
    /// parts are the capes' siblings that are not capes, the belt parts the children of the group named
    /// "..Hips_Attachment" beside them.
    /// </summary>
    private void FindLooseAttachments()
    {
        if (looseAttachmentsFound) return;
        looseAttachmentsFound = true;

        var cape = capes != null && capes.Length > 0 ? capes[0] : null;
        var backGroup = cape ? cape.transform.parent : null;
        if (!backGroup) return;
        backAttachments = NumberedParts(backGroup, "Chr_BackAttachment_", capes);

        var groups = backGroup.parent;
        if (!groups) return;
        for (var i = 0; i < groups.childCount; ++i)
        {
            var group = groups.GetChild(i);
            if (group.name.IndexOf("Hips_Attachment", StringComparison.OrdinalIgnoreCase) < 0) continue;
            beltAttachments = NumberedParts(group, "Chr_HipsAttachment_", null);
            break;
        }
    }

    /// <summary>
    /// The children of a group named prefix + a number, each at its number less one (Chr_HipsAttachment_03 at 2),
    /// without the ones in <paramref name="except"/>: those places stay empty.
    /// </summary>
    private static GameObject[] NumberedParts(Transform group, string prefix, GameObject[] except)
    {
        var found = new List<GameObject>();
        for (var i = 0; i < group.childCount; ++i)
        {
            var child = group.GetChild(i);
            if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (!int.TryParse(child.name.Substring(prefix.Length), out var number) || number < 1 || number > 99) continue;
            if (except != null && Array.IndexOf(except, child.gameObject) >= 0) continue;
            while (found.Count < number) found.Add(null);
            found[number - 1] = child.gameObject;
        }
        return found.ToArray();
    }

    public SyntyAppearance ToSyntyAppearanceData()
    {
        var appearance = new SyntyAppearance();
        // not a field of the same name as the others below: a hidden cape stays hidden when the player is dressed
        // again from this (another clan cape shape), and for whoever reads the appearance (the overlay)
        appearance.CapeHidden = !CapeVisible;
        var props = FieldsLookup;
        foreach (var prop in SyntyProperties)
        {
            if (props.TryGetValue(prop.Name, out var p))
            {
                var valueToSet = p.GetValue(this);// prop.GetValue(appearance);
                try
                {
                    if (p.FieldType == typeof(Color))
                    {
                        prop.SetValue(appearance, ColorUtility.ToHtmlStringRGB((Color)valueToSet));
                        //p.SetValue(this, GetColorFromHex(valueToSet?.ToString()));
                    }
                    else
                    {
                        prop.SetValue(appearance, valueToSet);
                    }

                }
                catch (Exception exc)
                {
                    Shinobytes.Debug.LogError("SyntyPlayerAppearance.ToSyntyAppearanceData: " + exc);
                }
            }
        }

        return appearance;
    }

    public int[] ToAppearanceData()
    {
        throw new NotSupportedException();
    }

    #region Private Members
    private void EquipArmor(ItemController item)
    {
        int itemIndex = Gender == Gender.Male
            ? item.MaleModelID
            : item.FemaleModelID;

        int[] additionalIndex = (Gender == Gender.Male
            ? item.MaleAdditionalIndex
            : item.FemaleAdditionalIndex) ?? Array.Empty<int>();

        if (itemIndex >= 0)
        {
            // a transmogrified item is drawn as its look: the look's parts and material, its kind of head piece
            // (a hat over a helmet), and what the look hides or leaves off
            EquipArmor(item.LookType, itemIndex, (int)item.Material - 1, item.HeadMask, additionalIndex);
        }
    }

    /// <summary>
    /// A model of an armor item on its own (a treasure chest's loot, the icon scene): copies of the parts it is made
    /// of, in its material. <paramref name="mask"/> is the item's HeadMask: the pauldrons or forearms it has none of
    /// are left out, as on a player.
    /// </summary>
    internal GameObject Get(ItemType type, ItemMaterial material, string maleModelId, HeadMask mask = HeadMask.None)
    {
        var items = gameManager ? gameManager.Items : itemManager;
        var itemMaterial = items ? items.GetMaterial((int)material - 1) : null;
        var ids = ParseModelIds(maleModelId);
        if (ids.Length == 0) return null;
        var itemIndex = ids[0];

        var shoulderIndex = 20;
        var upperArmIndex = 13;
        var armLowerIndex = 5;

        var output = new GameObject(type + "_" + material);
        switch (type)
        {

            case ItemType.Boots:
                AddCopy(maleLegsLeft, itemIndex, output.transform);
                AddCopy(maleLegsRight, itemIndex, output.transform);
                break;

            case ItemType.Chest:
                AddCopy(maleTorso, itemIndex, output.transform);

                if (ids.Length > 1) shoulderIndex = ids[1];
                if (ids.Length > 2) upperArmIndex = ids[2];

                if ((mask & HeadMask.NoShoulderPads) == 0)
                {
                    AddCopy(shoulderPadsLeft, shoulderIndex, output.transform);
                    AddCopy(shoulderPadsRight, shoulderIndex, output.transform);
                }

                AddCopy(maleArmUpperLeft, upperArmIndex, output.transform);
                AddCopy(maleArmUpperRight, upperArmIndex, output.transform);

                // the elbow pads the item names (not the default ones a player gets with a plain plate chest)
                if (ids.Length > 3 && (mask & HeadMask.NoElbowPads) == 0)
                {
                    AddCopy(elbowsLeft, ids[3], output.transform);
                    AddCopy(elbowsRight, ids[3], output.transform);
                }

                // what it wears on the back
                if (ids.Length > 4 && ids[4] >= 0)
                {
                    FindLooseAttachments();
                    AddCopy(backAttachments, ids[4], output.transform);
                }
                break;

            case ItemType.Gloves:
                AddCopy(maleHandsLeft, itemIndex, output.transform);
                AddCopy(maleHandsRight, itemIndex, output.transform);

                if (ids.Length > 1)
                {
                    armLowerIndex = ids[1];
                }

                if ((mask & HeadMask.NoForearms) == 0)
                {
                    AddCopy(maleArmLowerLeft, armLowerIndex, output.transform);
                    AddCopy(maleArmLowerRight, armLowerIndex, output.transform);
                }
                break;
            case ItemType.LeftShoulder:
            case ItemType.RightShoulder:
                AddCopy(shoulderPadsLeft, itemIndex, output.transform);
                AddCopy(shoulderPadsRight, itemIndex, output.transform);
                break;

            case ItemType.Hat:
            case ItemType.Mask:
            case ItemType.Helmet:
            case ItemType.HeadCovering:

                var isHelmet = type == ItemType.Helmet;
                var isMask = type == ItemType.Mask;
                var isHat = type == ItemType.Hat;

                var itemObjs = isHelmet ? maleHelmets : isMask ? masks : isHat ? hats : headCoverings;

                AddCopy(itemObjs, itemIndex, output.transform);

                for (var i = 1; i < ids.Length; ++i)
                {
                    AddCopy(headAttachments, ids[i], output.transform);
                }
                break;
            case ItemType.Leggings:
                AddCopy(maleHips, itemIndex, output.transform);

                if (ids.Length > 1 && (mask & HeadMask.NoKneePads) == 0)
                {
                    AddCopy(kneeAttachmentsLeft, ids[1], output.transform);
                    AddCopy(kneeAttachmentsRight, ids[1], output.transform);
                }

                // what hangs on its belt
                if (ids.Length > 2)
                {
                    FindLooseAttachments();
                    for (var i = 2; i < ids.Length; ++i)
                    {
                        AddCopy(beltAttachments, ids[i], output.transform);
                    }
                }
                break;
        }

        if (itemMaterial)
        {
            foreach (var renderer in output.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterial = itemMaterial;
            }
        }
        return output;
    }

    /// <summary>
    /// The indices of a model id ("27,20,20,0"): the main part first. A token that is not a number is -1, which no
    /// part has, so it is left out and the others keep their places. Empty when the first one is not a number.
    /// </summary>
    private static int[] ParseModelIds(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return Array.Empty<int>();
        var tokens = modelId.Split(',');
        var ids = new int[tokens.Length];
        for (var i = 0; i < tokens.Length; ++i)
        {
            if (!int.TryParse(tokens[i], out ids[i]))
            {
                if (i == 0) return Array.Empty<int>();
                ids[i] = -1;
            }
        }
        return ids;
    }

    /// <summary>The part at an index, or null when the set has no such part (an item made for a newer model set).</summary>
    private static GameObject PartAt(GameObject[] parts, int index)
    {
        return parts != null && index >= 0 && index < parts.Length ? parts[index] : null;
    }

    private void AddCopy(GameObject[] parts, int index, Transform parent)
    {
        var part = PartAt(parts, index);
        if (part) ActivateAndInstantiate(part, parent);
    }

    private void ActivateAndInstantiate(GameObject obj, Transform parent)
    {
        obj.SetActive(true);
        RendererOf(obj);
        Instantiate(obj, parent);
    }

    private void EquipArmor(ItemType itemType, int itemIndex, int material, HeadMask mask, params int[] additionalIndices)
    {

        var newMaterial = itemManager.GetMaterial(material);
        switch (itemType)
        {
            case ItemType.Boots:
                EquipPairItems(
                    maleLegsLeft,
                    maleLegsRight,
                    femaleLegsLeft,
                    femaleLegsRight,
                    itemIndex, newMaterial);
                break;

            case ItemType.Chest:
                EquipItems(maleTorso, femaleTorso, itemIndex, newMaterial);

                // equip or unequip upper arms
                if (itemIndex > 0)
                {
                    // 0: shoulder. A robe or a jerkin has no pauldrons (NoShoulderPads): none are put on, and the
                    // ones of the armor worn before go
                    if ((mask & HeadMask.NoShoulderPads) != 0)
                    {
                        SwitchOff(shoulderPadsLeft);
                        SwitchOff(shoulderPadsRight);
                    }
                    else
                    {
                        var shoulderIndex = Gender == Gender.Male ? 20 : 14;
                        if (additionalIndices.Length > 0)
                        {
                            shoulderIndex = additionalIndices[0];
                        }

                        EquipAttachments(shoulderPadsLeft, shoulderPadsRight, shoulderIndex, newMaterial);
                    }

                    // upper arm
                    var upperArmIndex = Gender == Gender.Male ? 13 : 12;
                    if (additionalIndices.Length > 1)
                    {
                        upperArmIndex = additionalIndices[1];
                    }

                    EquipPairItems(
                        maleArmUpperLeft, maleArmUpperRight,
                        femaleArmUpperLeft, femaleArmUpperRight,
                        upperArmIndex, newMaterial);

                    // elbows, or none (NoElbowPads)
                    if ((mask & HeadMask.NoElbowPads) != 0)
                    {
                        SwitchOff(elbowsLeft);
                        SwitchOff(elbowsRight);
                    }
                    else
                    {
                        var elbowIndex = 0;
                        if (additionalIndices.Length > 2)
                        {
                            elbowIndex = additionalIndices[2];
                        }
                        EquipAttachments(elbowsLeft, elbowsRight, elbowIndex, newMaterial);
                    }

                    // 3: what is worn on the back (a pack, a pelt), or nothing
                    EquipBack(additionalIndices.Length > 3 ? additionalIndices[3] : -1, newMaterial);
                }
                else
                {
                    EquipBack(-1, newMaterial);
                    EquipPairItems(
                        maleArmUpperLeft, maleArmUpperRight,
                        femaleArmUpperLeft, femaleArmUpperRight, 0, newMaterial);
                }
                break;

            case ItemType.Gloves:
                EquipPairItems(
                    maleHandsLeft,
                    maleHandsRight,
                    femaleHandsLeft,
                    femaleHandsRight,
                    itemIndex, newMaterial);

                if (itemIndex > 0 && (mask & HeadMask.NoForearms) != 0)
                {
                    // gloves that end at the wrist: the body's own forearms, in the skin they have
                    EquipPairItems(
                        maleArmLowerLeft, maleArmLowerRight,
                        femaleArmLowerLeft, femaleArmLowerRight,
                        0, null);
                }
                else if (itemIndex > 0)
                {
                    var lowerArm = Gender == Gender.Male ? 5 : 15;
                    if (additionalIndices.Length > 0)
                    {
                        lowerArm = additionalIndices[0];
                    }

                    EquipPairItems(
                        maleArmLowerLeft, maleArmLowerRight,
                        femaleArmLowerLeft, femaleArmLowerRight,
                           lowerArm, newMaterial);
                }
                else
                {
                    EquipPairItems(
                        maleArmLowerLeft, maleArmLowerRight,
                        femaleArmLowerLeft, femaleArmLowerRight,
                        0, newMaterial);
                }

                break;
            case ItemType.LeftShoulder:
            case ItemType.RightShoulder:
                EquipPairItems(shoulderPadsLeft, shoulderPadsRight, itemIndex, newMaterial);
                break;

            case ItemType.Hat:
            case ItemType.Mask:
            case ItemType.Helmet:
            case ItemType.HeadCovering:
                var isHelmet = itemType == ItemType.Helmet;
                var isMask = itemType == ItemType.Mask;
                var isHat = itemType == ItemType.Hat;

                // one head piece at a time, whichever set of parts it is from: the one worn before goes, with its
                // attachments
                SwitchOff(maleHelmets);
                SwitchOff(femaleHelmets);
                SwitchOff(hats);
                SwitchOff(masks);
                SwitchOff(headCoverings);
                SwitchOff(headAttachments);
                equippedHeadAttachments.Clear();

                equippedHelmet = EquipItems(
                    isHelmet ? maleHelmets : isMask ? masks : isHat ? hats : headCoverings,
                    isHelmet ? femaleHelmets : isMask ? masks : isHat ? hats : headCoverings,
                    itemIndex, newMaterial);
                equippedHeadHides = HeadHides(itemType, mask);

                if (!equippedHelmet)
                {
                    // a head piece this model set does not have: the head as it is without one
                    ShowHead(HeadMask.None);
                    break;
                }

                foreach (var headAttachment in additionalIndices)
                {
                    var attachment = PartAt(headAttachments, headAttachment);
                    if (!attachment) continue;
                    Equip(attachment, newMaterial, false);
                    equippedHeadAttachments.Add(attachment);
                }
                UpdateHelmetVisibility();
                break;
            case ItemType.Leggings:
                EquipItems(maleHips, femaleHips, itemIndex, newMaterial);
                // knee pads: the item's own, or none (NoKneePads, or leggings without any: the ones of the leggings
                // worn before go)
                if (additionalIndices.Length > 0 && (mask & HeadMask.NoKneePads) == 0)
                {
                    EquipAttachments(kneeAttachmentsLeft, kneeAttachmentsRight, additionalIndices[0], newMaterial);
                }
                else
                {
                    SwitchOff(kneeAttachmentsLeft);
                    SwitchOff(kneeAttachmentsRight);
                }

                // 1 and on: what hangs on the belt (a pouch, bottles, a scroll), or nothing
                EquipBelt(additionalIndices, 1, newMaterial);
                break;
        }
    }

    /// <summary>
    /// The belt of a pair of leggings: the parts its model ids name from <paramref name="first"/> on, in its
    /// material, and none of the leggings worn before. Model ids: hips, knee pads, then these. A game before 0.9.7
    /// reads the first two only, so the knee position always holds a real knee pad index (with NoKneePads when the
    /// leggings have none).
    /// </summary>
    private void EquipBelt(int[] ids, int first, Material material)
    {
        if (ids.Length <= first && !looseAttachmentsFound) return; // nothing to put on, nothing was ever on
        FindLooseAttachments();
        SwitchOff(beltAttachments);
        for (var i = first; i < ids.Length; ++i)
        {
            Equip(PartAt(beltAttachments, ids[i]), material, false);
        }
    }

    /// <summary>
    /// What a chest piece wears on the back, in its material, and nothing of the chest worn before. Model ids:
    /// torso, pauldrons, upper arms, elbow pads, then this (-1 or absent for nothing; no game before 0.9.7 reads it).
    /// </summary>
    private void EquipBack(int index, Material material)
    {
        if (index < 0 && !looseAttachmentsFound) return;
        FindLooseAttachments();
        SwitchOff(backAttachments);
        if (BackPieceFits()) Equip(PartAt(backAttachments, index), material, false);
    }

    /// <summary>
    /// The back is the cape's place first: a player whose cape shows (their own or their clan's) keeps it and the
    /// armor's back piece stays off. Both hang from the same bone and cut through each other. A player without a
    /// cape, or who has hidden theirs (!toggle cape), wears the back piece.
    /// </summary>
    private bool BackPieceFits()
    {
        return !CapeVisible || !HasCape;
    }

    /// <summary>
    /// What a head piece hides while it shows. A helmet is a whole head of its own (Chr_Head_No_Elements) and every
    /// helmet made before the mask was read says None: those hide everything, as they always did. Hats, masks and
    /// head coverings sit on the player's head and hide what their mask says (a hat nothing, a hood the hair).
    /// </summary>
    private static HeadMask HeadHides(ItemType itemType, HeadMask mask)
    {
        var hides = mask & HeadMask.All;
        return itemType == ItemType.Helmet && hides == HeadMask.None ? HeadMask.All : hides;
    }

    private static void SwitchOff(GameObject[] parts)
    {
        if (parts == null) return;
        for (var i = 0; i < parts.Length; ++i)
        {
            if (parts[i]) parts[i].SetActive(false);
        }
    }

    private GameObject EquipItems(
        GameObject[] male,
        GameObject[] female,
        int newItemIndex,
        Material material)
    {
        return Equip(PartAt(Gender == Gender.Male ? male : female, newItemIndex), material);
    }

    private void EquipPairItems(
        GameObject[] genericLeft,
        GameObject[] genericRight,
        int newItemIndex,
        Material material)
    {
        EquipPair(PartAt(genericLeft, newItemIndex), PartAt(genericRight, newItemIndex), material);
    }

    /// <summary>
    /// A pair of attachments (pauldrons, elbow pads, knee pads) at an index, or none on a side whose set has no such
    /// part: the ones of the armor worn before do not stay.
    /// </summary>
    private void EquipAttachments(GameObject[] left, GameObject[] right, int index, Material material)
    {
        if (!Equip(PartAt(left, index), material)) SwitchOff(left);
        if (!Equip(PartAt(right, index), material)) SwitchOff(right);
    }

    private void EquipPairItems(
        GameObject[] maleLeft,
        GameObject[] maleRight,
        GameObject[] femaleLeft,
        GameObject[] femaleRight,
        int newItemIndex,
        Material material)
    {
        var leftNew = PartAt(Gender == Gender.Male ? maleLeft : femaleLeft, newItemIndex);
        var rightNew = PartAt(Gender == Gender.Male ? maleRight : femaleRight, newItemIndex);

        EquipPair(leftNew, rightNew, material);
    }

    private void EquipPair(GameObject left, GameObject right, Material material)
    {
        Equip(left, material);
        Equip(right, material);
    }

    private void UnEquip(GameObject item)
    {
        item.SetActive(false);
    }
    private GameObject Equip(GameObject item, Material material, bool deactivateOthers = true)
    {
        // no such part (an index this model set does not have): nothing changes
        if (!item) return null;

        if (item.transform.parent && deactivateOthers)
        {
            for (var i = 0; i < item.transform.parent.childCount; ++i)
            {
                item.transform.parent.GetChild(i).gameObject.SetActive(false);
            }
        }

        item.SetActive(true);
        var renderer = RendererOf(item);
        if (material && renderer)
        {
            renderer.sharedMaterial = material;
        }

        return item;
    }


    private IReadOnlyDictionary<string, GameObject[]> GetAllModels()
    {
        if (modelObjects == null)
        {
            modelObjects = ModelFields.ToDictionary(x => x.Name, x => x.GetValue(this) as GameObject[]);
        }

        return modelObjects;
    }

    public void RandomizeCharacter()
    {
        Gender = Enums.GetValues<Gender>().Random();
        EyeColor = RandomColor();
        HairColor = RandomColor();
        BeardColor = RandomColor();
        SkinColor = RandomColor();
    }
    private void UpdateBoneTransforms()
    {
        //this.equipmentSlots[ItemType.] = this.transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Back_Attachment");

        var mainHand = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R");
        var offHand = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_L/Shoulder_L/Elbow_L/Hand_L");
        var shoulderLeft = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_L/Shoulder_L");
        var shoulderRight = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R");

        var neck = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Neck");
        equipmentSlots[ItemType.Amulet] = neck;
        equipmentSlots[ItemType.Helmet] = neck.Find("Head/Head_Attachment");
        equipmentSlots[ItemType.Mask] = equipmentSlots[ItemType.Helmet];
        equipmentSlots[ItemType.Hat] = equipmentSlots[ItemType.Helmet];
        equipmentSlots[ItemType.HeadCovering] = equipmentSlots[ItemType.Helmet];

        equipmentSlots[ItemType.TwoHandedSword] = mainHand;
        equipmentSlots[ItemType.TwoHandedStaff] = mainHand;
        equipmentSlots[ItemType.TwoHandedBow] = offHand;
        equipmentSlots[ItemType.TwoHandedAxe] = mainHand;
        equipmentSlots[ItemType.OneHandedAxe] = mainHand;
        equipmentSlots[ItemType.TwoHandedSpear] = mainHand;
        equipmentSlots[ItemType.OneHandedSword] = mainHand;
        equipmentSlots[ItemType.Shield] = offHand;

        equipmentSlots[ItemType.LeftShoulder] = shoulderLeft;
        equipmentSlots[ItemType.RightShoulder] = shoulderRight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color RandomColor()
    {
        return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }

    public void ToggleHelmVisibility()
    {
        HelmetVisible = !HelmetVisible;

        if (useMeshCombiner)
            Optimize(UpdateHelmetVisibility);
        else
            UpdateHelmetVisibility();
    }

    public void UpdateHelmetVisibility()
    {
        try
        {
            if (!equippedHelmet && Helmet < 0)
            {
                return;
            }

            // A head piece that shows hides what it says: a helmet everything (it is a head of its own), a hat
            // nothing, a hood the hair. One the player has switched off hides nothing. Without an item, the helmet
            // of the appearance itself (the item creator's) is a whole head too.
            var hides = !HelmetVisible ? HeadMask.None : equippedHelmet ? equippedHeadHides : HeadMask.All;
            ShowHead(hides, HelmetVisible ? equippedHelmet : null);

            if (equippedHelmet)
            {
                equippedHelmet.SetActive(HelmetVisible);
            }

            // the plumes and wings on it go with it
            foreach (var attachment in equippedHeadAttachments)
            {
                if (attachment) attachment.SetActive(HelmetVisible);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Failed to toggle helmet visibility for " + (player ? player.Name : name) + ": " + exc);
        }
    }

    /// <summary>
    /// Shows the player's own head, brows, beard and hair, less what <paramref name="hides"/> names. Under
    /// <paramref name="hat"/> (the head piece that shows, if any) a hair style that would come through it is
    /// swapped for a flat one.
    /// </summary>
    private void ShowHead(HeadMask hides, GameObject hat = null)
    {
        var male = Gender == Gender.Male;
        ShowPart(male ? maleHeads : femaleHeads, Head, (hides & HeadMask.Head) == 0);
        ShowPart(male ? maleEyebrows : femaleEyebrows, Eyebrows, (hides & HeadMask.Brows) == 0);
        ShowPart(male ? maleFacialHairs : femaleFacialHairs, FacialHair, (hides & HeadMask.Beard) == 0);

        var showHair = (hides & HeadMask.Hair) == 0;
        var under = showHair ? HairUnder(hat, Hair) : Hair;
        if (under != Hair && !RendererOf(PartAt(hairs, Hair))) under = Hair; // no hair of their own: none under a hat
        if (hairUnderHat >= 0 && hairUnderHat != under) ShowPart(hairs, hairUnderHat, false);
        hairUnderHat = under != Hair ? under : -1;
        ShowPart(hairs, Hair, showHair && under == Hair);
        if (under != Hair)
        {
            // the stand-in is worn in the color of the player's own hair (on a material of its own, as every
            // hair has: sharing the other's would leave one behind when the player changes hair style)
            var standIn = RendererOf(PartAt(hairs, under));
            var own = RendererOf(PartAt(hairs, Hair));
            if (standIn && own.sharedMaterial) standIn.material.SetColor("_Color_Hair", own.sharedMaterial.GetColor("_Color_Hair"));
            ShowPart(hairs, under, true);
        }

        // elf ears are not part of an appearance yet; if they ever are, a hood covers them
        if ((hides & HeadMask.Ears) != 0) SwitchOff(ears);
    }

    private void ShowPart(GameObject[] parts, int index, bool show)
    {
        var part = PartAt(parts, index);
        if (!part) return;
        if (show) RendererOf(part);
        part.SetActive(show);
    }

    // Hair under a hat. A hat (Chr_HeadCoverings_Base_Hair) sits on the head and hides nothing, and many of the 38
    // hair styles come through it: top knots, quiffs, mohawks, big hair. Every style was measured under the hats
    // that items use (tools/items/class-gear-hair-under-hats.py: hair seen above the hat's lower edge, from 16
    // sides; listed when it covers 1% of the hat or more in the worst view). A listed style is swapped, only while
    // the hat shows, for a flat one of about its length that fits under every hat: the short hair 2, or the
    // neck-length hair 19 for the styles that reach below the jaw. A hat that has not been measured swaps nothing.
    private const int FlatShortHair = 2;
    private const int FlatLongHair = 19;
    private static readonly HashSet<int> LongHair = new HashSet<int> { 1, 19, 20, 22, 23, 24, 25, 27, 28, 29, 30, 31, 33, 34, 35, 37 };
    private static readonly Dictionary<int, HashSet<int>> HairThroughHat = new Dictionary<int, HashSet<int>>
    {
        // hats[1], the wizard hat
        { 1, new HashSet<int> { 0, 3, 4, 5, 6, 8, 12, 13, 15, 18, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 37 } },
        // hats[4], the feathered cap
        { 4, new HashSet<int> { 0, 3, 4, 5, 8, 12, 13, 15, 18, 22, 23, 24, 25, 26, 27, 28, 29, 31, 32, 37 } },
    };

    /// <summary>The hair style to show under a head piece: the player's own, or the flat one that fits under that hat.</summary>
    private int HairUnder(GameObject headPiece, int hair)
    {
        if (!headPiece) return hair;
        var hat = Array.IndexOf(hats, headPiece);
        if (hat < 0 || !HairThroughHat.TryGetValue(hat, out var through) || !through.Contains(hair)) return hair;
        var flat = LongHair.Contains(hair) ? FlatLongHair : FlatShortHair;
        return PartAt(hairs, flat) ? flat : hair;
    }

    public void SetFullBodySkinMesh(GameObject prefab)
    {
        var combinedMesh = GetCombinedMesh();
        FullBodySkinMesh = Instantiate(prefab, this.transform);
        FullBodySkinMesh.name = "FullBodySkin";
        FullBodySkinMesh.transform.localScale = Vector3.one;

        if (combinedMesh)
        {
            SetLayerRecursive(FullBodySkinMesh, combinedMesh.gameObject.layer);
        }
    }

    public void DestroyFullBodySkinMesh()
    {
        var combinedMesh = GetCombinedMesh();
        if (combinedMesh)
            combinedMesh.gameObject.SetActive(true);

        if (!FullBodySkinMesh)
            return;

        Destroy(FullBodySkinMesh);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        for (var i = 0; i < go.transform.childCount; ++i)
        {
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
    #endregion
}
