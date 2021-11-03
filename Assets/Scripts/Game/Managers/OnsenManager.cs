using System.Runtime.Remoting.Messaging;
using UnityEngine;

public class OnsenManager : MonoBehaviour
{
    [SerializeField] private OnsenController onsen;
    [SerializeField] private GameManager game;

    public Vector3 EntryPoint => onsen.EntryPoint;
    private void Start()
    {
        if (!game) game = FindObjectOfType<GameManager>();
    }
    public bool Join(PlayerController player)
    {
        if (player.Island?.Identifier != onsen.Island.Identifier)
        {
            if (!player.Island)
            {
                game.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_FERRY);
                return false;
            }

            game.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_WRONG_ISLAND, player.Island.Identifier);
            return false;
        }

        var spot = onsen.GetNextAvailableSpot();
        if (spot == null)
        {
            game.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_FULL);
            return false;
        }

        player.Onsen.Enter(spot.Type, spot.Target);
        //player.Teleporter.

        onsen.UpdateDetailsLabel();
        return true;
    }
    public void Leave(PlayerController player)
    {
        player.Onsen.Exit(onsen.EntryPoint);
        onsen.UpdateDetailsLabel();
    }
}
