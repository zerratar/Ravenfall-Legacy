using UnityEngine;

public class TeleportHandler : MonoBehaviour
{
    [SerializeField] private IslandManager islandManager;

    private PlayerController player;
    private Transform parent;

    private void Start()
    {
        if (!islandManager) islandManager = FindObjectOfType<IslandManager>();
        player = GetComponent<PlayerController>();
    }

    public void Teleport(Vector3 position)
    {
        // check if player has been removed
        // could have been kicked. *Looks at Solunae*
        if (!this || this == null)
            return;

        var hasParent = !!player.transform.parent;
        if (hasParent)
        {
            parent = player.Transform.parent;
            player.transform.SetParent(null);
        }

        player.Lock();

        if (!hasParent && parent)
        {
            player.transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }

        player.Island = islandManager.FindPlayerIsland(player);
    }
}
