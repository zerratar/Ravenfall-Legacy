using UnityEngine;
using UnityEngine.Rendering;

public class LookAt : MonoBehaviour
{
    public bool ReverseX;

    public Transform Target;
    private Vector3 localScale;

    // Start is called before the first frame update
    void Start()
    {
        localScale = this.transform.localScale;

        if (ReverseX)
        {
            this.transform.localScale = new Vector3(localScale.x * -1, localScale.y, localScale.z);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (OnDemandRendering.renderFrameInterval > 2)
        {
            return;
        }

        if (!Target)
        {
            if (!Camera.main)
            {
                return;
            }
            if (!Target && Camera.main) Target = Camera.main.transform;
        }

        this.transform.rotation = Target.rotation;
    }
}
