using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    private RectTransform rectTransform;
    public bool ReverseX;

    public Transform Target;
    private Vector3 localScale;
    
    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (!Target) Target = Camera.main.transform;
        localScale = this.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target) return;
        if (ReverseX)
        {
            this.transform.localScale = new Vector3(localScale.x * -1, localScale.y, localScale.z);
        }
        if (Time.frameCount % 10 == 0)
        {
            rectTransform.LookAt(Target);
        }
    }
}
