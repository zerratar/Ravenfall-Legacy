using SqlParser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI clanName;
    [SerializeField] private Image logo;

    // the title the player wears (!title), on a line of its own above the name, made from the clan line
    private TextMeshProUGUI titleLabel;
    private int titleVersion = -1;
    private PlayerController titleOwner;
    private static readonly Color TitleColor = new Color(1f, 0.78f, 0.36f);

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

    // faded out by NameTagManager while it would cover a tag nearer the camera
    private CanvasGroup group;
    internal float Alpha = 1f;

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
        if (titleVersion != QuestTitles.Version || titleOwner != TargetPlayer) UpdateTitle();
    }

    /// <summary>Shows or hides the worn title. The line is a copy of the clan line, as far above the name as that is below it.</summary>
    private void UpdateTitle()
    {
        titleVersion = QuestTitles.Version;
        titleOwner = TargetPlayer;
        if (!TargetPlayer || !clanName || !label) return;
        var text = QuestTitles.TextFor(TargetPlayer.Id);
        if (string.IsNullOrEmpty(text))
        {
            if (titleLabel) titleLabel.gameObject.SetActive(false);
            return;
        }

        if (!titleLabel)
        {
            var go = Instantiate(clanName.gameObject, clanName.transform.parent);
            go.name = "Title";
            titleLabel = go.GetComponent<TextMeshProUGUI>();
            var nameRect = label.rectTransform;
            var clanRect = clanName.rectTransform;
            titleLabel.rectTransform.anchoredPosition = nameRect.anchoredPosition + (nameRect.anchoredPosition - clanRect.anchoredPosition);
            titleLabel.color = TitleColor;
            titleLabel.fontStyle = FontStyles.Italic;
        }

        titleLabel.text = text;
        titleLabel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Where the tag's text is on screen, the name with its clan and title lines. False while it has
    /// no text yet or is behind the camera.
    /// </summary>
    internal bool TryGetScreenRect(Camera camera, out Rect rect)
    {
        rect = default;
        var any = false;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        void Add(TextMeshProUGUI text)
        {
            if (!text || !text.gameObject.activeSelf || string.IsNullOrEmpty(text.text)) return;
            var b = text.textBounds;
            if (b.size.x <= 0f) return;
            var t = text.rectTransform;
            var a = camera.WorldToScreenPoint(t.TransformPoint(b.min));
            var c = camera.WorldToScreenPoint(t.TransformPoint(b.max));
            if (a.z <= 0f || c.z <= 0f) return;
            xMin = Mathf.Min(xMin, a.x, c.x); xMax = Mathf.Max(xMax, a.x, c.x);
            yMin = Mathf.Min(yMin, a.y, c.y); yMax = Mathf.Max(yMax, a.y, c.y);
            any = true;
        }
        Add(label);
        Add(clanName);
        Add(titleLabel);
        if (!any) return false;
        rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return true;
    }

    /// <summary>Eases the tag toward shown (1) or hidden (0); a fifth of a second either way.</summary>
    internal void FadeTo(float target, float deltaTime)
    {
        if (Alpha == target && group) return;
        if (!group)
        {
            group = GetComponent<CanvasGroup>();
            if (!group) group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        Alpha = Mathf.MoveTowards(Alpha, target, deltaTime * 5f);
        group.alpha = Alpha;
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
