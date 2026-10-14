using RavenNest.Models;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class TownHouseSlot : MonoBehaviour
{
    [SerializeField] private GameObject mud;
    [SerializeField] private GameManager gameManager;

    private PlayerManager playerManager;
    private MeshRenderer meshRenderer;
    private TownHouse townHouseSource;

    public TownHouseController House;
    public Skills PlayerSkills { get; private set; }
    public string PlayerName { get; private set; }
    public string OwnerPlatformId { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public PlayerController Player { get; private set; }
    public TownHouseSlotType SlotType { get; set; }
    public int Slot { get; set; }
    public bool Selected { get; private set; }
    public float IconOffset => townHouseSource?.IconOffset ?? 0;

    private bool defaultsCaptured;
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;

    /// <summary>True when build mode has moved the plot away from its original scene position.</summary>
    public bool IsMoved { get; private set; }

    /// <summary>
    /// Moved off its original position because something else stands there, for this session only:
    /// not a placement the server has. See <see cref="VillageBuildMode.SettlePlots"/>.
    /// </summary>
    public bool IsNudged { get; private set; }

    /// <summary>The plot's original position in the scene, before any build mode move.</summary>
    public Vector3 DefaultPosition
    {
        get
        {
            CaptureDefaults();
            return transform.parent ? transform.parent.TransformPoint(defaultLocalPosition) : defaultLocalPosition;
        }
    }

    /// <summary>
    /// Remembers where the scene put this plot, so a moved plot can always go back. Done lazily:
    /// plots start inactive, so Awake does not run for them until the village has enough levels.
    /// </summary>
    private void CaptureDefaults()
    {
        if (defaultsCaptured)
        {
            return;
        }

        defaultLocalPosition = transform.localPosition;
        defaultLocalRotation = transform.localRotation;
        defaultsCaptured = true;
    }

    /// <summary>Moves the plot, and whatever is built on it, to a position on the ground.</summary>
    public void ApplyPlacement(float x, float z, float rotation)
    {
        CaptureDefaults();
        transform.position = PlacementUtility.FindGroundPoint(new Vector3(x, transform.position.y, z));
        transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        IsMoved = true;
        IsNudged = false;
    }

    /// <summary>
    /// Moves a plot whose original spot is taken to a free one nearby, without making it a
    /// placement: it is not "moved", and goes back to its original spot when that is free again.
    /// </summary>
    public void Nudge(float x, float z, float rotation)
    {
        CaptureDefaults();
        transform.position = PlacementUtility.FindGroundPoint(new Vector3(x, transform.position.y, z));
        transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        IsNudged = true;
    }

    /// <summary>Puts the plot back where the scene had it.</summary>
    public void ResetPlacement()
    {
        CaptureDefaults();
        transform.localPosition = defaultLocalPosition;
        transform.localRotation = defaultLocalRotation;
        IsMoved = false;
        IsNudged = false;
    }


    [Button("Adjust Placement")]
    public void AdjustPlacement()
    {
        PlacementUtility.PlaceOnGround(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!mud) mud = transform.Find("Mud").gameObject;
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!playerManager) playerManager = FindAnyObjectByType<PlayerManager>();
        if (!meshRenderer) meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void SetHouse(VillageHouseInfo houseInfo, TownHouse townHouse, bool updateExpBonus = true)
    {
        if (SlotType == (TownHouseSlotType)houseInfo.Type && OwnerUserId == houseInfo.OwnerUserId && updateExpBonus)
        {
            UpdateExpBonus();
            return;
        }

        OwnerUserId = houseInfo.OwnerUserId;
        Player = houseInfo.OwnerUserId != null ? playerManager.GetPlayerByUserId(houseInfo.OwnerUserId.Value) : null;

        if (Player)
        {
            PlayerSkills = Player.Stats;
            PlayerName = Player.Name;
        }

        SlotType = (TownHouseSlotType)houseInfo.Type;
        Slot = houseInfo.Slot;
        townHouseSource = townHouse;

        try
        {
            // if we have a hut,
            // check if the hut changed. If it did, we want to destroy and instantiate the new model.
            if (House)
            {
                House.Slot = this;

                if (townHouse != null && House.TownHouse.Name == townHouse.Name)
                {
                    return;
                }

                Destroy(House.gameObject);
                House = null;
                mud.SetActive(true);
            }

            // if the townhouse was null, or the type is now empty. Return out
            if (!townHouse || SlotType == TownHouseSlotType.Empty)
            {
                return;
            }

            var houseObject = Instantiate(townHouse.Prefab, this.transform);
            if (houseObject)
            {
                House = houseObject.GetComponent<TownHouseController>();
                House.TownHouse = townHouse;
                House.Slot = this;
                mud.SetActive(false);
            }
        }
        finally
        {
            UpdateExpBonus(updateExpBonus);
        }
    }

    public void UpdateExpBonus(bool notify = true)
    {
        float bonus = 0;
        try
        {
            var playerSkills = this.PlayerSkills;
            if (Player != null && Player)
            {
                playerSkills = Player.Stats;
            }

            if (playerSkills != null)
            {
                var existingSkill = GameMath.GetSkillByHouseType(playerSkills, SlotType);
                bonus = GameMath.CalculateHouseExpBonus(existingSkill);
                if (notify)
                {
                    gameManager?.Village?.SetBonus(Slot, SlotType, bonus);
                }
                else
                {
                    gameManager?.Village?.SetBonusWithoutNotify(Slot, SlotType, bonus);
                }
                return;
            }

            if (notify)
            {
                gameManager?.Village?.SetBonus(Slot, SlotType, 0);
            }
            else
            {
                gameManager?.Village?.SetBonusWithoutNotify(Slot, SlotType, 0);
            }

        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("TownHouseSlot.UpdateExpBonus: " + exc);
        }
    }

    public void InvalidateOwner(bool assignNewOwnerIfEmpty)
    {
        Player = OwnerUserId != null ? playerManager.GetPlayerByUserId(OwnerUserId.Value) : null;
        if (Player)
        {
            PlayerSkills = Player.Stats;
            PlayerName = Player.Name;
        }
        else
        {
            PlayerSkills = null;
        }

        if (OwnerUserId == null)
        {
            PlayerSkills = null;
            PlayerName = null;
        }

        if (assignNewOwnerIfEmpty && PlayerSkills == null)
        {
            var players = playerManager.GetAllRealPlayers();
            PlayerController targetPlayer = null;
            int highestSkill = 0;
            foreach (var plr in players)
            {
                var hasSlotAlready = gameManager.Village.TownHouses.FindSlotByOwner(plr);
                if (hasSlotAlready)
                {
                    continue;
                }

                var houseSkill = GameMath.GetSkillByHouseType(plr.Stats, SlotType);
                if (highestSkill < houseSkill.MaxLevel)
                {
                    highestSkill = houseSkill.MaxLevel;
                    targetPlayer = plr;
                }
            }

            if (targetPlayer)
            {
                OwnerUserId = targetPlayer.UserId;
                PlayerSkills = targetPlayer.Stats;
                PlayerName = targetPlayer.Name;
            }
        }

        UpdateExpBonus();
    }

    internal void SetOwner(Guid? userId)
    {
        this.OwnerUserId = userId;
        InvalidateOwner(PlayerSettings.Instance.AutoAssignVacantHouses.GetValueOrDefault());
    }

    internal void RemoveOwner()
    {
        this.OwnerUserId = null;
        InvalidateOwner(false);
    }

    public void Deselect(Material defaultMaterial)
    {
        Selected = false;
        meshRenderer.material = defaultMaterial;
    }

    public void Select(Material selectedMaterial)
    {
        Selected = true;
        meshRenderer.material = selectedMaterial;
    }
}
