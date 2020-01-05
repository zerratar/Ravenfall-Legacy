using UnityEngine;

public class TeleportHandler : MonoBehaviour
{
    [SerializeField] private IslandManager islandManager;

    private bool hasTeleported;
    private PlayerController player;
    private IChunk lastChunk;

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

        player.Lock();
        transform.position = position;
        player.Island = islandManager.FindPlayerIsland(player);
    }
}
