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
        this.playerLogoManager = FindObjectOfType<PlayerLogoManager>();
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

        if (InClan && !string.IsNullOrEmpty(clan.Owner))
            Logo = clan.Logo ?? (plm?.ClanLogoUrl + clan.Owner);
        else
            Logo = null;

        if (clan != null && gm != null && gm.Clans != null)
            gm.Clans.RegisterClan(clan);
    }

    public void SetRole(ClanRole role)
    {
        this.Role = role;
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
