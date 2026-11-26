using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using RavenNest.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     Ravenfall > Tower Places' "Play this part": enters play mode and sends one test bot straight to a part, a town or
///     a place not in the climb yet, the way the tower tests do (tools/tower/tower2-test.cs, tower3-test.cs): the part
///     is loaded (TowerManager.LoadFloor, whatever the stream's climb), the bot walks in, and the game's camera follows
///     it. A bot needs the game signed in to the server (the local RavenNest with the saved device token); without a
///     sign in the part is still loaded and the Scene view looks at it.
///     <para>With "Quiet chat" on (the default), chat is muted for that play session (Ravenfall > Mute Chat In Play
///     Mode, put back as it was afterwards) and the chat bot is stopped once the game has signed in, so nothing of the
///     test reaches a real chat.</para>
///     <para>The request lives in SessionState, as play mode reloads the scripts.</para>
/// </summary>
[InitializeOnLoad]
public static class TowerPlayPart
{
    private const string KeyRequest = "Ravenfall.TowerPlaces.Play.Request";
    private const string KeyQuiet = "Ravenfall.TowerPlaces.Play.Quiet";
    private const string KeyMuteBefore = "Ravenfall.TowerPlaces.Play.MuteBefore";
    private const string KeyStatus = "Ravenfall.TowerPlaces.Play.Status";

    private const float SignInSeconds = 150f;
    private const float LoadSeconds = 60f;

    /// <summary>What the last request is doing now, for the window.</summary>
    public static string Status => SessionState.GetString(KeyStatus, "");

    public static bool Busy => SessionState.GetString(KeyRequest, "").Length > 0;

