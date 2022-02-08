using UnityEngine;
using RavenNest.Models;

public class ClanHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private PlayerLogoManager playerLogoManager;

    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        this.playerLogoManager = FindObjectOfType<PlayerLogoManager>();
    }
    public bool InClan { get; private set; }
    public Clan ClanInfo { get; private set; }
    public ClanRole Role { get; private set; }
    public string Logo { get; private set; }
    public void SetClan(Clan clan, ClanRole role, GameManager gm, PlayerLogoManager plm)
    {
        //if (!playerLogoManager) playerLogoManager = FindObjectOfType<PlayerLogoManager>();

        InClan = clan != null;
        ClanInfo = clan;
        Role = role;

        if (InClan && !string.IsNullOrEmpty(clan.Owner))
            Logo = clan.Logo ?? (plm?.TwitchLogoUrl + clan.Owner);
        else
            Logo = null;

        if (clan != null && gm != null && gm.Clans != null)
            gm.Clans.RegisterClan(clan);
    }
}
