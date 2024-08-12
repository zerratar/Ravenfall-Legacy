using UnityEngine;

public class OceanController : MonoBehaviour
{
    [SerializeField] private GameCamera cameraFollow;

    private Vector3 defaultPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.defaultPosition = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!cameraFollow) return;
        if (cameraFollow.State != GameCameraType.Free)
        {
            this.transform.position = defaultPosition;
            return;
        }

        var camPosition = cameraFollow.transform.position;
        this.transform.position = new Vector3(camPosition.x + defaultPosition.x, defaultPosition.y, camPosition.z + defaultPosition.z);
    }
}
