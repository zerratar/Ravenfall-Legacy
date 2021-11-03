using UnityEngine;

public class AttributeStats : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lblValue;

    public string Text
    {
        get
        {
            return lblValue.text;
        }
        set
        {
            lblValue.text = value;
        }
    }
}
