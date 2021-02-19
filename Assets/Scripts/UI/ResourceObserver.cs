using System;
using TMPro;
using UnityEngine;

public class ResourceObserver : MonoBehaviour
{
    private Func<decimal?> observedResource;

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
        var formattedValue = Utility.FormatValue(value);

        label.text = formattedValue;
    }

    public void Observe(Func<decimal?> amount)
    {
        observedResource = amount;
    }
}
