using Shinobytes.Linq;
//using System.Linq;
using UnityEngine;

public class OnsenManager : MonoBehaviour
{
    private OnsenController[] restingAreas;

    [SerializeField] private GameManager game;

    private Vector3[] entryPoints;
    public Vector3[] EntryPoint => entryPoints;//.EntryPoint;

    private void Start()
    {
        if (!game) game = FindAnyObjectByType<GameManager>();

        restingAreas = FindObjectsByType<OnsenController>(FindObjectsSortMode.None);

        this.entryPoints = restingAreas.Select(x => x.EntryPoint).ToArray();
    }

    public bool Join(PlayerController player)
    {
        var restingArea = restingAreas.FirstOrDefault(x => x.Island.Identifier == player.Island?.Identifier);

        if (!restingArea)
        {
            if (!player.Island)
            {
                game.RavenBot.SendReply(player, Localization.MSG_ONSEN_FERRY.Random());
                return false;
            }

#if DEBUG
            if (!AdminControlData.ControlPlayers)
            {
                game.RavenBot.SendReply(player, Localization.MSG_ONSEN_WRONG_ISLAND, player.Island.Identifier);
            }
#else
            game.RavenBot.SendReply(player, Localization.MSG_ONSEN_WRONG_ISLAND, player.Island.Identifier);
#endif
            return false;
        }

        player.onsenHandler.Enter(restingArea);

        return true;
    }
    public void Leave(PlayerController player)
    {
        player.onsenHandler.Exit();
    }
}
