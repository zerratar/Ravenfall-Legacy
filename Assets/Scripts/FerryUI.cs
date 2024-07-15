using UnityEngine;

public class FerryUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        this.gameObject.SetActive(false);
    }


    public void ShowDialog()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
