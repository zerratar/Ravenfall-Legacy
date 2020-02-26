using UnityEngine;

public class TownHouseSelectionDialog : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TownHouseManager townHouseManager;
    [SerializeField] private RectTransform buildingScrollViewContent;    

    private TownHouse selectedHouse;
    public TownHouse SelectedHouse => selectedHouse;

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
        if (!townHouseManager) townHouseManager = gameManager.Village.TownHouses;

        GenerateTownHouseButtons();
    }

    private void GenerateTownHouseButtons()
    {
        foreach (var townHouse in townHouseManager.TownHouses)
        {
            // add house buttons to content
            // buildingScrollViewContent
        }
    }    
}