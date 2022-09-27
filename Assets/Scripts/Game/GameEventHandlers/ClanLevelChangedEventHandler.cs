public class ClanLevelChangedEventHandler : GameEventHandler<ClanLevelChanged>
{
    public override void Handle(GameManager gameManager, ClanLevelChanged data)
    {
        gameManager.Clans.UpdateClanLevel(data);
    }
}
