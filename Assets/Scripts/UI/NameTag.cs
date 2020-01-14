using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image logo;

    public float YOffset = 2.75f;
    public PlayerController Target;
    public Sprite RaiderLogo;
    private bool nameTagSet;
    private GameManager gameManager;
    private bool oldCensor;

    public NameTagManager Manager { get; set; }

    void Start()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!logo) logo = GetComponentInChildren<Image>();
        gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {

        if (!Target)
        {
            return;
        }

        if (string.IsNullOrEmpty(label.text) || !nameTagSet)
        {
            SetupNameTagLabel();
        }

        if (Target.Raider != null && (RaiderLogo == null || !logo.gameObject.activeSelf || oldCensor != gameManager.LogoCensor))
        {
            SetupRaiderTag();
        }
        else if (!RaiderLogo && logo && logo.gameObject.activeSelf)
        {
            logo.gameObject.SetActive(false);
        }

        transform.position = Target.transform.position + (Vector3.up * YOffset);
        oldCensor = gameManager.LogoCensor;
    }

    private void SetupNameTagLabel()
    {
        nameTagSet = true;
        label.color = GetColorFromHex(Target.PlayerNameHexColor);
        label.text = Target.PlayerName;
    }

    private void SetupRaiderTag()
    {
        RaiderLogo = Manager.LogoManager.GetLogo(Target.Raider.RaiderUserId);
        if (logo && RaiderLogo)
        {
            logo.gameObject.SetActive(true);
            logo.sprite = RaiderLogo;
        }
    }

    public static Color GetColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;

        return Color.white;

        //if (string.IsNullOrEmpty(hex)) return Color.white;
        //if (hex.StartsWith("#")) hex = hex.Substring(1);
        //if (hex.Length == 6)
        //{
        //    var r = int.Parse(hex[0] + "" + hex[1], NumberStyles.HexNumber);
        //    var g = int.Parse(hex[2] + "" + hex[3], NumberStyles.HexNumber);
        //    var b = int.Parse(hex[4] + "" + hex[5], NumberStyles.HexNumber);
        //    return new Color(r / 255f, g / 255f, b / 255f);
        //}

        //return new Color();
    }
}
