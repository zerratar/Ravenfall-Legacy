using UnityEngine;
using RavenNest.Models;

public class ClanHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;

    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }
    public bool InClan { get; private set; }
    public Clan ClanInfo { get; private set; }
    public ClanRole Role { get; private set; }
    public string Logo { get; private set; }
    public void SetClan(Clan clan, ClanRole role)
    {
        InClan = clan != null;
        ClanInfo = clan;
        Role = role;

        if (InClan && !string.IsNullOrEmpty(clan.Owner))
            Logo = clan.Logo ?? (PlayerLogoManager.TwitchLogoUrl + clan.Owner);
        else
            Logo = null;

        if (clan != null && gameManager != null && gameManager.Clans != null)
            gameManager.Clans.RegisterClan(clan);
    }
}
