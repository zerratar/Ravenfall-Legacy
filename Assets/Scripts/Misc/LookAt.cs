using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    private RectTransform rectTransform;

    public Transform Target;
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (!Target) Target = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;

        if (Time.frameCount % 10 == 0)
        {
            rectTransform.LookAt(Target);
        }
    }
}
