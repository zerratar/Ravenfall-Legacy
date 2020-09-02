using UnityEngine;

public class TownHallController : MonoBehaviour
{
    [SerializeField] private TownHallResource resources;
    [SerializeField] private TownHallManager manager;
    [SerializeField] private BoxCollider hitCollider;
    [SerializeField] private Transform infoPos;

    // Start is called before the first frame update
    void Start()
    {
        if (!manager) manager = FindObjectOfType<TownHallManager>();
        if (!resources) resources = GetComponentInChildren<TownHallResource>();
        if (!hitCollider) hitCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0) && hitCollider)
        {
            var activeCamera = Camera.main;
            var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            var result = Physics.RaycastAll(ray);
            foreach (var res in result)
            {
                if (res.collider.GetInstanceID() == hitCollider.GetInstanceID())
                {
                    manager.OpenVillageDialog();
                    return;
                }
            }
        }
    }
}
