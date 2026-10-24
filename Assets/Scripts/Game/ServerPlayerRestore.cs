using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;

/// <summary>
/// Players the server remembers in the stream that the game's own state file did not have. A streamer who reinstalls
/// or loses the state file used to start with nobody, and the players had to be put back by hand (an admin generated a
/// state file from the server). Now the game asks the server at startup, restores its own state file first as it
/// always did, and offers the rest: restored at once with the "always restore from the server" setting, otherwise
/// through PlayerRestoreView, which asks without stopping anything, so a streamer who is away loses nothing.
/// </summary>
/// <remarks>
/// <para>
/// The server's list is the characters locked to the streamer plus the last player list the game sent. The game
/// sends its list about once a minute (<see cref="UpdateUploads"/>), replacing the stored one; the server only ever
/// added to it before, so players who left stayed in it. Nothing is sent before the server's list has been read and
/// compared, and players still waiting to be restored are sent with the rest, so a half restored game never shrinks
/// what the server remembers.
/// </para>
/// <para>
/// The expiry setting applies to the server's list as to the state file (the server says when it was stored), and an
/// expiry of none means no restoring from either.
/// </para>
/// </remarks>
public sealed class ServerPlayerRestore
{
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan UploadInterval = TimeSpan.FromSeconds(60);

    public static ServerPlayerRestore Instance { get; private set; }

    private readonly List<RestorablePlayer> pending = new List<RestorablePlayer>();
    private RestorableGameState? serverState;
    private DateTime nextUpload;
    private string lastUploaded;
    private bool restoring;
    private bool uploadWarned;

    public ServerPlayerRestore()
    {
        Instance = this;
    }

    /// <summary>Players the server remembers that are not in the game, waiting for the streamer.</summary>
    public IReadOnlyList<RestorablePlayer> Pending => pending;

    /// <summary>Goes up whenever <see cref="Pending"/> changes, so the window can tell a new offer from an old one.</summary>
    public int Version { get; private set; }

    /// <summary>The server's list has been read (or could not be) and compared: the game's own list may be sent.</summary>
    public bool Settled { get; private set; }

    /// <summary>How many players were brought back from the server's list this session.</summary>
    public int RestoredFromServer { get; private set; }

    public bool Restoring => restoring;

    /// <summary>Reads the server's list, waiting at most <see cref="FetchTimeout"/>. A server without it (older) gives none.</summary>
    public IEnumerator Fetch(GameManager game)
    {
        serverState = null;
        if (SettingsApplier.GetPlayerCacheExpiryTime() == TimeSpan.Zero)
        {
            yield break; // the streamer restores nobody
        }

        Task<RestorableGameState> task = null;
        try
        {
            task = game.RavenNest.Game.GetSessionPlayersAsync();
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Could not ask the server who was in the stream: " + exc.Message);
            yield break;
        }

        var started = Time.unscaledTime;
        while (!task.IsCompleted && Time.unscaledTime - started < FetchTimeout.TotalSeconds)
        {
            yield return null;
        }

        if (!task.IsCompleted || task.IsFaulted)
        {
            Shinobytes.Debug.LogWarning("The server did not say who was in the stream" + (task.IsFaulted ? ": " + task.Exception?.GetBaseException().Message : " in time") + ".");
            yield break;
        }

        serverState = task.Result;
        Shinobytes.Debug.Log("The server remembers " + (serverState?.Players?.Count ?? 0) + " players in the stream.");
    }

    /// <summary>
    /// What the server remembers that is neither in the state file nor in the game. Settles the comparison either way:
    /// from here the game's list is sent to the server.
    /// </summary>
    public void Compare(GameManager game, RestorableGameState? local)
    {
        pending.Clear();
        try
        {
            var state = serverState;
            if (state?.Players == null || state.Value.Players.Count == 0)
            {
                return;
            }

            var expiry = SettingsApplier.GetPlayerCacheExpiryTime();
            if (state.Value.Created > DateTime.MinValue && DateTime.UtcNow - state.Value.Created > expiry)
            {
                Shinobytes.Debug.Log("The server's player list is older than the state expiry setting; not offered.");
                return;
            }

            var known = new HashSet<Guid>();
            if (local?.Players != null)
            {
                foreach (var p in local.Value.Players) if (p != null) known.Add(p.CharacterId);
            }

            foreach (var p in game.Players.GetAllPlayers()) if (p) known.Add(p.Id);

            foreach (var p in state.Value.Players)
            {
                if (p == null || p.User == null || p.CharacterId == Guid.Empty || !known.Add(p.CharacterId)) continue;
                pending.Add(p);
            }

            if (pending.Count > 0)
            {
                Shinobytes.Debug.Log(pending.Count + " players the server remembers were not in the state file.");
            }
        }
        finally
        {
            Settled = true;
            Version++;
        }
    }

    /// <summary>Restores every pending player still not in the game, adding them to those already in it.</summary>
    public async void RestorePending(GameManager game)
    {
        if (restoring || pending.Count == 0)
        {
            return;
        }

        restoring = true;
        Version++;
        try
        {
            var inGame = new HashSet<Guid>(game.Players.GetAllPlayers().Where(x => x).Select(x => x.Id));
            var toRestore = pending.Where(x => !inGame.Contains(x.CharacterId)).ToList();
            Shinobytes.Debug.Log("Restoring " + toRestore.Count + " players the server remembers.");
            await game.Players.RestoreAsync(toRestore, additive: true);
            RestoredFromServer += toRestore.Count;
            pending.Clear();
            lastUploaded = null; // send the new list soon
            nextUpload = DateTime.UtcNow.AddSeconds(10);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Could not restore the players the server remembers: " + exc);
        }
        finally
        {
            restoring = false;
            Version++;
        }
    }

    /// <summary>The streamer does not want them back: the server lets go of them, so they are not offered again.</summary>
    public async void Decline(GameManager game)
    {
        if (restoring || pending.Count == 0)
        {
            return;
        }

        var ids = pending.Select(x => x.CharacterId).ToArray();
        pending.Clear();
        Version++;
        lastUploaded = null;
        nextUpload = DateTime.UtcNow;
        try
        {
            await game.RavenNest.Game.ReleasePlayersAsync(ids);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Could not tell the server to let go of " + ids.Length + " players: " + exc.Message);
        }
    }

    /// <summary>Sends the game's player list (with those still pending) about once a minute when it changed.</summary>
    public void UpdateUploads(GameManager game)
    {
        if (!Settled || restoring || DateTime.UtcNow < nextUpload)
        {
            return;
        }

        nextUpload = DateTime.UtcNow + UploadInterval;
        var ids = game.Players.GetAllRealPlayers().Where(x => x).Select(x => x.Id)
            .Concat(pending.Select(x => x.CharacterId))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var key = string.Join(",", ids);
        if (key == lastUploaded)
        {
            return;
        }

        lastUploaded = key;
        _ = Upload(game, ids);
    }

    private async Task Upload(GameManager game, Guid[] ids)
    {
        try
        {
            await game.RavenNest.Game.SetSessionPlayersAsync(ids);
        }
        catch (Exception exc)
        {
            // an older server has no such call; nothing is lost. Sent again next time.
            lastUploaded = null;
            if (!uploadWarned)
            {
                uploadWarned = true;
                Shinobytes.Debug.LogWarning("Could not send the player list to the server: " + exc.Message);
            }
        }
    }
}
