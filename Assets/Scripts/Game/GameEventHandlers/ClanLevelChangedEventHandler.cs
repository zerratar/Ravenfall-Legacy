public class ClanLevelChangedEventHandler : GameEventHandler<ClanLevelChanged>
{
    protected override void Handle(GameManager gameManager, ClanLevelChanged data)
    {
        gameManager.Clans.UpdateClanLevel(data);
    }
}
