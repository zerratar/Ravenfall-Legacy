using System;
using System.Linq;
using System.Reflection;
using RavenNest.Models;
using UnityEngine;
using Resources = UnityEngine.Resources;

public class PlayerAppearance : MonoBehaviour, IPlayerAppearance
{
    [SerializeField] private Avatar femaleAvatar;
    [SerializeField] private Avatar maleAvatar;

    //[SerializeField] private RuntimeAnimatorController skillAnimatorController;
    //[SerializeField] private RuntimeAnimatorController craftingAnimatorController;
    [SerializeField] internal GameObject Base;

    private RavenNest.Models.Appearance definition;

    public Transform EquipHead;
    public Transform EquipREar;
    public Transform EquipLEar;
    public Transform EquipRWrist;
    public Transform EquipLWrist;
    public Transform EquipRShoulder;
    public Transform EquipLShoulder;
    public Transform EquipNeck;
    public Transform EquipBack;
    public Transform EquipRHand;
    public Transform EquipLHand;
    public Transform EquipRKnee;
    public Transform EquipLKnee;

    public Gender Gender;
    public SkinColor SkinColor;
    public HairColor HairColor;
    public HairColor BrowColor;
    public HairColor BeardColor;
    public EyeColor EyeColor;
    public CostumeColor CostumeColor;

    [Range(1, 20)] public int BaseModelNumber = 1;
    [Range(1, 20)] public int TorsoModelNumber = 1;
    [Range(1, 21)] public int BottomModelNumber = 1;
    [Range(0, 6)] public int FeetModelNumber = 1;
    [Range(0, 4)] public int HandModelNumber = 1;

    [Range(0, 10)] public int BeltModelNumber = 0; // <= 0 : none    
    [Range(1, 7)] public int EyesModelNumber = 1;
    [Range(1, 15)] public int BrowsModelNumber = 1;
    [Range(1, 10)] public int MouthModelNumber = 1;
    [Range(0, 11)] public int MaleHairModelNumber = 1;
    [Range(0, 20)] public int FemaleHairModelNumber = 1;
    [Range(0, 10)] public int BeardModelNumber = 1;

    public ItemMaterial TorsoMaterial = 0; // 0: empty, 1: bronze, 2: iron, ..
    public ItemMaterial BottomMaterial = 0; // 0: empty, 1: bronze, 2: iron, ..
    public ItemMaterial FeetMaterial = 0; // 0: empty, 1: bronze, 2: iron, ..
    public ItemMaterial HandMaterial = 0; // 0: empty, 1: bronze, 2: iron, ..

    private Transform torsoModel;
    private Transform bottomModel;
    private Transform feetModel;
    private Transform handModel;

    private GameObject helmet;
    private GameObject pet;

    private GameObject hair;
    private bool appearanceUpdateRequired;

    private bool helmetVisible = true;

    private bool petVisible = true;

    Gender IPlayerAppearance.Gender => Gender;

    public Transform MainHandTransform => throw new NotImplementedException();

    public Transform OffHandTransform => throw new NotImplementedException();

    public GameObject MonsterMesh => throw new NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        //UpdateAppearance();
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    if (Base)
    //    {
    //        Base.transform.localPosition = Vector3.zero;
    //    }
    //}

    public void SetItem(ItemController item, Transform parent)
    {
        item.transform.SetParent(parent);
        item.transform.localPosition = Vector3.zero;

        if (item.Type == ItemType.Helm)
        {
            helmet = item.gameObject;

            if (!helmetVisible)
            {
                ShowHair();
                HideHelmet();
            }
        }

        if (item.Type == ItemType.Pet)
        {
            pet = item.gameObject;
            if (!petVisible)
            {
                HidePet();
            }
        }
    }

    public void HideHair()
    {
        if (hair)
        {
            hair.SetActive(false);
        }
    }

    public void ShowHair()
    {
        if (hair)
        {
            hair.SetActive(true);
        }
    }

    private void HideHelmet()
    {
        if (helmet)
        {
            helmet.SetActive(false);
        }
    }

    private void ShowHelmet()
    {
        if (helmet)
        {
            helmet.SetActive(true);
        }
    }

    private void HidePet()
    {
        if (pet)
        {
            pet.SetActive(false);
        }
    }

    private void ShowPet()
    {
        if (pet)
        {
            pet.SetActive(true);
        }
    }

    public void ToggleHelmVisibility()
    {
        helmetVisible = !helmetVisible;
        if (helmetVisible)
        {
            HideHair();
            ShowHelmet();
        }
        else
        {
            HideHelmet();
            ShowHair();
        }
    }
    public void TogglePetVisibility()
    {
        petVisible = !petVisible;
        if (petVisible)
        {
            ShowPet();
        }
        else
        {
            HidePet();
        }
    }


