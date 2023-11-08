using System;
using System.Threading;
using System.Threading.Tasks;

public class ExpMultiplierChecker
{
    public static DateTime LastExpMultiOnlineCheck;
    private static TimeSpan onlineExpCheckInterval = TimeSpan.FromMinutes(1);
    private static TimeSpan twitchExpCheckInterval = TimeSpan.FromMinutes(1);
    private volatile static int isRunning;

    public static async Task RunAsync(GameManager game)
    {
        if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 1)
            return;

        try
        {
            var now = DateTime.UtcNow;
            if ((now - LastExpMultiOnlineCheck) >= onlineExpCheckInterval)
            {
                LastExpMultiOnlineCheck = now;

                // the game.Twitch.LastUpdated is only set IF actually the multiplier was changed.            
                if (now - game.Twitch.LastUpdated >= twitchExpCheckInterval)
                {
                    var result = await game.RavenNest.Game.GetExpMultiplierAsync();
                    if (result != null)
                    {
                        game.HandleGameEvent(result);
                    }
                }
            }
        }
        catch { }
        finally
        {
            Interlocked.Exchange(ref isRunning, 0);
        }
    }
}
