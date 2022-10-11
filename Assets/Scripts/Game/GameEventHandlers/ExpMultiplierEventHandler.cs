public class ExpMultiplierEventHandler : GameEventHandler<ExpMultiplier>
{
    public override void Handle(GameManager gameManager, ExpMultiplier data)
    {
        ExpMultiplierChecker.LastExpMultiOnlineCheck = System.DateTime.UtcNow; // just to make sure we don't test too frequently.

        gameManager.Twitch.SetExpMultiplier(
            data.EventName,
            data.Multiplier,
            data.StartTime,
            data.EndTime);
    }
}
