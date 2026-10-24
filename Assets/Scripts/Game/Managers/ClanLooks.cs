using System;
using System.Collections.Generic;
using RavenNest.SDK.Endpoints;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// What each clan looks like: its cape shape (per role too) and whether it has its own cape image. Asked for once
/// per clan (api/clan/look/{clanId}) and kept, and replaced when the server sends ClanLookChanged. See
/// docs/clan-images-design.md.
/// </summary>
/// <remarks>
/// A server without the endpoint (or a failed request) counts as the default look, the long cape with the logo,
/// and is asked again later, so this client works against an older server too.
/// </remarks>
public class ClanLooks
{
    /// <summary>The clan cape meshes on the player for each ClanCapeStyle: long, pointed, short, tattered.</summary>
    public static readonly int[] StyleToCape = { 0, 1, 2, 4 };

    private static readonly TimeSpan LifeSpan = TimeSpan.FromMinutes(30);

    private readonly GameManager gameManager;
    private readonly Dictionary<Guid, Entry> looks = new Dictionary<Guid, Entry>();
    private readonly HashSet<Guid> requesting = new HashSet<Guid>();

    public ClanLooks(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    /// <summary>The clan's look, or null while it is being asked for (the clan's players are updated when it comes).</summary>
    public ClanLook Get(Guid clanId)
    {
        if (clanId == Guid.Empty) return null;
        if (looks.TryGetValue(clanId, out var entry))
        {
            if (DateTime.UtcNow > entry.Expires) Request(clanId);
            return entry.Look;
        }

        Request(clanId);
        return null;
    }

    /// <summary>The cape mesh for a member with this role: the role's own shape when it has one, otherwise the clan's.</summary>
    public int CapeFor(Guid clanId, Guid? roleId)
    {
        var look = Get(clanId);
        if (look == null) return StyleToCape[0];
        var style = look.CapeStyle;
        if (roleId != null && look.RoleCapeStyles != null && look.RoleCapeStyles.TryGetValue(roleId.Value, out var roleStyle))
            style = roleStyle;
        return StyleToCape[Mathf.Clamp(style, 0, StyleToCape.Length - 1)];
    }

    /// <summary>The server says the clan changed: keep the new look and dress its players again.</summary>
    public void Apply(ClanLook look)
    {
        if (look == null || look.ClanId == Guid.Empty) return;
        looks[look.ClanId] = new Entry(look);
        Refresh(look.ClanId);
    }

    private void Request(Guid clanId)
    {
        if (!requesting.Add(clanId)) return;
        var url = (gameManager.ServerAddress ?? "https://www.ravenfall.stream/api/") + "clan/look/" + clanId;
        var www = UnityWebRequest.Get(url);
        www.SendWebRequest().completed += _ =>
        {
            requesting.Remove(clanId);
            ClanLook look = null;
            try
            {
                if (www.result == UnityWebRequest.Result.Success)
                    look = Newtonsoft.Json.JsonConvert.DeserializeObject<ClanLook>(www.downloadHandler.text);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogWarning("Could not read the look of clan " + clanId + ": " + exc.Message);
            }
            finally
            {
                www.Dispose();
            }

            var previous = looks.TryGetValue(clanId, out var old) ? old.Look : null;
            looks[clanId] = new Entry(look ?? previous ?? new ClanLook { ClanId = clanId });
            if (!Same(previous, looks[clanId].Look)) Refresh(clanId);
        };
    }

    private void Refresh(Guid clanId)
    {
        if (gameManager.Players == null) return;
        foreach (var player in gameManager.Players.GetAllPlayers())
        {
            if (player && player.clanHandler && player.clanHandler.InClan && player.clanHandler.ClanInfo != null && player.clanHandler.ClanInfo.Id == clanId)
                player.Appearance.UpdateClanCape();
        }
    }

    private static bool Same(ClanLook a, ClanLook b)
    {
        if (a == null || b == null) return a == b;
        if (a.CapeStyle != b.CapeStyle || a.CapeImageId != b.CapeImageId || a.LogoImageId != b.LogoImageId) return false;
        var ra = a.RoleCapeStyles ?? new Dictionary<Guid, int>();
        var rb = b.RoleCapeStyles ?? new Dictionary<Guid, int>();
        if (ra.Count != rb.Count) return false;
        foreach (var pair in ra)
            if (!rb.TryGetValue(pair.Key, out var v) || v != pair.Value) return false;
        return true;
    }

    private class Entry
    {
        public Entry(ClanLook look)
        {
            Look = look;
            Expires = DateTime.UtcNow + LifeSpan;
        }

        public ClanLook Look { get; }
        public DateTime Expires { get; }
    }
}

namespace RavenNest.SDK.Endpoints
{
    /// <summary>A copy of the server's RavenNest.Models.ClanLook, kept here rather than in the old models assembly.</summary>
    public class ClanLook
    {
        public Guid ClanId { get; set; }
        public Guid? LogoImageId { get; set; }
        public Guid? CapeImageId { get; set; }
        public int CapeStyle { get; set; }
        public Dictionary<Guid, int> RoleCapeStyles { get; set; } = new Dictionary<Guid, int>();
    }
}
