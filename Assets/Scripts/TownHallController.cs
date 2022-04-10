using UnityEngine;

public class TownHallController : MonoBehaviour
{
    [SerializeField] private TownHallResource resources;
    [SerializeField] private TownHallManager manager;
    [SerializeField] private BoxCollider hitCollider;
    [SerializeField] private Transform infoPos;
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        if (!manager) manager = FindObjectOfType<TownHallManager>();
        if (!resources) resources = GetComponentInChildren<TownHallResource>();
        if (!hitCollider) hitCollider = GetComponent<BoxCollider>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (!gameManager || gameManager == null || manager == null || !manager)
            {
                return;
            }

            if (gameManager && (gameManager.RavenNest == null || !gameManager.RavenNest.Authenticated))
            {
                return;
            }

            if (hitCollider && Input.GetMouseButtonUp(0))
            {
                var activeCamera = Camera.main;
                if (!activeCamera || activeCamera == null)
                {
                    return;
                }

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
        catch (System.Exception exc)
        {
            Shinobytes.Debug.LogError(exc.ToString());
        }
    }
}
