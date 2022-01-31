using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RavenNest.Models;
using System.Text;

public class ItemDropHandler : MonoBehaviour
{
    [SerializeField] private ItemDropList dropList;

    [SerializeField] private ItemDrop[] items;
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public void SetDropList(ItemDropList droplist)
    {
        dropList = droplist;
        items = dropList.Items;
    }

    internal void DropItemForPlayers(List<PlayerController> joinedPlayers, DropType dropType = DropType.Standard, int itemRewardCount = 1)
    {
        if (items == null || items.Length == 0)
            return;

        Dictionary<Item, List<string>> playerListByItem = new Dictionary<Item, List<string>>(); //List<PlayerController> if more details needed
        List<string> playerList;

        foreach (PlayerController player in joinedPlayers)
        {
            if (player != null) continue;

            for (var i = 0; i < itemRewardCount; ++i)
            {
                var itemRecieved = DropItem(player, dropType);
                if (itemRecieved == null)
                    continue;

                bool? EquipIfBetter = player.PickupItem(itemRecieved);
                if (EquipIfBetter == null)
                    continue;

                string playerName = player.Name;
                if (EquipIfBetter ?? false)
                    playerName += "*"; //* denote item was equipped

                if (playerListByItem.TryGetValue(itemRecieved, out playerList)) //add to list of item with a player, otherwise add player to existing item
                {

                    playerList.Add(playerName);
                }
                else
                {
                    playerList = new List<string>();
                    playerList.Add(playerName);
                    playerListByItem.Add(itemRecieved, playerList);
                }
            }
        }

        AlertPlayers(playerListByItem);
    }

    public Item DropItem(PlayerController player,
        DropType dropType = DropType.Standard)
    {
        var guaranteedDrop = dropType == DropType.MagicRewardGuaranteed || dropType == DropType.StandardGuaranteed;

        var dropitems = this.items.ToList();
        var now = DateTime.UtcNow;
        //AddTimelyMonthDrop(10, 1, "Halloween Token");
        AddMonthDrop(dropitems, 12, 1, "Christmas Token", "061edf28-ca3f-4a00-992e-ba8b8a949631", 0.05f, 0.0175f);
        AddMonthDrop(dropitems, 10, now.Year == 2021 ? 2 : 1, "Halloween Token", "91fc824a-0ede-4104-96d1-531cdf8d56a6", 0.05f, 0.0175f);

        do
        {
            var allItems = gameManager.Items.GetItems();
            var droppableItems = dropitems.Select(x =>
            {
                Item item = allItems.FirstOrDefault(y =>
                    y.Name.StartsWith(x.ItemName ?? "", StringComparison.OrdinalIgnoreCase) ||
                    y.Name.StartsWith(x.ItemID, StringComparison.OrdinalIgnoreCase) ||
                    y.Id.ToString().ToLower() == x.ItemID.ToLower());

                if (item == null && Guid.TryParse(x.ItemID, out var itemId))
                    item = allItems.FirstOrDefault(y => y.Id == itemId);

                return new
                {
                    Item = item,
                    x.DropChance
                };
            })
                .Where(x => x.Item != null)
                .OrderByDescending(x => x.DropChance).ToList();

            foreach (var item in droppableItems)
            {

                if (UnityEngine.Random.value <= item.DropChance)
                {
                    if (!player.IsBot)
                        return null;

                    return item.Item;
                }
            }
        } while (guaranteedDrop);

        return null;
    }

    //Send Message Winning loot, limited by 500 text
    //Formatted like so, sending new message if last string goes over 500 text
    /*
     * Victorious! The loots have gone to these players: Loot Name:- UserName1 & Username2!! Other Loot:- UserName4 & UserName7!!
     */
    internal void AlertPlayers(Dictionary<Item, List<string>> playerListByItem)
    {
        try
        {
            int maxMessageLenght = 500; //Set to max lenght that can be transmitted over twitch
            string messageStart = "Victorious! The loots have gone to these players: "; //first part of the message
            StringBuilder sb = new StringBuilder(100, maxMessageLenght);
            int rollingCount = messageStart.Length;
            sb.Append(messageStart);

            foreach (KeyValuePair<Item, List<string>> kvItem in playerListByItem) //for each keypair
            {
                Item thisItem = kvItem.Key;
                string name = name = thisItem.Name;

                if (thisItem.Category == ItemCategory.StreamerToken)
                {
                    name = this.gameManager.RavenNest.TwitchDisplayName + " Token"; //convert streamertoken to correct name
                }
                name += ":- "; //add :-

                if (rollingCount + name.Length >= maxMessageLenght) //check that we don't appending a message over max Lenght
                {
                    gameManager.RavenBot.Send(null, sb.ToString(), null); //send and clear if we do
                    sb.Clear();
                }
                sb.Append(name);
                rollingCount = sb.Length;

                List<string> playerInList = kvItem.Value; //get list of players that gotten this loot
                int totalplayerInList = playerInList.Count;

                for (int i = 0; i < totalplayerInList; i++)
                {
                    string playerName = playerInList[i];
                    playerName += (i + 1) == totalplayerInList ? "!! " : " & "; //append !! to last player in the list, & if not
                    if (rollingCount + playerName.Length >= maxMessageLenght) //message check
                    {
                        gameManager.RavenBot.Send(null, sb.ToString().Trim(), null);
                        sb.Clear();
                    }
                    sb.Append(playerName);
                    rollingCount = sb.Length;
                }
            }

            if (sb.Length > 0)
                gameManager.RavenBot.Send(null, sb.ToString(), null); //I think RavenBot catches empty string, I don't think we'll get an empty char or few
        }
        catch (Exception ex)
        {
            //TODO - most likely error is over Max cap for StringBuilder, set maxMessageLenght
        }
    }

    private void AddMonthDrop(List<ItemDrop> droplist, int monthStart, int monthsLength, string itemName, string itemId, float maxDropRate, float minDropRate)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, monthStart, 1);
        var end = start.AddMonths(monthsLength);
        if (now >= start && now < end)
        {
            var dropChance = now.Date == start || now.Date >= end.AddDays(-1)
                    ? maxDropRate
                    : Mathf.Lerp(minDropRate, maxDropRate, (float)((end - now) / (end - start)));

            droplist.Add(new ItemDrop
            {
                ItemName = itemName,
                ItemID = itemId,
                DropChance = dropChance,
            });
        }
    }


}

public enum DropType
{
    Standard,
    StandardGuaranteed,
    MagicReward,
    MagicRewardGuaranteed
}

[Serializable]
public class ItemDrop
{
    public string ItemID;
    public string ItemName;

    [Range(0.0001f, 1f)]
    public float DropChance;
}