    public void UpdateAppearance(Sprite capeLogo = null)
    {
        if (Base)
        {
            appearanceUpdateRequired = true;
            Destroy(Base);
            Base = null;
        }

        var gender = Gender.ToString();
        var skin = SkinColor + " Skin";
        var skinColor = SkinColor.ToString().ToUpper()[0] + "S";
        var baseModelName = $"{skinColor} {gender} 01 {BaseModelNumber:00}";

        var rsxChar = System.IO.Path.Combine("Character/");
        var rsxGender = System.IO.Path.Combine(rsxChar, gender + "/");
        var rsxBase = System.IO.Path.Combine(rsxGender, "Base/", skin + "/");
        var rsxBaseModel = System.IO.Path.Combine(rsxBase, baseModelName);

        var rsxHeads = System.IO.Path.Combine(rsxGender, "Heads/");
        var rsxMouth = System.IO.Path.Combine(rsxHeads, "Mouths/", $"Mouth {MouthModelNumber:00} {skin}");
        var rsxBrows = System.IO.Path.Combine(rsxHeads, "Brows/", $"Brow {BrowsModelNumber:00} {BrowColor}");
        var rsxEyes = System.IO.Path.Combine(rsxHeads, "Eyes/", $"Eyes {EyesModelNumber:00} {EyeColor}");
        var rsxBeard = System.IO.Path.Combine(rsxHeads, "Beards/", $"Beard {BeardModelNumber:00} {BeardColor}");

        var hairNumber = Gender == Gender.Male ? MaleHairModelNumber : FemaleHairModelNumber;
        var rsxHair = System.IO.Path.Combine(rsxHeads, "Hair/", $"Hair {gender} {hairNumber:00} {HairColor}");

        var baseModel = Resources.Load(rsxBaseModel);

        Base = Instantiate(baseModel, transform) as GameObject;

        if (Base)
        {
            var pelvis = Base.transform.Find("RigPelvis");

            EquipLKnee = pelvis.Find("RigLThigh/RigLCalf/+ L Knee");
            EquipRKnee = pelvis.Find("RigRThigh/RigRCalf/+ R Knee");

            var ribcage = pelvis.Find("RigSpine1/RigSpine2/RigSpine3/RigRibcage");
            EquipBack = ribcage.Find("+ Back");

            //if (this.Gender == Gender.Female)
            //{
            //    this.EquipNeck = ribcage.Find("RigNeck");
            //}
            //else
            //{
            EquipNeck = ribcage.Find("+ Neck");
            if (!EquipNeck)
                EquipNeck = ribcage.Find("RigNeck/+ Neck");
            //}

            var lupperarm = ribcage.Find("RigLCollarbone/RigLUpperarm");
            EquipLShoulder = ribcage.Find("RigLCollarbone/+ L Shoulder");
            EquipLWrist = lupperarm.Find("RigLForearm/+ L Wrist");
            EquipLHand = lupperarm.Find("RigLForearm/RigLPalm/+ L Hand");

            var rupperarm = ribcage.Find("RigRCollarbone/RigRUpperarm");
            EquipRShoulder = ribcage.Find("RigRCollarbone/+ R Shoulder");
            EquipRWrist = rupperarm.Find("RigRForearm/+ R Wrist");
            EquipRHand = rupperarm.Find("RigRForearm/RigRPalm/+ R Hand");

            var rhead = ribcage.Find("RigNeck/RigHead");
            EquipHead = rhead.Find("+ Head");
            EquipREar = rhead.Find("+ R Ear");
            EquipLEar = rhead.Find("+ L Ear");

            if (TorsoModelNumber > 0)
            {
                var torso = Math.Min(Gender == Gender.Male ? 18 : 21, TorsoModelNumber);
                //if (Deactivate("Torso"))
                Deactivate("Torso");
                (torsoModel = Base.transform.Find($"02 Torso {torso:00}"))?.gameObject.SetActive(true);

                if (TorsoMaterial > 0)
                {
                    var torsoMaterial = Resources.Load<Material>($"Materials/Armor/{TorsoMaterial}");
                    if (torsoMaterial && torsoModel)
                    {
                        torsoModel.GetComponent<SkinnedMeshRenderer>().material = torsoMaterial;
                    }
                }
            }

            if (BottomModelNumber > 0)
            {
                Deactivate("Bottom");
                (bottomModel = Base.transform.Find($"03 Bottom {BottomModelNumber:00}"))?.gameObject.SetActive(true);
                if (BottomMaterial > 0)
                {
                    var material = Resources.Load<Material>($"Materials/Armor/{BottomMaterial}");
                    if (material && bottomModel)
                    {
                        bottomModel.GetComponent<SkinnedMeshRenderer>().material = material;
                    }
                }
            }

            if (FeetModelNumber > 0)
            {
                Deactivate("Feet");
                (feetModel = Base.transform.Find($"04 Feet {FeetModelNumber:00}"))?.gameObject.SetActive(true);
                if (FeetMaterial > 0)
                {
                    var material = Resources.Load<Material>($"Materials/Armor/{FeetMaterial}");
                    if (material && feetModel)
                    {
                        feetModel.GetComponent<SkinnedMeshRenderer>().material = material;
                    }
                }
            }

            if (HandModelNumber > 0)
            {
                Deactivate("Hand");
                (handModel = Base.transform.Find($"05 Hand {HandModelNumber:00}"))?.gameObject.SetActive(true);
                if (HandMaterial > 0)
                {
                    var material = Resources.Load<Material>($"Materials/Armor/{HandMaterial}");
                    if (material && handModel)
                    {
                        handModel.GetComponent<SkinnedMeshRenderer>().material = material;
                    }
                }
            }

            if (BeltModelNumber > 0)
            {
                Deactivate("Belt");
                Base.transform.Find($"06 Belt {BeltModelNumber:00}")?.gameObject.SetActive(true);
            }

            if (Gender == Gender.Male)
            {
                if (BeardModelNumber > 0) CreateInstance(rsxBeard, EquipHead);
                if (MaleHairModelNumber > 0) hair = CreateInstance(rsxHair, EquipHead);
            }
            else if (FemaleHairModelNumber > 0) hair = CreateInstance(rsxHair, EquipHead);

            if (EyesModelNumber > 0) CreateInstance(rsxEyes, EquipHead);
            if (BrowsModelNumber > 0) CreateInstance(rsxBrows, EquipHead);
            if (MouthModelNumber > 0) CreateInstance(rsxMouth, EquipHead);
        }

        var animEventController = Base.GetComponent<AnimationEventController>();
        if (!animEventController) Base.AddComponent<AnimationEventController>();
    }

