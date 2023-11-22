using System;
using UnityEngine;
using UnityEngine.Rendering;

public class LookAt : MonoBehaviour
{
    public bool ReverseX;

    public Transform Target;
    private Transform _transform;
    private Vector3 localScale;
    private bool hasTarget;


    public static bool HasGameCameraRotation;
    public static Quaternion GameCameraRotation;
    private bool useCameraRotation;

    private void Awake()
    {
        this._transform = this.transform;
    }

    // Start is called before the first frame update
    void Start()
    {
        localScale = this._transform.localScale;

        TryGetTarget();

        if (ReverseX)
        {
            this._transform.localScale = new Vector3(localScale.x * -1, localScale.y, localScale.z);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!GraphicsToggler.GraphicsEnabled)
        {
            return;
        }

        if (useCameraRotation)
        {
            this._transform.rotation = GameCameraRotation;
            return;
        }

        if (!hasTarget)
        {
            TryGetTarget();
            return;
        }

        if (!hasTarget && HasGameCameraRotation)
        {
            useCameraRotation = true;
        }

        this._transform.rotation = Target.rotation;
    }

    private void TryGetTarget()
    {
        if (!hasTarget && Target == null)
        {
            if (Camera.main)
            {
                //Target = Camera.main.transform;
                useCameraRotation = HasGameCameraRotation;
            }

            hasTarget = Target != null;
        }
    }
}
