using System;
using TMPro;
using UnityEngine;

public class ResourceObserver : MonoBehaviour
{
    private Func<double?> observedResource;

    [SerializeField] private TextMeshProUGUI label;

    // Start is called before the first frame update
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (observedResource == null || !label)
        {
            return;
        }

        var value = observedResource().GetValueOrDefault();
        var formattedValue = Utility.FormatAmount(value);

        label.text = formattedValue;
    }

    public void Observe(Func<double?> amount)
    {
        observedResource = amount;
    }
}
