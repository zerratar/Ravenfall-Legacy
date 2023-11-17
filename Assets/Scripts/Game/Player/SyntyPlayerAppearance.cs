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

    //public Color EyebrowsColor;
    //public int HelmetAttachment;
    //public int HipsAttachment;

    private GameObject equippedHelmet;
    public GameManager gameManager;
    public PlayerController player;

    public Transform MainHandTransform => equipmentSlots[ItemType.TwoHandedSword];

    public Transform OffHandTransform => equipmentSlots[ItemType.Shield];

    public GameObject FullBodySkinMesh { get; set; }

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
                if (currentItem != null && currentItem && currentItem.gameObject)
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

        if (updateAppearance)
        {
            UpdateAppearance();

            if (useMeshCombiner)
                Optimize(null, prepareForCamera);
        }

        onReady?.Invoke();
        UpdateClanCape();
    }

    public void UpdateClanCape()
    {
        if (this.player.Clan.InClan)
        {
            gameManager.PlayerLogo.GetLogo(
                this.player.Clan.ClanInfo.OwnerUserId,
                this.player.Clan.Logo, logo =>
                {
                    SetCapeLogo(logo);
                });
        }
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

    public void Optimize(Action afterUndo = null, bool ensureObserverCamera = true)
    {
        if (useMeshCombiner)
            StartCoroutine(OptimizeAppearance(afterUndo, ensureObserverCamera));
    }

    private IEnumerator OptimizeAppearance(Action afterUndo, bool ensureObserverCamera)
    {
        yield return new WaitForSeconds(0.1f);

        int meshLayer = -1;
        Transform cm = null;
        if (useMeshCombiner)
        {
            cm = GetCombinedMesh();
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

            meshCombiner.CombineMeshes();
            while (!meshCombiner.isMeshesCombineds)
            {
                yield return null;
            }
            cm = GetCombinedMesh();
            if (cm)
            {
                cm.transform.localPosition = Vector3.zero;
            }
        }

        if (ensureObserverCamera && gameManager && gameManager.Camera)
        {
            gameManager.Camera.EnsureObserverCamera();
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

        InitAppearance();
    }

    public SyntyAppearance ToSyntyAppearanceData()
    {
        var appearance = new SyntyAppearance();
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

        int[] additionalIndex = Gender == Gender.Male
            ? item.MaleAdditionalIndex
            : item.FemaleAdditionalIndex;

        if (itemIndex >= 0)
        {
            EquipArmor(item.Type, itemIndex, (int)item.Material - 1, additionalIndex);
        }
    }

    internal GameObject Get(ItemType type, ItemMaterial material, string maleModelId)
    {
        var itemMaterial = (gameManager ? gameManager.Items : itemManager).GetMaterial((int)material - 1);
        var itemIndex = -1;
        var ids = new int[0];
        //var headCovering_no_hair = false; // 
        if (!string.IsNullOrEmpty(maleModelId))
        {
            //if (maleModelId.ToLower().StartsWith("c"))
            //{
            //    maleModelId = maleModelId.Substring(1);
            //}
            if (maleModelId.Contains(','))
            {
                ids = maleModelId.Split(',').Select(int.Parse).ToArray();
                itemIndex = ids[0];
            }
            else
            {
                if (!int.TryParse(maleModelId, out itemIndex)) return null;
                ids = new int[1] { itemIndex };
            }
        }

        if (ids.Length == 0) return null;

        var shoulderIndex = 20;
        var upperArmIndex = 13;
        var armLowerIndex = 5;

        var output = new GameObject(type + "_" + material);
        switch (type)
        {

            case ItemType.Boots:
                ActivateAndInstantiate(maleLegsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(maleLegsRight[itemIndex], output.transform);
                break;

            case ItemType.Chest:
                ActivateAndInstantiate(maleTorso[itemIndex], output.transform);

                if (ids.Length > 1)
                {
                    shoulderIndex = ids[1];
                    upperArmIndex = ids[2];
                }

                ActivateAndInstantiate(shoulderPadsLeft[shoulderIndex], output.transform);
                ActivateAndInstantiate(shoulderPadsRight[shoulderIndex], output.transform);

                ActivateAndInstantiate(maleArmUpperLeft[upperArmIndex], output.transform);
                ActivateAndInstantiate(maleArmUpperRight[upperArmIndex], output.transform);
                break;

            case ItemType.Gloves:
                ActivateAndInstantiate(maleHandsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(maleHandsRight[itemIndex], output.transform);

                if (ids.Length > 1)
                {
                    armLowerIndex = ids[1];
                }

                ActivateAndInstantiate(maleArmLowerLeft[armLowerIndex], output.transform);
                ActivateAndInstantiate(maleArmLowerRight[armLowerIndex], output.transform);
                break;
            case ItemType.LeftShoulder:
            case ItemType.RightShoulder:
                ActivateAndInstantiate(shoulderPadsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(shoulderPadsRight[itemIndex], output.transform);
                break;

            case ItemType.Hat:
            case ItemType.Mask:
            case ItemType.Helmet:
            case ItemType.HeadCovering:

                var isHelmet = type == ItemType.Helmet;
                var isMask = type == ItemType.Mask;
                var isHat = type == ItemType.Hat;

                var itemObjs = isHelmet ? maleHelmets : isMask ? masks : isHat ? hats : headCoverings;

                ActivateAndInstantiate(itemObjs[itemIndex], output.transform);

                if (ids.Length > 1)
                {
                    foreach (var headAttachment in ids.Skip(1))
                    {
                        if (headAttachment < 0) continue;
                        if (headAttachments.Length > headAttachment)
                        {
                            ActivateAndInstantiate(headAttachments[headAttachment], output.transform);
                        }
                    }
                }
                break;
            case ItemType.Leggings:
                ActivateAndInstantiate(maleHips[itemIndex], output.transform);
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

    private void ActivateAndInstantiate(GameObject obj, Transform parent)
    {
        obj.SetActive(true);
        Instantiate(obj, parent);
    }

    private void EquipArmor(ItemType itemType, int itemIndex, int material, params int[] additionalIndices)
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
                    // 0: shoulder
                    var shoulderIndex = Gender == Gender.Male ? 20 : 14;
                    if (additionalIndices.Length > 0)
                    {
                        shoulderIndex = additionalIndices[0];
                    }

                    EquipPair(
                        shoulderPadsLeft[shoulderIndex],
                        shoulderPadsRight[shoulderIndex],
                        newMaterial);

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

                    // elbows
                    var elbowIndex = 0;
                    if (additionalIndices.Length > 2)
                    {
                        elbowIndex = additionalIndices[2];
                    }
                    EquipPair(elbowsLeft[elbowIndex], elbowsRight[elbowIndex], newMaterial);
                }
                else
                {
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

                if (itemIndex > 0)
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

                equippedHelmet = EquipItems(
                    isHelmet ? maleHelmets : isMask ? masks : isHat ? hats : headCoverings,
                    isHelmet ? femaleHelmets : isMask ? masks : isHat ? hats : headCoverings,
                    itemIndex, newMaterial);

                if (additionalIndices.Length > 0)
                {
                    var i = 0;
                    foreach (var headAttachment in additionalIndices)
                    {
                        Equip(headAttachments[headAttachment], newMaterial, i++ == 0);
                    }
                }
                UpdateHelmetVisibility();
                break;
            case ItemType.Leggings:
                EquipItems(maleHips, femaleHips, itemIndex, newMaterial);
                if (additionalIndices.Length > 0)
                {
                    var kneepadIndex = additionalIndices[0];
                    EquipItems(kneeAttachmentsLeft, kneeAttachmentsLeft, kneepadIndex, newMaterial);
                    EquipItems(kneeAttachmentsRight, kneeAttachmentsRight, kneepadIndex, newMaterial);
                }
                break;
        }
    }
    private GameObject EquipItems(
        GameObject[] male,
        GameObject[] female,
        int newItemIndex,
        Material material)
    {
        return Equip(Gender == Gender.Male ? male[newItemIndex] : female[newItemIndex], material);
    }

    private void EquipPairItems(
        GameObject[] genericLeft,
        GameObject[] genericRight,
        int newItemIndex,
        Material material)
    {
        EquipPair(genericLeft[newItemIndex], genericRight[newItemIndex], material);
    }

    private void EquipPairItems(
        GameObject[] maleLeft,
        GameObject[] maleRight,
        GameObject[] femaleLeft,
        GameObject[] femaleRight,
        int newItemIndex,
        Material material)
    {
        var leftNew = Gender == Gender.Male
            ? maleLeft[newItemIndex]
            : femaleLeft[newItemIndex];

        var rightNew = Gender == Gender.Male
            ? maleRight[newItemIndex]
            : femaleRight[newItemIndex];

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
        if (item.transform.parent && deactivateOthers)
        {
            for (var i = 0; i < item.transform.parent.childCount; ++i)
            {
                item.transform.parent.GetChild(i).gameObject.SetActive(false);
            }
        }

        item.SetActive(true);
        if (material)
        {
            item.GetComponent<SkinnedMeshRenderer>().sharedMaterial = material;
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
            var helmets = Gender == Gender.Female ? femaleHelmets : maleHelmets;

            if (!equippedHelmet && Helmet < 0)
            {
                return;
            }

            //var toggleHair = true;
            //var toggleHead = true;
            //var toggleFacials = true;

            if (equippedHelmet)
            {
                if (equippedHelmet.name.IndexOf("no_elements", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Toggle All off
                }
                else // Check if _Base_Hair (Hat), _No_FacialHair (Mask), _No_Hair (HeadCovering)
                {
                    // ...
                }
            }

            if (Gender == Gender.Male)
            {
                Head = Mathf.Clamp(Head, 0, maleHeads.Length - 1);
                FacialHair = Mathf.Clamp(FacialHair, 0, maleFacialHairs.Length - 1);
                Eyebrows = Mathf.Clamp(Eyebrows, 0, maleEyebrows.Length - 1);

                if (FacialHair < maleFacialHairs.Length)
                    maleFacialHairs[FacialHair].gameObject.SetActive(!HelmetVisible);

                maleHeads[Head].gameObject.SetActive(!HelmetVisible);

                if (Eyebrows < maleEyebrows.Length)
                    maleEyebrows[Eyebrows].gameObject.SetActive(!HelmetVisible);
            }
            else
            {
                Head = Mathf.Clamp(Head, 0, femaleHeads.Length - 1);
                Eyebrows = Mathf.Clamp(Eyebrows, 0, femaleEyebrows.Length - 1);

                femaleHeads[Head].gameObject.SetActive(!HelmetVisible);
                if (Eyebrows < femaleEyebrows.Length)
                    femaleEyebrows[Eyebrows].gameObject.SetActive(!HelmetVisible);
            }

            Hair = Mathf.Clamp(Hair, 0, hairs.Length - 1);

            if (Hair < hairs.Length)
            {
                hairs[Hair].gameObject.SetActive(!HelmetVisible);
            }

            if (equippedHelmet)
            {
                equippedHelmet.SetActive(HelmetVisible);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Failed to toggle helmet visibility for " + this.player.Name + ": " + exc);
        }
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
