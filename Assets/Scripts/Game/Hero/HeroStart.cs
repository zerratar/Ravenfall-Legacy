using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Starting hero mode from outside (docs/hero-mode-design.md, 7d): !hero start from the streamer, and the forced
/// "goblins storm Ravebridge" event (RavebridgeForced with Storm). Ravebridge opens if it is closed, the streamer's own
/// character comes along, and once it is on the island it is played by hand as the hero; a storm also starts a wave
/// run at once. Without a character of the streamer's in the stream there is nothing to play, and it says so.
/// </summary>
public static class HeroStart
{
    public enum Result { Started, AlreadyHero, NoCharacter, Closed }

    private static Coroutine waiting;

    public static Result Begin(GameManager game, bool storm, string reason = null)
    {
        var rb = game ? game.Ravebridge : null;
        if (!rb) return Result.Closed;
        if (HeroController.Active)
        {
            if (storm && !WaveRun.Current) WaveRun.Begin(HeroController.Active.Player, 0, null);
            return Result.AlreadyHero;
        }

        if (!rb.Active)
        {
            var started = rb.Begin(storm, reason, true);
            if (started != RavebridgeManager.StartResult.Started && started != RavebridgeManager.StartResult.AlreadyActive) return Result.Closed;
        }

        var hero = game.Players.GetAllPlayers().FirstOrDefault(p => p && !p.IsBot && RavebridgeManager.IsStreamers(p));
        if (!hero) return Result.NoCharacter;
        if (!rb.Contains(hero)) rb.Join(hero);
        if (waiting != null) rb.StopCoroutine(waiting);
        waiting = rb.StartCoroutine(WhenThere(hero, storm));
        return Result.Started;
    }

    /// <summary>The character sails over first; it becomes the hero once it is on Ravebridge (a minute at most).</summary>
    private static IEnumerator WhenThere(PlayerController hero, bool storm)
    {
        for (var t = 0f; t < 60f; t += 0.5f)
        {
            if (!hero) yield break;
            if (hero.InRavebridge && hero.Island) break;
            yield return new WaitForSeconds(0.5f);
        }
        waiting = null;
        if (!hero || !hero.InRavebridge) yield break;
        if (!RavebridgePlay.Playing || RavebridgePlay.Playing != hero) RavebridgePlay.Begin(hero);
        if (!HeroController.Active) HeroController.Begin(hero);
        if (storm && HeroController.Active && !WaveRun.Current) WaveRun.Begin(hero, 0, null);
    }
}
