using UnityEngine;

public class TownHouseSlot : MonoBehaviour
{
    [SerializeField] private GameObject mud;

    private PlayerManager playerManager;
    private MeshRenderer meshRenderer;



    public TownHouseController House;

    public PlayerController Owner { get; private set; }
    public int SlotType { get; set; }
    public bool Selected { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        if (!mud) mud = transform.Find("Mud").gameObject;

        playerManager = FindObjectOfType<PlayerManager>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void SetHouse(VillageHouseInfo houseInfo, TownHouse townHouse)
    {
        Owner = playerManager.GetPlayerByUserId(houseInfo.Owner);
        SlotType = houseInfo.Type;

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
            House.Owner = Owner;
            mud.SetActive(false);
        }
    }

    internal void Deselect(Material defaultMaterial)
    {
        Selected = false;
        meshRenderer.material = defaultMaterial;
    }

    internal void Select(Material selectedMaterial)
    {
        Selected = true;
        meshRenderer.material = selectedMaterial;
    }
}
