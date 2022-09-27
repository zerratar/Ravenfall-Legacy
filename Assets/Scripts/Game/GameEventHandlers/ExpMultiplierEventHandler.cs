public class ExpMultiplierEventHandler : GameEventHandler<ExpMultiplier>
{
    public override void Handle(GameManager gameManager, ExpMultiplier data)
    {        
        gameManager.Twitch.SetExpMultiplier(
            data.EventName, 
            data.Multiplier,
            data.StartTime,
            data.EndTime);
    }
}
