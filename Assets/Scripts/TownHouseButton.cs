using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TownHouseButton : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI buildingNameLabel;
    [SerializeField] private RawImage buildingIconBackground;
    [SerializeField] private RawImage buildingIcon;


    private TownHouse house;
    private TownHouseSelectionDialog dialog;

    // Start is called before the first frame update
    void Start()
    {
        if (!dialog) dialog = FindObjectOfType<TownHouseSelectionDialog>();
    }

    public void SetBuilding(TownHouse house, RenderTexture renderTexture)
    {
        this.house = house;
        buildingNameLabel.text = house.Name;
        buildingIcon.texture = renderTexture;
        buildingIconBackground.texture = renderTexture;
        SetOutline(false);
    }

    public void OnClick()
    {
        dialog.SelectedHouse = house;
        SetOutline(true);
    }

    public void SetOutline(bool enabled)
    {
        buildingIconBackground.gameObject.SetActive(enabled);
    }
}
