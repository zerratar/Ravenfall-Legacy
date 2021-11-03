using UnityEngine;

[CreateAssetMenu(fileName = "New Item Drop list", menuName = "Game/Items")]
public class ItemDropList : ScriptableObject
{
    public ItemDrop[] Items;
}