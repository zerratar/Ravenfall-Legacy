using UnityEngine;
using RavenNest.Models;
using System;

public class ClanHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private PlayerLogoManager playerLogoManager;

    private void Start()
    {
        if (!player) player = GetComponent<PlayerController>();
        this.playerLogoManager = FindAnyObjectByType<PlayerLogoManager>();
    }

    public bool InClan { get; private set; }
    public Clan ClanInfo { get; private set; }
    public ClanRole Role { get; private set; }
    public string Logo { get; private set; }

    public void SetClan(Clan clan, ClanRole role, GameManager gm, PlayerLogoManager plm)
    {
        if (!playerLogoManager) playerLogoManager = plm;

        InClan = clan != null;
        ClanInfo = clan;
        Role = role;

        // the clan's own logo when it has a usable one (an empty or relative one broke the download and left the
        // plain image on every cape), otherwise the owner's from the clan logo endpoint
        if (InClan && plm != null)
            Logo = plm.ResolveClanLogoUrl(clan.Logo, clan.Owner);
        else if (InClan && !string.IsNullOrEmpty(clan.Owner))
            Logo = string.IsNullOrWhiteSpace(clan.Logo) ? null : clan.Logo;
        else
            Logo = null;

        if (clan != null && gm != null && gm.Clans != null)
            gm.Clans.RegisterClan(clan);
    }

    public void SetRole(ClanRole role)
    {
        this.Role = role;
        // a role can wear its own cape shape
        if (player && player.Appearance) player.Appearance.UpdateClanCape();
    }

    internal void Leave()
    {
        // remove clan details
        InClan = false;
        ClanInfo = null;
        Logo = null;
        Role = null;

        // update clan related mesh objects, disabling cape, etc.
        InvalidateGraphics();
    }

    internal void Join(Clan clan, ClanRole role)
    {
        SetClan(clan, role, player.GameManager, playerLogoManager);

        // update clan related mesh objects, disabling cape, etc.
        InvalidateGraphics();
    }

    private void InvalidateGraphics()
    {
        var nameTag = player.GameManager.NameTags.Get(player);
        if (nameTag)
        {
            nameTag.Refresh();
        }
    }
}
