using MTAssets;
using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SyntyPlayerAppearance : MonoBehaviour, IPlayerAppearance
{
    private readonly Dictionary<ItemType, Transform> equipmentSlots = new Dictionary<ItemType, Transform>();
    private readonly Dictionary<ItemType, GameObject> equippedObjects = new Dictionary<ItemType, GameObject>();
    private readonly Dictionary<ItemType, ItemController> equippedItems = new Dictionary<ItemType, ItemController>();

    [Header("Editor")]
    [SerializeField] private bool PhotoMode;
    [SerializeField] private ItemManager itemManager;

    [Header("Optimizer")]
    [SerializeField] private SkinnedMeshCombiner meshCombiner;

    /* Genderless Models */
    [Header("Generic Model Objects")]
    //[SerializeField] private GameObject[] hairCoverings;    
    [SerializeField] private GameObject[] faceCoverings;
    //[SerializeField] private GameObject[] headCoverings;

    [SerializeField] private GameObject[] hairs;
    //[SerializeField] private GameObject[] helmetAttachments;

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
    public int Torso = 0;
    public int ArmUpper = 0;
    public int ArmLower = 0;
    public int Hands = 0;
    public int Hips = 0;
    public int Legs = 0;

    public Color SkinColor;
    public Color HairColor;
    public Color BeardColor;
    public Color StubbleColor;
    //public Color EyebrowsColor;
    public Color WarPaintColor;
    public Color ScarColor;
    public Color EyeColor;

    public bool HelmetVisible;

    //public int HelmetAttachment;
    //public int HipsAttachment;

    private Dictionary<string, GameObject[]> modelObjects;
    private GameObject equippedHelmet;
    private GameManager gameManager;

    Gender IPlayerAppearance.Gender => Gender;

    public Transform MainHandTransform => equipmentSlots[ItemType.TwoHandedSword];

    public Transform OffHandTransform => equipmentSlots[ItemType.Shield];

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (!meshCombiner) meshCombiner = GetComponent<SkinnedMeshCombiner>();
        UpdateBoneTransforms();
    }

    public void Equip(ItemController item)
    {
        if (item.Category == ItemCategory.Armor)
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
                Destroy(currentItem.gameObject);
            }
            else if (equippedObjects.TryGetValue(item.Type, out var currentObject))
            {
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

    public void SetAppearance(Appearance appearance)
    {
        throw new NotSupportedException();
    }

    public void SetAppearance(SyntyAppearance appearance)
    {
        ResetAppearance();

        var props = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(x => x.Name, x => x);
        foreach (var prop in appearance
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (props.TryGetValue(prop.Name, out var p))
            {
                var valueToSet = prop.GetValue(appearance);
                try
                {
                    if (p.FieldType == typeof(Color))
                    {
                        p.SetValue(this, GetColorFromHex(valueToSet?.ToString()));
                    }
                    else
                    {
                        p.SetValue(this, valueToSet);
                    }

                }
                catch (Exception exc)
                {
                    Debug.LogError(exc.ToString());
                }
            }
        }

        UpdateAppearance();
        Optimize();
    }

    private Color GetColorFromHex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out var color);
        return color;
    }

    public bool TryUpdate(int[] values)
    {
        return false;
    }

    public void UnEquip(ItemController item)
    {
        UnEquip(item.gameObject);
    }

    public void ResetAppearance()
    {
        var allModels = GetAll();
        foreach (var model in allModels) model?.SetActive(false);
        if (meshCombiner?.isMeshesCombineds ?? false)
            meshCombiner?.UndoCombineMeshes();
    }

    public void Optimize(Action afterUndo = null)
    {
        int meshLayer = -1;
        if (transform.Find("Combined Mesh"))
        {
            meshLayer = meshCombiner.gameObject.layer;
            meshCombiner.UndoCombineMeshes();
        }


        afterUndo?.Invoke();

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

        meshCombiner.CombineMeshes();
        gameManager.Camera.EnsureObserverCamera();

    }

    public void UpdateAppearance()
    {
        ResetAppearance();
        var models = GetAllModels();
        var fields = GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.FieldType == typeof(int))
            .ToList();

        fields.ForEach(field =>
        {
            var items = models.Where(x =>
                x.Key.StartsWith(field.Name, StringComparison.OrdinalIgnoreCase) ||
                x.Key.StartsWith(Gender.ToString() + field.Name, StringComparison.OrdinalIgnoreCase));

            var index = (int)field.GetValue(this);
            if (index == -1) return;
            foreach (var item in items)
            {
                try
                {
                    // female facial hairs Kappa
                    if (item.Value.Length == 0)
                    {
                        continue;
                    }

                    if (index >= item.Value.Length && item.Key != nameof(femaleHeads) && item.Key != nameof(maleHeads))
                        continue;

                    if (item.Value.Length > 0)
                    {
                        index = Mathf.Min(item.Value.Length - 1, index);
                    }

                    item.Value[index].SetActive(true);
                    var renderer = item.Value[index].GetComponent<SkinnedMeshRenderer>();
                    if (renderer)
                    {
                        //renderer.material =   //itemMaterials.Random();

                        if (item.Key == nameof(femaleHeads) || item.Key == nameof(maleHeads))
                        {
                            renderer.material.SetColor("_Color_Eyes", EyeColor);
                            renderer.material.SetColor("_Color_Skin", SkinColor);
                            //renderer.material.SetColor("_Color_Scar", ScarColor);
                            renderer.material.SetColor("_Color_Stubble", StubbleColor);
                            renderer.material.SetColor("_Color_BodyArt", WarPaintColor);
                        }
                        else if (item.Key == nameof(maleHeads) || item.Key == nameof(maleFacialHairs))
                        {
                            //renderer.material.SetColor("_Color_Stubble", BeardColor);
                            renderer.material.SetColor("_Color_Hair", BeardColor);
                        }
                        else if (item.Key == nameof(maleEyebrows) || item.Key == nameof(femaleEyebrows))
                        {
                            renderer.material.SetColor("_Color_Hair", HairColor);
                        }
                        else if (item.Key == nameof(hairs))
                        {
                            renderer.material.SetColor("_Color_Hair", HairColor);
                        }
                        // when unequipped, we need to update the skin color to match
                        // any other items should not have material changed or it will be instanced.
                        else if (index == 0)
                        {
                            renderer.material.SetColor("_Color_Skin", SkinColor);
                        }

                        //renderer.material.SetColor("_Color_Eyes", EyeColor);
                        //renderer.material.SetColor("_Color_Hair", HairColor);
                        //renderer.material.SetColor("_Color_Skin", SkinColor);
                        //renderer.material.SetColor("_Color_Stubble", BeardColor);
                    }
                }
                catch
                {
                    Debug.LogError($"({field.Name}) {item.Key}[{index}] out of bounds, {item.Key}.Length = {item.Value.Length}");
                }
            }
        });
    }

    public SyntyAppearance ToSyntyAppearanceData()
    {
        return new SyntyAppearance();
    }

    public int[] ToAppearanceData()
    {
        throw new NotSupportedException();
    }

    #region Private Members
    private void EquipArmor(ItemController item)
    {
        int itemIndex;
        if (Gender == Gender.Male
            ? int.TryParse(item.MaleModelID, out itemIndex)
            : int.TryParse(item.FemaleModelID, out itemIndex))
        {
            EquipArmor(item.Type, itemIndex, (int)item.Material - 1);
        }
    }

    internal GameObject Get(ItemType type, ItemMaterial material, string maleModelId)
    {
        var itemMaterial = (gameManager ? gameManager.Items : itemManager).GetMaterial((int)material - 1);
        if (!int.TryParse(maleModelId, out var itemIndex)) return null;
        var output = new GameObject(type + "_" + material);
        switch (type)
        {

            case ItemType.Boots:
                ActivateAndInstantiate(maleLegsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(maleLegsRight[itemIndex], output.transform);
                break;

            case ItemType.Chest:
                ActivateAndInstantiate(maleTorso[itemIndex], output.transform);
                ActivateAndInstantiate(maleArmUpperLeft[13], output.transform);
                ActivateAndInstantiate(maleArmUpperRight[13], output.transform);
                ActivateAndInstantiate(shoulderPadsLeft[20], output.transform);
                ActivateAndInstantiate(shoulderPadsRight[20], output.transform);
                break;

            case ItemType.Gloves:
                ActivateAndInstantiate(maleHandsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(maleHandsRight[itemIndex], output.transform);
                ActivateAndInstantiate(maleArmLowerLeft[5], output.transform);
                ActivateAndInstantiate(maleArmLowerRight[5], output.transform);
                break;
            case ItemType.LeftShoulder:
            case ItemType.RightShoulder:
                ActivateAndInstantiate(shoulderPadsLeft[itemIndex], output.transform);
                ActivateAndInstantiate(shoulderPadsRight[itemIndex], output.transform);
                break;
            case ItemType.Helm:
                ActivateAndInstantiate(maleHelmets[itemIndex], output.transform);
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

    private void EquipArmor(ItemType itemType, int itemIndex, int material)
    {
        var newMaterial = gameManager.Items.GetMaterial(material);
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

                // equip or unequip upper armsF
                if (itemIndex > 0)
                {
                    EquipPairItems(
                        maleArmUpperLeft, maleArmUpperRight,
                        femaleArmUpperLeft, femaleArmUpperRight,
                        Gender == Gender.Male ? 13 : 12, newMaterial);

                    var shoulderIndex = Gender == Gender.Male ? 20 : 14;
                    EquipPair(
                        shoulderPadsLeft[shoulderIndex],
                        shoulderPadsRight[shoulderIndex],
                        newMaterial);

                    EquipPair(elbowsLeft[0], elbowsRight[0], newMaterial);
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
                    EquipPairItems(
                        maleArmLowerLeft, maleArmLowerRight,
                        femaleArmLowerLeft, femaleArmLowerRight,
                            Gender == Gender.Male ? 5 : 15, newMaterial);
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
            case ItemType.Helm:
                equippedHelmet = EquipItems(maleHelmets, femaleHelmets, itemIndex, newMaterial);
                UpdateHelmetVisibility();
                break;
            case ItemType.Leggings:
                EquipItems(maleHips, femaleHips, itemIndex, newMaterial);
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
    private GameObject Equip(GameObject item, Material material)
    {
        if (item.transform.parent)
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

    private GameObject[] GetAll()
    {
        return GetAllModels().SelectMany(x => x.Value).ToArray();
    }

    private IReadOnlyDictionary<string, GameObject[]> GetAllModels()
    {
        if (modelObjects == null)
        {
            modelObjects = GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.FieldType == typeof(GameObject[]))
                .Select(x => new { Key = x.Name, Value = x.GetValue(this) as GameObject[] })
                .ToDictionary(x => x.Key, x => x.Value);
        }

        return modelObjects;
    }

    private void RandomizeCharacter()
    {
        Gender = Enum.GetValues(typeof(Gender)).Cast<Gender>().Random();
        var models = GetAllModels();
        var fields = GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.FieldType == typeof(int))
            .ToList();

        EyeColor = RandomColor();
        HairColor = RandomColor();
        BeardColor = RandomColor();
        SkinColor = RandomColor();

        fields.ForEach(field =>
        {
            var item = models
            .FirstOrDefault(x => x.Key.StartsWith(field.Name, StringComparison.OrdinalIgnoreCase) || x.Key.StartsWith(Gender.ToString() + field.Name, StringComparison.OrdinalIgnoreCase));

            if (item.Value != null && item.Value.Length > 0)
            {
                field.SetValue(this, item.Value.RandomIndex());
            }
        });
    }
    //public Material GetMaterial(int material)
    //{
    //    return material >= 0 && this.itemMaterials.Length > material ? this.itemMaterials[material] : null;
    //}
    private void UpdateBoneTransforms()
    {
        //this.equipmentSlots[ItemType.] = this.transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Back_Attachment");

        var mainHand = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R"); ;
        var offHand = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_L/Shoulder_L/Elbow_L/Hand_L"); ;
        var shoulderLeft = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_L/Shoulder_L"); ;
        var shoulderRight = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R"); ;

        equipmentSlots[ItemType.Amulet] = transform.Find("Root/Hips/Spine_01/Spine_02/Spine_03/Neck");
        equipmentSlots[ItemType.TwoHandedSword] = mainHand;
        equipmentSlots[ItemType.TwoHandedStaff] = mainHand;
        equipmentSlots[ItemType.TwoHandedBow] = mainHand;
        equipmentSlots[ItemType.TwoHandedAxe] = mainHand;
        equipmentSlots[ItemType.OneHandedAxe] = mainHand;
        equipmentSlots[ItemType.OneHandedMace] = mainHand;
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
        Optimize(UpdateHelmetVisibility);
    }

    private void UpdateHelmetVisibility()
    {
        var helmets = Gender == Gender.Female ? femaleHelmets : maleHelmets;

        if (!equippedHelmet)
        {
            return;
        }

        if (Gender == Gender.Male)
        {
            Head = Math.Min(Head, maleHeads.Length - 1);
            //FacialHair = Math.Min(FacialHair, maleFacialHairs.Length - 1);
            //Eyebrows = Math.Min(Eyebrows, maleEyebrows.Length - 1);

            if (FacialHair < maleFacialHairs.Length)
                maleFacialHairs[FacialHair].gameObject.SetActive(!HelmetVisible);

            maleHeads[Head].gameObject.SetActive(!HelmetVisible);

            if (Eyebrows < maleEyebrows.Length)
                maleEyebrows[Eyebrows].gameObject.SetActive(!HelmetVisible);
        }
        else
        {
            Head = Math.Min(Head, femaleHeads.Length - 1);
            //Eyebrows = Math.Min(Eyebrows, femaleEyebrows.Length - 1);

            femaleHeads[Head].gameObject.SetActive(!HelmetVisible);
            if (Eyebrows < femaleEyebrows.Length)
                femaleEyebrows[Eyebrows].gameObject.SetActive(!HelmetVisible);
        }

        if (Hair < hairs.Length)
        {
            hairs[Hair].gameObject.SetActive(!HelmetVisible);
        }

        equippedHelmet.SetActive(HelmetVisible);
    }

    #endregion
}

public static class IEnumerableExtensions
{
    public static IReadOnlyList<decimal> Delta(this IList<decimal> newValue, IReadOnlyList<decimal> oldValue)
    {
        if (oldValue == null)
        {
            return new List<decimal>(newValue.Count);
        }
        if (newValue.Count != oldValue.Count)
        {
            return new List<decimal>(newValue.Count);
        }

        return newValue.Select((x, i) => x - oldValue[i]).ToList();
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T except)
    {
        return items.Where(x => !x.Equals(except));
    }

    public static int RandomIndex<T>(this IEnumerable<T> items)
    {
        return Mathf.FloorToInt(UnityEngine.Random.value * items.Count());
    }
    public static T Random<T>(this IEnumerable<T> items)
    {
        var selections = items.ToList();
        return selections[Mathf.FloorToInt(UnityEngine.Random.value * selections.Count)];
    }
}