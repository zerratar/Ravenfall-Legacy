using UnityEngine;

public class OnsenHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private OnsenController activeOnsen;
    private OnsenPositionType positionType;
    private int onsenParentID;

    public bool InOnsen;
    public Vector3 EntryPoint => activeOnsen.EntryPoint;

    public const double RestedGainFactor = 2.0;
    public const double RestedDrainFactor = 1.0;

    private void Update()
    {
        var rested = player.Rested;
        if (rested.RestedTime > 0)
        {
            if (rested.ExpBoost == 0)
                rested.ExpBoost = 2;
        }
        else if (rested.ExpBoost > 0)
        {
            rested.ExpBoost = 0;
        }

        if (!InOnsen)
        {
            if (rested.RestedTime > 0)
            {
                rested.RestedTime -= GameTime.deltaTime * RestedDrainFactor;
            }
            return;
        }

        if (!this.transform.parent || activeOnsen == null || player.InCombat)
        {
            this.InOnsen = false;
            return;
        }

        if (this.transform.parent.GetInstanceID() != onsenParentID)
        {
            this.InOnsen = false;
            return;
        }

        rested.RestedTime += GameTime.deltaTime * RestedGainFactor;
        if (rested.RestedTime >= CharacterRestedState.RestedTimeMax)
        {
            rested.RestedTime = CharacterRestedState.RestedTimeMax;
        }

        if (this.InOnsen)
        {
            // force player position
            player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void Enter(OnsenController onsen)
    {
        var spot = onsen.GetNextAvailableSpot();
        if (spot == null)
        {
            player.GameManager.RavenBot.SendReply(player, Localization.MSG_ONSEN_FULL);
            return;
        }
        
        var target = spot.Target;

        player.Movement.Lock();
        player.teleportHandler.Teleport(target.position, false);
        player._transform.SetParent(target);
        player._transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        // used for determing which animation to use
        this.positionType = spot.Type;
        this.activeOnsen = onsen;
        switch (positionType)
        {
            case OnsenPositionType.Sitting:
                player.Animations.Sit();
                break;

            case OnsenPositionType.Swimming:
                player.Animations.Swim();
                break;

            case OnsenPositionType.Meditating:
                player.Animations.Meditate();
                break;

            case OnsenPositionType.Sleeping:
                player.Animations.Sleep();
                break;
        }

        this.onsenParentID = target.GetInstanceID();

        InOnsen = true;
        onsen.UpdateDetailsLabel();
    }

    public void Exit()
    {
        var prevOnsen = activeOnsen;
        activeOnsen = null;

        player.Animations.ClearOnsenAnimations();
        onsenParentID = -1;

        if (InOnsen)
        {
            player.Movement.Unlock();
            player.transform.SetParent(null);
            player.teleportHandler.Teleport(prevOnsen.EntryPoint, true, true);
        }

        prevOnsen.UpdateDetailsLabel();
        InOnsen = false;
    }
}