    static TowerPlayPart()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (EditorApplication.isPlaying && Busy) Begin();
    }

    /// <summary>
    ///     Plays a place: "part N", "town KEY" or "place SCENE". False (with why) when play mode cannot start from here.
    /// </summary>
    public static bool Request(string key, bool quiet, out string error)
    {
        error = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { error = "Already in play mode: stop it first."; return false; }
        var main = SceneManager.GetSceneByName("MainWorld");
        if (!main.IsValid() || !main.isLoaded) { error = "Open MainWorld first: the game runs from it."; return false; }
        // a tower scene open in the editor would play as a second copy beside the one the game loads
        foreach (var s in TowerPlaceScenes.OpenTowerScenes())
        {
            if (s.isDirty)
            {
                var choice = EditorUtility.DisplayDialogComplex("Tower Places", s.name + " has changes that are not saved. The game loads its own copy, so it is closed first. Save it?", "Save", "Cancel", "Don't save");
                if (choice == 1) { error = "Canceled."; return false; }
                if (choice == 0 && !TowerPlaceScenes.Save(s, out error)) return false;
            }
            TowerPlaceScenes.Close(s);
        }
        if (SceneManager.GetActiveScene() != main) SceneManager.SetActiveScene(main);
        SessionState.SetString(KeyRequest, key);
        SessionState.SetBool(KeyQuiet, quiet);
        if (quiet)
        {
            SessionState.SetBool(KeyMuteBefore, EditorPrefs.GetBool(MuteChatInPlayMode.PrefKey, false));
            EditorPrefs.SetBool(MuteChatInPlayMode.PrefKey, true);
        }
        SetStatus("Starting play mode...");
        EditorApplication.EnterPlaymode();
        return true;
    }

    private static void SetStatus(string text)
    {
        SessionState.SetString(KeyStatus, text);
        Debug.Log("Tower Places, play: " + text);
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && Busy) Begin();
        if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.ExitingPlayMode)
        {
            if (SessionState.GetBool(KeyQuiet, false))
            {
                EditorPrefs.SetBool(MuteChatInPlayMode.PrefKey, SessionState.GetBool(KeyMuteBefore, false));
                SessionState.EraseBool(KeyQuiet);
            }
            if (Busy) SetStatus("Play mode ended before the part was reached.");
            SessionState.EraseString(KeyRequest);
            running = null;
            EditorApplication.update -= Tick;
        }
    }

    // ---- in play mode ---------------------------------------------------------------------------------------------

    private static IEnumerator running;
    private static double waitUntil;

    private static void Begin()
    {
        if (running != null) return;
        running = Run(SessionState.GetString(KeyRequest, ""), SessionState.GetBool(KeyQuiet, true));
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (running == null || !EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < waitUntil) return;
        try
        {
            if (!running.MoveNext())
            {
                running = null;
                SessionState.EraseString(KeyRequest);
                EditorApplication.update -= Tick;
            }
        }
        catch (Exception e)
        {
            SetStatus("Stopped: " + e.Message);
            Debug.LogException(e);
            running = null;
            SessionState.EraseString(KeyRequest);
            EditorApplication.update -= Tick;
        }
    }

    private static object Wait(double seconds)
    {
        waitUntil = EditorApplication.timeSinceStartup + seconds;
        return null;
    }

    private static IEnumerator Run(string key, bool quiet)
    {
        var started = EditorApplication.timeSinceStartup;
        int step = 0;
        string townKey = null, layout = null;
        if (key.StartsWith("part ")) int.TryParse(key.Substring(5), out step);
        else if (key.StartsWith("town ")) townKey = key.Substring(5);
        else if (key.StartsWith("place ")) layout = key.Substring(6);

        SetStatus("Waiting for the game to load and sign in...");
        GameManager gm = null;
        var signedIn = false;
        while (EditorApplication.timeSinceStartup - started < SignInSeconds)
        {
            gm = GameManager.Instance;
            if (gm && gm.IsLoaded && TowerManager.Instance && gm.RavenNest != null && gm.RavenNest.Authenticated && gm.RavenNest.SessionStarted)
            {
                signedIn = true;
                break;
            }
            yield return Wait(1);
        }
        var tower = TowerManager.Instance;
        if (!tower) { SetStatus("No TowerManager in the game: is MainWorld the scene in play?"); yield break; }
        if (quiet && signedIn)
        {
            AdminControlData.NoChatBotMessages = true;
            try { gm.RavenBot?.Stop(false); } catch (Exception e) { Debug.LogWarning("Tower Places: could not stop the chat bot: " + e.Message); }
        }

        PlayerController bot = null;
        if (signedIn)
        {
            SetStatus("Signed in. Making a test bot...");
            var before = gm.Players.GetAllBots().ToList();
            var task = gm.SpawnBotPlayer();
            var t0 = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - t0 < 40)
            {
                bot = gm.Players.GetAllBots().FirstOrDefault(b => b && !before.Contains(b));
                if (bot || task.IsFaulted) break;
                yield return Wait(0.5);
            }
            if (!bot) SetStatus("The test bot did not join" + (task.IsFaulted ? " (" + task.Exception?.GetBaseException().Message + ")" : "") + ". Loading the place without one...");
            else yield return Wait(2); // a moment to land and equip
        }

        if (townKey != null)
        {
            var definition = Tower.Town(townKey);
            if (definition == null) { SetStatus("There is no town " + townKey + "."); yield break; }
            SetStatus("Loading " + definition.Name + "...");
            if (bot && tower.TownUnlocked(definition)) tower.JoinTown(bot, definition);
            else if (!TowerTown.Loaded || TowerTown.Loaded.Key != townKey)
            {
                // a town the stream has not reached (or no bot): loaded as the tower loads it
                var routine = typeof(TowerManager).GetMethod("LoadTownRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
                tower.StartCoroutine((IEnumerator)routine.Invoke(tower, new object[] { TowerManager.ScenePath(definition.Layout) }));
            }
            var t0 = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - t0 < LoadSeconds && (!TowerTown.Loaded || TowerTown.Loaded.Key != townKey || tower.TownLoading)) yield return Wait(0.5);
            var town = TowerTown.Loaded;
            if (!town || town.Key != townKey) { SetStatus(definition.Name + " did not load."); yield break; }
            if (bot && !(bot.towerHandler.InTown)) bot.towerHandler.Enter(town);
            Arrive(gm, bot, town, definition.Name, signedIn);
            yield break;
        }

        if (layout != null)
        {
            // a place not in the climb: every part loads it for now (the admin debug menu's layout preview)
            if (!tower.PreviewLayout(layout)) { SetStatus("The tower is busy (a boss attempt or a part loading): try again."); yield break; }
            step = tower.StreamFloor > 0 ? tower.StreamFloor : 1;
        }
        if (Tower.Get(step) == null) { SetStatus("There is no part " + step + "."); yield break; }
        var label = layout != null ? Tower.AreaName(layout) + " (filled as " + Tower.Label(step) + ")" : Tower.Label(step) + ", " + Tower.Get(step).Name;
        SetStatus("Loading " + label + "...");
        tower.LoadFloor(step);
        TowerManager.Area area = null;
        var t1 = EditorApplication.timeSinceStartup;
        while (EditorApplication.timeSinceStartup - t1 < LoadSeconds)
        {
            area = tower.AreaAt(step);
            if (area != null && !area.Loading && area.Floor) break;
            yield return Wait(0.5);
        }
        if (area == null || !area.Floor) { SetStatus(label + " did not load (no free place for it? the tower keeps " + TowerManager.MaxAreas + " parts open)."); yield break; }
        if (bot) bot.towerHandler.Enter(area.Floor);
        Arrive(gm, bot, area.Floor, label, signedIn);
    }

    private static void Arrive(GameManager gm, PlayerController bot, TowerPlace place, string label, bool signedIn)
    {
        if (bot && gm.Camera) gm.Camera.ObservePlayer(bot);
        TowerPlaceScenes.Frame(place);
        if (bot) SetStatus(bot.Name + " is in " + label + "; the game's camera follows it.");
        else if (signedIn) SetStatus(label + " is loaded, without a test bot (it did not join). The Scene view looks at it.");
        else SetStatus(label + " is loaded, without a player: the game did not sign in within " + (int)SignInSeconds + " s, and a test bot needs the server. Start the local RavenNest (Ravenfall > Use Local RavenNest Server), sign in, and press Play this part again. The Scene view looks at it.");
    }
}
