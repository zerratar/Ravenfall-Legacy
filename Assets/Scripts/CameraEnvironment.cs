using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEnvironment : MonoBehaviour
{

    [SerializeField] private GameCamera gameCamera;

    private Vector3 cameraOffset;

    // Start is called before the first frame update
    void Start()
    {
        cameraOffset = transform.position - gameCamera.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        var pos = gameCamera.transform.position + cameraOffset;
        transform.position = new Vector3(pos.x, 0, pos.z);
    }
}
