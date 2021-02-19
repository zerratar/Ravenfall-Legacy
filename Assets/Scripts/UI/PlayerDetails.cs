using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class PlayerDetails : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController observedPlayer;
    [SerializeField] private CombatSkillObserver combatSkillObserver;
    [SerializeField] private ClanSkillObserver clanSkillObserver;
    [SerializeField] private SkillStatsObserver skillStatsObserver;
    [SerializeField] private ResourcesObserver resourcesObserver;
    [SerializeField] private TextMeshProUGUI lblObserving;
    [SerializeField] private TextMeshProUGUI lblPlayername;
    [SerializeField] private TextMeshProUGUI lblPlayerlevel;
    [SerializeField] private TextMeshProUGUI lblClanName;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private AttributeStatsManager attributeStatsManager;
    [SerializeField] private EquipmentSlotManager equipmentSlotManager;

    [SerializeField] private GameObject subscriberBadge;
    [SerializeField] private GameObject vipBadge;
    [SerializeField] private GameObject modBadge;
    [SerializeField] private GameObject broadcasterBadge;
    [SerializeField] private GameObject devBadge;

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Dragscript dragscript;
    [SerializeField] private TooltipViewer playerDetailsTooltip;
    public PlayerController ObservedPlayer => observedPlayer;

    public static bool IsMoving { get; private set; }
    public static bool IsExpanded { get; private set; }

    private float observedPlayerTimeout;
    private RectTransform rectTransform;
    private bool visible = true;

    void Start()
    {
        if (!dragscript) dragscript = GetComponent<Dragscript>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        playerInventory.Hide();
    }

    public void ToggleVisibility()
    {
        this.visible = !this.visible;
        if (this.visible)
        {
            canvasGroup.alpha = 1;
        }
        else
        {
            canvasGroup.alpha = 0;
            playerDetailsTooltip.Hide(0);
        }
    }

    public void ToggleInventory()
    {
        IsExpanded = playerInventory.ToggleVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        if (!visible || !observedPlayer)
            return;

        if (canvasGroup.alpha != 1)
            canvasGroup.alpha = 1;

        IsMoving = dragscript.IsDragging;

        if (observedPlayer.Clan.InClan)
            SetText(lblClanName, "<" + observedPlayer.Clan.ClanInfo.Name + ">");
        else
            SetText(lblClanName, "");

        SetActive(subscriberBadge, observedPlayer.IsSubscriber);
        SetActive(vipBadge, observedPlayer.IsVip);
        SetActive(modBadge, observedPlayer.IsModerator);

        SetActive(devBadge, observedPlayer.IsGameAdmin);

        SetActive(broadcasterBadge, observedPlayer.IsBroadcaster);

        observedPlayerTimeout -= Time.deltaTime;
        SetText(lblObserving, $"({Mathf.FloorToInt(observedPlayerTimeout) + 1}s)");
        SetText(lblPlayerlevel, $"LV : <b>{observedPlayer.Stats.CombatLevel}");

        if (observedPlayer.CharacterIndex > 0)
            SetText(lblPlayername, $"{observedPlayer.PlayerName} #{observedPlayer.CharacterIndex}");
        else
            SetText(lblPlayername, $"{observedPlayer.PlayerName}");

        if (observedPlayerTimeout < 0)
            gameManager.Camera.ObserveNextPlayer();
    }

    public void Observe(PlayerController player, float timeout)
    {
        if (!player || player == null)
            playerDetailsTooltip.Disable();
        else
            playerDetailsTooltip.Enable();

        observedPlayer = player;

        ForceUpdate();

        this.gameObject.SetActive(player != null);
        observedPlayerTimeout = timeout;
    }

    public void ForceUpdate()
    {
        attributeStatsManager.Observe(observedPlayer);
        equipmentSlotManager.Observe(observedPlayer);
        clanSkillObserver.Observe(observedPlayer);
        resourcesObserver.Observe(observedPlayer);
        combatSkillObserver.Observe(observedPlayer);
        skillStatsObserver.Observe(observedPlayer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetText(TextMeshProUGUI obj, string value)
    {
        if (!obj) return;
        if (obj.text != value)
            obj.text = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetActive(GameObject obj, bool value)
    {
        if (!obj) return;
        if (obj.activeSelf != value)
            obj.SetActive(value);
    }

}
