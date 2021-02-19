using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI clanName;
    [SerializeField] private Image logo;

    private bool nameTagSet;
    private GameManager gameManager;
    private float originalFontSize;
    private bool oldCensor;
    private string lastSetClanName;
    private float oldScale = 1f;

    public float YMinDistance = 0.45f;
    public float YOffset = 2.75f;
    public float Scale = 1f;

    public Transform TargetTransform;
    public PlayerController TargetPlayer;
    public Sprite RaiderLogo;
    public NameTagManager Manager { get; set; }


    void Start()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!logo) logo = GetComponentInChildren<Image>();
        gameManager = FindObjectOfType<GameManager>();
        originalFontSize = label.fontSize;
    }

    // Update is called once per frame
    void Update()
    {
        if (!TargetTransform)
        {
            return;
        }

        if (string.IsNullOrEmpty(label.text) || !nameTagSet || oldScale != Scale)
        {
            SetupNameTagLabel();
        }

        if (TargetPlayer)
        {
            if (clanName && TargetPlayer.Clan.InClan && lastSetClanName != TargetPlayer.Clan.ClanInfo.Name)
            {
                clanName.text = "<" + TargetPlayer.Clan.ClanInfo.Name + ">";
                lastSetClanName = TargetPlayer.Clan.ClanInfo.Name;
            }
            else if (!TargetPlayer.Clan.InClan && !string.IsNullOrEmpty(lastSetClanName))
            {
                clanName.text = "";
                lastSetClanName = "";
            }
            if (TargetPlayer.Raider != null && (RaiderLogo == null || !logo.gameObject.activeSelf || oldCensor != gameManager.LogoCensor))
            {
                SetupRaiderTag();
            }
            else if (!RaiderLogo && logo && logo.gameObject.activeSelf)
            {
                logo.gameObject.SetActive(false);
            }
        }
        else
        {
            logo.gameObject.SetActive(false);
        }

        transform.position = TargetTransform.position
            + (Vector3.up * TargetTransform.localScale.y * YOffset * Scale)
            + (Vector3.up * YMinDistance * Scale);

        oldCensor = gameManager.LogoCensor;
    }

    private void SetupNameTagLabel()
    {
        nameTagSet = true;
        var collider = TargetTransform.GetComponent<CapsuleCollider>();
        if (collider)
            YOffset = collider.height;

        if (TargetPlayer)
        {
            label.color = GetColorFromHex(TargetPlayer.PlayerNameHexColor);
            label.text = TargetPlayer.PlayerName;
        }
        else
        {
            label.text = TargetTransform.name;
        }

        if (Scale != oldScale)
        {
            label.fontSize = originalFontSize * Scale;
            oldScale = Scale;
        }
    }

    private void SetupRaiderTag()
    {
        RaiderLogo = Manager.LogoManager.GetLogo(TargetPlayer.Raider.RaiderUserId);
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
    }
}
