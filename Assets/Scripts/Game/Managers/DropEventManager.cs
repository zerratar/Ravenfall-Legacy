using RavenNest.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DropEventManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float randomDistance = 1.5f;

    private readonly List<ItemController> droppedItems = new List<ItemController>();
    private readonly object mutex = new object();

    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
    }

    public bool Contains(ItemController item)
    {
        lock (mutex) return droppedItems.FirstOrDefault(x => x.GetInstanceID() == item.GetInstanceID());
    }

    public IReadOnlyList<ItemController> GetDropItems()
    {
        lock (mutex) return droppedItems.ToList();
    }

    public void RemoveDropItem(ItemController item)
    {
        lock (mutex)
        {
            if (!droppedItems.Remove(item)) return;

            if (droppedItems.Count == 0)
            {
                IsActive = false;
                foreach (var player in gameManager.Players.GetAllPlayers())
                {
                    player.EndItemDropEvent();
                }
            }
        }
    }

    public void Drop(Item item, int amount)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Shinobytes.Debug.LogError("Uh oh, unable to spawn items!! No spawn points have been configured!");
            return;
        }

        IsActive = true;

        lock (mutex)
        {
            for (var i = 0; i < amount; ++i)
            {
                var pos = spawnPoints[Random.Range(0, spawnPoints.Length)];
                ItemController controller = gameManager.Items.Create(item, true);
                controller.transform.SetParent(pos);
                controller.transform.position = new Vector3(randomDistance, 0, randomDistance) * 2f * UnityEngine.Random.value - new Vector3(randomDistance, 0, randomDistance);
                controller.EnablePickup(this);

                droppedItems.Add(controller);
            }
        }

        foreach (var player in gameManager.Players.GetAllPlayers())
        {
            player.BeginItemDropEvent();
        }

        gameManager.RavenBot.Announce(
            "Great news everyone! {amount}x {itemName} was dropped in the world!",
            amount.ToString(),
            item.Name);
    }
}
