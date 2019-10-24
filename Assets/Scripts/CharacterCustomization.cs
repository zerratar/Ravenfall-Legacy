using System;
using System.Runtime.CompilerServices;
using RavenNest.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomization : MonoBehaviour
{
    [SerializeField] private PlayerAppearance appearance;

    [SerializeField] private TextMeshProUGUI lblHairNumber;
    [SerializeField] private TextMeshProUGUI lblEyesNumber;
    [SerializeField] private TMP_InputField generatedCommand;

    [SerializeField] private Slider hairSlider;
    [SerializeField] private Slider hairColorSlider;
    [SerializeField] private Slider eyeSlider;
    [SerializeField] private Slider skinColorSlider;
    [SerializeField] private Slider beardSlider;
    [SerializeField] private Slider beardColorSlider;
    [SerializeField] private Slider browSlider;
    [SerializeField] private Slider browColorSlider;
    [SerializeField] private Slider mouthSlider;



    private bool requiresUpdate = false;

    private void Start()
    {
        if (!appearance)
        {
            appearance = appearance.GetComponent<PlayerAppearance>();
        }
    }

    private void UpdateSliderMax()
    {
        hairColorSlider.maxValue = Enum.GetValues(typeof(HairColor)).Length - 1;
        beardColorSlider.maxValue = Enum.GetValues(typeof(HairColor)).Length - 1;
        browColorSlider.maxValue = Enum.GetValues(typeof(HairColor)).Length - 1;
        skinColorSlider.maxValue = Enum.GetValues(typeof(SkinColor)).Length - 1;
        mouthSlider.maxValue = 11;
        eyeSlider.maxValue = 7;
        browSlider.maxValue = 15;

        if (appearance)
        {
            var female = appearance.Gender == Gender.Female;
            if (female) beardColorSlider.maxValue = 0;
            hairSlider.maxValue = female ? 20 : 10;
            beardSlider.maxValue = female ? 0 : 10;
        }
    }

    private void Update()
    {
        UpdateSliderMax();

        if (appearance && requiresUpdate)
        {
            appearance.UpdateAppearance();
            requiresUpdate = false;
        }

        if (appearance && generatedCommand)
        {
            var text = "!appearance ";
            text += (int)appearance.Gender + ",";
            text += Mathf.FloorToInt(hairSlider.value) + ",";
            text += Mathf.FloorToInt(hairColorSlider.value) + ",";
            text += Mathf.FloorToInt(eyeSlider.value) + ",";
            text += Mathf.FloorToInt(skinColorSlider.value) + ",";
            text += Mathf.FloorToInt(beardSlider.value) + ",";
            text += Mathf.FloorToInt(beardColorSlider.value) + ",";
            text += Mathf.FloorToInt(browSlider.value) + ",";
            text += Mathf.FloorToInt(browColorSlider.value) + ",";
            text += Mathf.FloorToInt(mouthSlider.value);
            generatedCommand.text = text;
        }
    }

    public void ChangeToMale()
    {
        appearance.Gender = Gender.Male;
        requiresUpdate = true;
    }

    public void ChangeToFemale()
    {
        appearance.Gender = Gender.Female;
        requiresUpdate = true;
    }

    public void UpdateHair(float value)
    {
        var i = Mathf.FloorToInt(hairSlider.value);

        if (appearance.Gender == Gender.Male)
            appearance.MaleHairModelNumber = i;

        if (appearance.Gender == Gender.Female)
            appearance.FemaleHairModelNumber = i;

        requiresUpdate = true;
    }

    public void UpdateHairColor(float value)
    {
        var i = Mathf.FloorToInt(hairColorSlider.value);
        appearance.HairColor = (HairColor)i;
        requiresUpdate = true;
    }

    public void UpdateEyes(float value)
    {
        var i = Mathf.FloorToInt(eyeSlider.value);
        appearance.EyesModelNumber = i;
        requiresUpdate = true;
    }

    public void UpdateSkinColor(float value)
    {
        var i = Mathf.FloorToInt(skinColorSlider.value);
        appearance.SkinColor = (SkinColor)i;
        requiresUpdate = true;
    }
    public void UpdateBeard(float value)
    {
        var i = Mathf.FloorToInt(beardSlider.value);
        appearance.BeardModelNumber = i;
        requiresUpdate = true;
    }
    public void UpdateBeardColor(float value)
    {
        var i = Mathf.FloorToInt(beardColorSlider.value);
        appearance.BeardColor = (HairColor)i;
        requiresUpdate = true;
    }
    public void UpdateEyebrows(float value)
    {
        var i = Mathf.FloorToInt(browSlider.value);
        appearance.BrowsModelNumber = i;
        requiresUpdate = true;
    }
    public void UpdateEyebrowColor(float value)
    {
        var i = Mathf.FloorToInt(browColorSlider.value);
        appearance.BrowColor = (HairColor)i;
        requiresUpdate = true;
    }
    public void UpdateMouth(float value)
    {
        var i = Mathf.FloorToInt(mouthSlider.value);
        appearance.MouthModelNumber = i;
        requiresUpdate = true;
    }
}