using Newtonsoft.Json;
using RavenNest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ItemCreator : MonoBehaviour
{
    [SerializeField] private SyntyPlayerAppearance appearance;
    [SerializeField] private TMPro.TextMeshProUGUI settingsInfo;
    [SerializeField] private TMPro.TMP_Dropdown materialDropDown;

    [SerializeField] private Toggle isMeleeGear;
    [SerializeField] private Toggle isArcherGear;
    [SerializeField] private Toggle isMageGear;

    [SerializeField] private GameObject listItemPrefab;
    [SerializeField] private Transform headAttachmentContainer;

    [SerializeField] private Transform itemButtonContainer;
    private ItemMaterial[] materialOptions;
    private ItemMaterial selectedMaterial;
    private HashSet<string> usedMaterials;

    private Item[] gameItems;

    public bool ItemIsCraftable;
    public int LevelRequirementOffset = 10;
    private bool generateMeleeGear;
    private bool generateArcherGear;
    private bool generateMageGear;

    // Start is called before the first frame update
    void Start()
    {

        isMeleeGear.onValueChanged.AddListener(v =>
        {
            this.generateMeleeGear = isMeleeGear.isOn;
            if (generateMeleeGear)
            {
                isArcherGear.SetIsOnWithoutNotify(false);
                isMageGear.SetIsOnWithoutNotify(false);

                this.generateArcherGear = false;
                this.generateMageGear = false;
            }
        });

        isArcherGear.onValueChanged.AddListener(v =>
        {
            this.generateArcherGear = isArcherGear.isOn;
            if (generateArcherGear)
            {
                isMeleeGear.SetIsOnWithoutNotify(false);
                isMageGear.SetIsOnWithoutNotify(false);

                this.generateMeleeGear = false;
                this.generateMageGear = false;
            }
        });

        isMageGear.onValueChanged.AddListener(v =>
        {
            this.generateMageGear = isMageGear.isOn;
            if (generateMageGear)
            {
                isArcherGear.SetIsOnWithoutNotify(false);
                isMeleeGear.SetIsOnWithoutNotify(false);

                this.generateArcherGear = false;
                this.generateMeleeGear = false;
            }
        });

        var itemsRepo = @"C:\git\Ravenfall Legacy\Data\Repositories\items.json";

        System.Net.WebClient cl = new System.Net.WebClient();
        try
        {
            cl.DownloadFile("https://www.ravenfall.stream/api/items", itemsRepo);
            Shinobytes.Debug.Log("Downloaded new items repo");
        }
        catch { }

        var json = System.IO.File.ReadAllText(itemsRepo);
        gameItems = JsonConvert.DeserializeObject<Item[]>(json);

        if (itemButtonContainer)
        {
            var buttons = itemButtonContainer.GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                btn.onClick.AddListener(() => MakeItem(btn.name));
            }
        }
        else
        {
            Shinobytes.Debug.LogError("No item button container set.");
        }

        this.usedMaterials = appearance.FromActive();

        FillHeadAttachments();

        FillMaterials();

        //appearance.RandomizeCharacter();
        //appearance.UpdateAppearance();

        UpdateSettingsInfo();
    }


    private void FillMaterials()
    {

        materialOptions = Enum.GetValues(typeof(ItemMaterial)).Cast<ItemMaterial>().ToArray();
        var strMaterialOptions = materialOptions.Select(x => x.ToString()).ToList();

        materialDropDown.ClearOptions();
        materialDropDown.AddOptions(strMaterialOptions);

        if (usedMaterials.Count > 0)
        {
            foreach (var mat in usedMaterials)
            {
                Shinobytes.Debug.Log(mat);
                var i = strMaterialOptions.IndexOf(mat);
                if (i >= 0)
                {
                    materialDropDown.value = i;
                    selectedMaterial = materialOptions[i];
                    break;
                }
            }
        }

        materialDropDown.onValueChanged.AddListener((index) =>
        {
            this.selectedMaterial = materialOptions[index];
        });
    }
    private void FillHeadAttachments()
    {
        GameObject[] array = appearance.GetHeadAttachments();
        var attachments = new HashSet<int>(appearance.HeadAttachments);
        for (int i = 0; i < array.Length; i++)
        {
            GameObject attachment = array[i];
            var item = Instantiate(listItemPrefab, headAttachmentContainer);
            var toggle = item.GetComponent<Toggle>();
            toggle.name = i.ToString();
            toggle.isOn = attachments.Contains(i);
            toggle.onValueChanged.AddListener((value) => OnHeadAttachmentToggled(toggle, value));

            var text = item.GetComponentInChildren<Text>();
            text.text = attachment.name;
        }
    }

    private void OnHeadAttachmentToggled(Toggle toggle, bool value)
    {
        var index = int.Parse(toggle.name);
        var attachments = new HashSet<int>(appearance.HeadAttachments);
        if (value) { attachments.Add(index); }
        else { attachments.Remove(index); }
        appearance.HeadAttachments = attachments.ToArray();
        UpdateDetails();
    }

    private void MakeItem(string type)
    {
        Item item = null;
        try
        {
            var isKatana = type.Contains("Katana") || type.Contains("katana");
            if (isKatana)
            {
                item = MakeKatana(selectedMaterial);
                Shinobytes.Debug.LogError(FormatItem(item));
                return;
            }

            if (Enum.TryParse<ItemType>(type, true, out var result))
            {
                item = MakeItem(result, false);
                Shinobytes.Debug.LogError(FormatItem(item));
            }
            else
            {
                Shinobytes.Debug.LogError(type + " BAD");
            }
        }
        finally
        {
            if (item != null && item.IsGenericModel)
            {
                var prefab = UnityEngine.Resources.Load(item.GenericPrefab);
                if (!prefab)
                {
                    Shinobytes.Debug.LogError(item.GenericPrefab + ".prefab does not exist. Make sure it exists before adding the item to the server.");
                }
            }
        }
    }

    private Item MakeItem(ItemType itemType, bool isKatana)
    {
        var category = GetItemCategory(itemType);
        var itemMaterialName = selectedMaterial.ToString();
        var itemTypeName = ToName(itemType);
        if (generateArcherGear)
        {
            itemMaterialName = "Leather";
        }
        if (generateMageGear)
        {
            itemMaterialName = "Arcane";
            if (itemType == ItemType.Chest)
            {
                itemTypeName = "Robe";
            }
        }

        var item = MakeItem(itemMaterialName + " " + itemTypeName, category, itemType, selectedMaterial);
        var existingItemsOfType = gameItems.Where(x => x.Category == category && x.Type == itemType).OrderBy(x => x.GetTotalStats()).ToArray();

        var scale_armorPower = 0f;
        var scale_magicPower = 0f;
        var scale_weaponPower = 0f;
        var scale_rangedPower = 0f;

        var scale_magicAim = 0f;
        var scale_weaponAim = 0f;
        var scale_rangedAim = 0f;

        if (existingItemsOfType.Length > 2)
        {
            var prevMatA = (ItemMaterial)(((int)selectedMaterial) - 1);
            var prevMatB = (ItemMaterial)(((int)selectedMaterial) - 2);

            Func<string, bool> katanaCheck = (string name) =>
                isKatana ? name.Contains("Katana") : !name.Contains("Katana");

            var prevA = existingItemsOfType.FirstOrDefault(x => x.Material == prevMatA || x.Name.Contains(ToName(prevMatA)) && katanaCheck(x.Name));
            var prevB = existingItemsOfType.FirstOrDefault(x => x.Material == prevMatB || x.Name.Contains(ToName(prevMatB)) && katanaCheck(x.Name));

            var a = prevA.Material == ItemMaterial.None ? prevA : existingItemsOfType[existingItemsOfType.Length - 1];
            var b = prevA.Material == ItemMaterial.None ? prevB : existingItemsOfType[existingItemsOfType.Length - 2];

            scale_magicPower = (float)a.MagicPower / b.MagicPower;
            scale_armorPower = (float)a.ArmorPower / b.ArmorPower;
            scale_weaponPower = (float)a.WeaponPower / b.WeaponPower;
            scale_rangedPower = (float)a.RangedPower / b.RangedPower;

            scale_magicAim = (float)a.MagicAim / b.MagicAim;
            scale_weaponAim = (float)a.WeaponAim / b.WeaponAim;
            scale_rangedAim = (float)a.RangedAim / b.RangedAim;

            var scale_ranged = (float)a.RequiredRangedLevel / b.RequiredRangedLevel;
            var scale_attack = (float)a.RequiredAttackLevel / b.RequiredAttackLevel;
            var scale_defense = (float)a.RequiredDefenseLevel / b.RequiredDefenseLevel;


            var scale_crafting = (float)a.RequiredCraftingLevel / b.RequiredCraftingLevel;


            var scale_magic = (float)a.RequiredMagicLevel / b.RequiredMagicLevel;
            var scale_slayer = (float)a.RequiredSlayerLevel / b.RequiredSlayerLevel;

            if (prevA != null)
            {

                item.RequiredRangedLevel = RoundUp(Mathf.Max(0, (int)(prevA.RequiredRangedLevel * scale_ranged)));
                item.RequiredAttackLevel = RoundUp(Mathf.Max(0, (int)(prevA.RequiredAttackLevel * scale_attack)));
                item.RequiredDefenseLevel = RoundUp(Mathf.Max(0, (int)(prevA.RequiredDefenseLevel * scale_defense)));
                item.RequiredMagicLevel = RoundUp(Mathf.Max(0, (int)(prevA.RequiredMagicLevel * scale_magic)));
                item.RequiredSlayerLevel = RoundUp(Mathf.Max(0, (int)(prevA.RequiredSlayerLevel * scale_slayer)));


                if (item.RequiredRangedLevel > 0)
                    item.RequiredRangedLevel += LevelRequirementOffset;

                if (item.RequiredAttackLevel > 0)
                    item.RequiredAttackLevel += LevelRequirementOffset;

                if (item.RequiredDefenseLevel > 0)
                    item.RequiredDefenseLevel += LevelRequirementOffset;

                if (item.RequiredMagicLevel > 0)
                    item.RequiredMagicLevel += LevelRequirementOffset;

                if (item.RequiredSlayerLevel > 0)
                    item.RequiredSlayerLevel += LevelRequirementOffset;

                if (ItemIsCraftable)
                {
                    item.RequiredCraftingLevel = RoundUp(Mathf.Max(0, (int)((prevA.RequiredCraftingLevel + LevelRequirementOffset) * scale_crafting)));
                    item.Craftable = true;
                }
                else
                {
                    item.RequiredCraftingLevel = 1000;
                    item.Craftable = false;
                }

                if (generateMageGear)
                {
                    var rangeAim = Mathf.Max(0, (int)(prevA.RangedAim * scale_rangedAim));
                    var rangePower = Mathf.Max(0, (int)(prevA.RangedPower * scale_rangedPower));

                    var weapAim = Mathf.Max(0, (int)(prevA.WeaponAim * scale_weaponAim));
                    var weapPower = Mathf.Max(0, (int)(prevA.WeaponPower * scale_weaponPower));


                    item.MagicAim = Mathf.Max(0, (int)(prevA.MagicAim * scale_magicAim));
                    item.MagicPower = Mathf.Max(0, (int)(prevA.MagicPower * scale_magicPower));

                    item.ArmorPower = Mathf.Max(0, (int)(prevA.ArmorPower * scale_armorPower));
                }
                else if (generateArcherGear)
                {
                    var weapAim = Mathf.Max(0, (int)(prevA.WeaponAim * scale_weaponAim));
                    var weapPower = Mathf.Max(0, (int)(prevA.WeaponPower * scale_weaponPower));

                    var magicAim = Mathf.Max(0, (int)(prevA.MagicAim * scale_magicAim));
                    var magicPower = Mathf.Max(0, (int)(prevA.MagicPower * scale_magicPower));

                    item.RangedAim = Mathf.Max(0, (int)(prevA.RangedAim * scale_rangedAim));
                    item.RangedPower = Mathf.Max(0, (int)(prevA.RangedPower * scale_rangedPower));

                    item.ArmorPower = Mathf.Max(0, (int)(prevA.ArmorPower * scale_armorPower));
                }
                else
                {
                    item.MagicAim = Mathf.Max(0, (int)(prevA.MagicAim * scale_magicAim));
                    item.MagicPower = Mathf.Max(0, (int)(prevA.MagicPower * scale_magicPower));

                    item.RangedAim = Mathf.Max(0, (int)(prevA.RangedAim * scale_rangedAim));
                    item.RangedPower = Mathf.Max(0, (int)(prevA.RangedPower * scale_rangedPower));

                    item.WeaponAim = Mathf.Max(0, (int)(prevA.WeaponAim * scale_weaponAim));
                    item.WeaponPower = Mathf.Max(0, (int)(prevA.WeaponPower * scale_weaponPower));

                    item.ArmorPower = Mathf.Max(0, (int)(prevA.ArmorPower * scale_armorPower));
                }
            }
        }
        if (item.Type == ItemType.Hat)
        {
            // first index is helmet to use
            // optional: any amount of indices afterwards are attachments
            var modelId = string.Join(",", appearance.Hat, string.Join(",", new HashSet<int>(appearance.HeadAttachments).ToArray()));
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Mask)
        {
            // first index is helmet to use
            // optional: any amount of indices afterwards are attachments
            var modelId = appearance.Mask.ToString();//string.Join(",", appearance.Mask, string.Join(",", new HashSet<int>(appearance.HeadAttachments).ToArray()));
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.HeadCovering)
        {
            // first index is helmet to use
            // optional: any amount of indices afterwards are attachments
            var modelId = string.Join(",", appearance.HeadCovering, string.Join(",", new HashSet<int>(appearance.HeadAttachments).ToArray()));
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Helmet)
        {
            // first index is helmet to use
            // optional: any amount of indices afterwards are attachments
            var modelId = string.Join(",", appearance.Helmet, string.Join(",", new HashSet<int>(appearance.HeadAttachments).ToArray()));
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Chest)
        {
            // [0]:chest piece
            // [1]:shoulder piece (default is 20 for male and 14 for female)
            // [2]:upper arms (default is 13 for male and 12 for female)
            // [3]:elbows (default is 0)

            var modelId = string.Join(",", appearance.Torso, appearance.Shoulder, appearance.ArmUpper, appearance.Elbow);
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Gloves)
        {
            var modelId = string.Join(",", appearance.Hands, appearance.ArmLower);
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Leggings)
        {
            //todo: kneepads not implemented
            var modelId = appearance.Hips.ToString();
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else if (item.Type == ItemType.Boots)
        {
            var modelId = appearance.Legs.ToString();
            item.FemaleModelId = modelId;
            item.MaleModelId = modelId;
        }
        else
        {
            item.Material = ItemMaterial.None;
            item.IsGenericModel = true;

            var prefabUrl = "";

            if (item.Type == ItemType.Shield)
            {
                prefabUrl = "Character/Weapons/Shields/" + item.Name;
            }
            else if (category == ItemCategory.Weapon)
            {
                prefabUrl = "Character/Weapons/";

                if (item.Type == ItemType.OneHandedSword || item.Type == ItemType.TwoHandedSword)
                {
                    prefabUrl += "Swords/" + item.Name;
                }

                if (item.Type == ItemType.TwoHandedAxe || item.Type == ItemType.OneHandedAxe)
                {
                    prefabUrl += "Axes/" + item.Name;
                }

                if (item.Type == ItemType.TwoHandedBow)
                {
                    prefabUrl += "Bows/" + item.Name;
                }

                if (item.Type == ItemType.TwoHandedStaff)
                {
                    prefabUrl += "Staffs/" + item.Name;
                }
            }

            item.GenericPrefab = prefabUrl;
        }
        return item;
    }

    private string ToName(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Helmet:
                return "Helmet";

            case ItemType.Hat:
                return "Hat";

            case ItemType.HeadCovering:
                return "Cover";

            case ItemType.Mask:
                return "Mask";

            case ItemType.TwoHandedAxe:
                return "2H Axe";

            case ItemType.TwoHandedSword:
                return "2H Sword";

            case ItemType.OneHandedAxe:
                return "Axe";

            case ItemType.OneHandedSword:
                return "Sword";

            case ItemType.OneHandedMace:
                return "Mace";

            case ItemType.TwoHandedBow:
                return "Bow";

            case ItemType.TwoHandedStaff:
                return "Staff";

            default:
                return itemType.ToString();
        }
    }

    private string ToName(ItemMaterial mat)
    {
        if (mat == ItemMaterial.Ultima)
        {
            return "Abraxas";
        }

        return mat.ToString();
    }

    private int RoundUp(float val)
    {
        return (Mathf.RoundToInt(val / 10f)) * 10;
    }

    private Item MakeKatana(ItemMaterial material)
    {
        var name = "Katana";
        if (material != ItemMaterial.None)
        {
            name = material + " Katana";
        }

        var weapon = MakeItem(ItemType.TwoHandedSword, true);

        if (!string.IsNullOrEmpty(weapon.GenericPrefab))
        {
            weapon.GenericPrefab = weapon.GenericPrefab.Replace(weapon.Name, name);
        }

        weapon.Name = name;
        return weapon;
    }

    private Item MakeItem(string name, ItemCategory category, ItemType type, ItemMaterial material)
    {
        var item = new Item();
        item.Id = Guid.NewGuid();

        item.Name = name;

        item.Type = type;
        item.Category = category;
        item.Material = material;
        item.Soulbound = false;
        item.Craftable = false;
        item.RequiredCraftingLevel = 1000;
        return item;
    }

    private string FormatItem(Item item)
    {
        var value = Newtonsoft.Json.JsonConvert.SerializeObject(item);
        GUIUtility.systemCopyBuffer = value;
        return value;
    }

    public void SetGenderMale()
    {
        appearance.RandomizeCharacter();
        appearance.Gender = RavenNest.Models.Gender.Male;
        UpdateDetails();
    }

    public void SetGenderFemale()
    {
        appearance.RandomizeCharacter();
        appearance.Gender = RavenNest.Models.Gender.Female;
        UpdateDetails();
    }


    public void HelmetPlus()
    {
        appearance.Helmet++;
        appearance.HelmetVisible = appearance.Helmet >= 0;
        UpdateDetails();
    }

    public void HelmetMinus()
    {
        appearance.Helmet--;
        appearance.HelmetVisible = appearance.Helmet >= 0;
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        appearance.UpdateAppearance();
        appearance.UpdateHelmetVisibility();
        UpdateSettingsInfo();
    }



    private void UpdateSettingsInfo()
    {
        var props = appearance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(x => x.Name, x => x);
        settingsInfo.text = "";

        foreach (var prop in props)
        {
            settingsInfo.text += prop.Key + ": ";
            var value = prop.Value.GetValue(appearance);
            if (value != null)
            {

                if (value is Array array)
                {
                    var str = "[";
                    foreach (var item in array)
                    {
                        str += item + ",";
                    }
                    str = str.TrimEnd(',');
                    settingsInfo.text += str + "]";
                }
                else
                {
                    settingsInfo.text += value;
                }

            }
            settingsInfo.text += "\r\n";
        }
    }


    private ItemCategory GetItemCategory(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.None:
                return ItemCategory.Resource;
            case ItemType.LeftShoulder:
            case ItemType.RightShoulder:
            case ItemType.Chest:
            case ItemType.Helmet:
            case ItemType.Hat:
            case ItemType.Mask:
            case ItemType.HeadCovering:
            case ItemType.Leggings:
            case ItemType.Gloves:
            case ItemType.Boots:
            case ItemType.Shield:
                return ItemCategory.Armor;

            case ItemType.Amulet:
                return ItemCategory.Amulet;
            case ItemType.Ring:
                return ItemCategory.Ring;
            case ItemType.Food:
                return ItemCategory.Food;

            case ItemType.Potion:
                return ItemCategory.Potion;

            case ItemType.TwoHandedBow:
            case ItemType.TwoHandedAxe:
            case ItemType.TwoHandedStaff:
            case ItemType.TwoHandedSword:
            case ItemType.OneHandedSword:
            case ItemType.OneHandedAxe:
                return ItemCategory.Weapon;

            case ItemType.Pet:
                return ItemCategory.Pet;

            case ItemType.StreamerToken:
                return ItemCategory.StreamerToken;

            case ItemType.Woodcutting:
            case ItemType.Farming:
            case ItemType.Fishing:
            case ItemType.Mining:
            case ItemType.Gathering:

            case ItemType.Arrows:
            case ItemType.Magic:
            case ItemType.Coins:
                return ItemCategory.Resource;

            case ItemType.Scroll:
                return ItemCategory.Scroll;

            default:
                return default(ItemCategory);

        }
    }

}
