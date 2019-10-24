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
        player.Lock();
        transform.position = position;
        player.Island = islandManager.FindPlayerIsland(player);
    }
}