    public void SetAppearance(RavenNest.Models.Appearance def)
    {
        definition = def;
        var props = def.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            var value = prop.GetValue(def);
            var field = fields.FirstOrDefault(x => x.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) && x.FieldType == prop.PropertyType);
            if (field != null)
            {
                field.SetValue(this, value);
            }
        }

        UpdateAppearance();
    }

    private GameObject CreateInstance(string asset, Transform bone)
    {
        var model = Resources.Load(asset);
        if (!model)
        {
            Debug.LogError("Failed to load " + asset);
            return null;
        }
        return Instantiate(model, bone) as GameObject;
    }

    private bool Deactivate(string nameContains)
    {
        var child = FindActiveChild(Base.transform, nameContains);
        if (!child) return false;
        child.SetActive(false);
        return true;
    }

    private GameObject FindActiveChild(Transform parent, string nameContains)
    {
        for (var i = 0; i < parent.childCount; ++i)
        {
            var child = parent.GetChild(i);
            if (child.name.Contains(nameContains) && child.gameObject.activeInHierarchy)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    public bool AppearanceUpdated()
    {
        if (appearanceUpdateRequired)
        {
            appearanceUpdateRequired = false;
        }
        return appearanceUpdateRequired;
    }

    public bool TryUpdate(int[] appearance)
    {
        if (appearance == null || appearance.Length != 10) return false;

        var playerController = GetComponent<PlayerController>();
        if (!playerController) return false;

        var index = 0;
        Gender = (Gender)appearance[index++]; // 0

        if (Gender == Gender.Female)
            FemaleHairModelNumber = appearance[index++];
        else
            MaleHairModelNumber = appearance[index++]; // 5

        HairColor = (HairColor)appearance[index++]; // 1
        EyesModelNumber = appearance[index++]; // 2
        SkinColor = (SkinColor)appearance[index++]; // 1
        BeardModelNumber = appearance[index++]; // 1
        BeardColor = (HairColor)appearance[index++]; // 6
        BrowsModelNumber = appearance[index++]; // 3
        BrowColor = (HairColor)appearance[index++]; // 1
        MouthModelNumber = appearance[index]; // 4

        appearanceUpdateRequired = true;
        playerController.UpdateCharacterAppearance();

        return true;
    }

    public int[] ToAppearanceData()
    {
        return new int[]
        {
            (int)Gender,
            Gender == Gender.Female ? FemaleHairModelNumber : MaleHairModelNumber,
            (int)HairColor,
            EyesModelNumber,
            (int)SkinColor,
            BeardModelNumber,
            (int)BeardColor,
            BrowsModelNumber,
            (int)BrowColor,
            MouthModelNumber
        };
    }
    public void Equip(ItemController item)
    {
        throw new NotImplementedException();
    }

    public void Unequip(ItemController item)
    {
        throw new NotImplementedException();
    }

    public void SetAppearance(SyntyAppearance appearance, Action onReady)
    {
        throw new NotSupportedException();
    }

    public SyntyAppearance ToSyntyAppearanceData()
    {
        throw new NotSupportedException();
    }

    public void Optimize(Action afterUndo = null)
    {
        throw new NotImplementedException();
    }

    public Transform GetCombinedMesh()
    {
        throw new NotImplementedException();
    }

    public void SetMonsterMesh(GameObject prefab)
    {
        throw new NotImplementedException();
    }

    public void DestroyMonsterMesh()
    {
        throw new NotImplementedException();
    }
}