using SqlParser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI clanName;
    [SerializeField] private Image logo;

    private bool hasInitialized;
    private bool nameTagSet;
    private GameManager gameManager;
    private float originalFontSize;
    //private Transform targetCamera;
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
    public bool HasTargetPlayer;

    private bool isVisible = true;
    private Vector3 offset;
    private Transform _transform;

    private void Awake()
    {
        this._transform = transform;
    }

    void Start()
    {
        if (!label) label = GetComponentInChildren<TextMeshProUGUI>();
        if (!logo) logo = GetComponentInChildren<Image>();
        gameManager = FindAnyObjectByType<GameManager>();
        originalFontSize = label.fontSize;

        // if graphics are disabled, main camera does not exist.

        if (!GraphicsToggler.GraphicsEnabled)
            return;

        //if (Camera.main)
        //    targetCamera = Camera.main.transform;
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }

    private void OnBecameVisible()
    {
        isVisible = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!GraphicsToggler.GraphicsEnabled)
            return;

        if (!isVisible || !HasTargetPlayer || !Init())
        {
            return;
        }

        //if (!targetCamera)
        //{
        //    var mainCamera = Camera.main;
        //    if (!mainCamera)
        //        return;

        //    targetCamera = mainCamera.transform;
        //}

        //+ (Vector3.up * TargetTransform.localScale.y * YOffset * Scale)
        //+ (Vector3.up * YMinDistance * Scale);

        //this.transform.rotation = GameCamera.Rotation;
        _transform.SetPositionAndRotation(TargetPlayer.Position + this.offset, GameCamera.Rotation); //targetCamera.rotation);
        oldCensor = gameManager.LogoCensor;
    }

    private bool Init()
    {
        if (hasInitialized)
        {
            return true;
        }
        _transform = this.transform;
        if (!nameTagSet || oldScale != Scale || string.IsNullOrEmpty(label.text))
        {
            SetupNameTagLabel();
            return false;
        }

        Refresh();

        hasInitialized = true;
        return true;
    }

    public void Refresh()
    {
        //this.transform.SetParent(TargetTransform, true);
        //this.transform.localPosition = this.offset;
        //this.transform.rotation = GameCamera.Rotation;

        if (!TargetPlayer)
        {
            return;
        }

        if (clanName && TargetPlayer.clanHandler.InClan && lastSetClanName != TargetPlayer.clanHandler.ClanInfo.Name)
        {
            clanName.text = "<" + TargetPlayer.clanHandler.ClanInfo.Name + ">";
            lastSetClanName = TargetPlayer.clanHandler.ClanInfo.Name;
        }
        else if (!TargetPlayer.clanHandler.InClan && !string.IsNullOrEmpty(lastSetClanName))
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

    private void SetupNameTagLabel()
    {
        nameTagSet = true;
        var collider = TargetTransform.GetComponent<CapsuleCollider>();
        if (collider)
            YOffset = collider.height;

        this.offset = (Vector3.up * TargetTransform.localScale.y * YOffset * Scale) + (Vector3.up * YMinDistance * Scale);

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
        {
            var c = color;

            if (c.r <= 0.15f && c.g <= 0.15f && c.b <= 0.15f)
            {
                return new Color(0.15f, 0.15f, 0.15f);
            }

            return c;
        }
        return Color.white;
    }
}
