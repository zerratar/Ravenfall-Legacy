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

    public string OwnerUserId { get; private set; }
    public PlayerController Owner { get; private set; }
    public TownHouseSlotType SlotType { get; set; }
    public int Slot { get; set; }
    public bool Selected { get; private set; }
    public float IconOffset => townHouseSource?.IconOffset ?? 0;

    // Start is called before the first frame update
    void Start()
    {
        if (!mud) mud = transform.Find("Mud").gameObject;
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        playerManager = FindObjectOfType<PlayerManager>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void SetHouse(VillageHouseInfo houseInfo, TownHouse townHouse)
    {
        var houseOwner = playerManager.GetPlayerByUserId(houseInfo.Owner);

        SetOwner(houseOwner);

        OwnerUserId = houseInfo.Owner;

        SlotType = (TownHouseSlotType)houseInfo.Type;
        Slot = houseInfo.Slot;
        townHouseSource = townHouse;
        if (House)
        {
            Destroy(House.gameObject);
            House = null;
            mud.SetActive(true);
        }

        if (!townHouse)
        {
            return;
        }

        var houseObject = Instantiate(townHouse.Prefab, this.transform);
        if (houseObject)
        {
            House = houseObject.GetComponent<TownHouseController>();
            House.TownHouse = townHouse;
            House.Owner = houseOwner;

            mud.SetActive(false);
        }
    }

    public void InvalidateOwner()
    {
        if (string.IsNullOrEmpty(OwnerUserId))
        {
            SetOwner(null);
            return;
        }

        var ownerId = OwnerUserId;
        var houseOwner = playerManager.GetPlayerByUserId(OwnerUserId);
        SetOwner(houseOwner);
        OwnerUserId = ownerId;
    }

    public void SetOwner(PlayerController player)
    {
        Owner = player;
        OwnerUserId = player?.UserId;

        if (!player)
        {

            gameManager.Village.SetBonus(Slot, SlotType, 0);
            return;
        }

        Debug.Log(player.Name + " was set as the new owner set for slot " + Slot);
        var existingSkill = GameMath.GetSkillByHouseType(player.Stats, SlotType);
        var bonus = GameMath.CalculateHouseExpBonus(existingSkill);

        gameManager.Village.SetBonus(Slot, SlotType, bonus);
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
