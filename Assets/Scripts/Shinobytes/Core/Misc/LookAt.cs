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
        localScale = this.transform.localScale;

        if (ReverseX)
        {
            this.transform.localScale = new Vector3(localScale.x * -1, localScale.y, localScale.z);
        }

        if (!Camera.main)
        {
            return;
        }
        if (!Target && Camera.main) Target = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Target && Camera.main) Target = Camera.main.transform;
        if (!Target) return;
        //this.transform.rotation = Target.rotation;
        //return;

        this.transform.rotation = Target.rotation;
    }
}
