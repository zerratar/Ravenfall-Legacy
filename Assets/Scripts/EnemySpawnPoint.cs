using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public DungeonRoomController Room;

    private void OnDrawGizmos()
    {
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }
}
