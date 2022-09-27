using UnityEngine;

public class OnsenHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private OnsenController activeOnsen;
    private OnsenPositionType positionType;
    private int onsenParentID;

    public bool InOnsen { get; private set; }
    public Vector3 EntryPoint => activeOnsen.EntryPoint;

    public const double RestedGainFactor = 2.0;
    public const double RestedDrainFactor = 1.0;

    private void Update()
    {
        if (player.Rested.RestedTime > 0)
        {
            if (player.Rested.ExpBoost == 0)
                player.Rested.ExpBoost = 2;
        }
        else if (player.Rested.ExpBoost > 0)
        {
            player.Rested.ExpBoost = 0;
        }

        if (!InOnsen)
        {
            if (player.Rested.RestedTime > 0)
            {
                player.Rested.RestedTime -= Time.deltaTime * RestedDrainFactor;
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

        player.Rested.RestedTime += Time.deltaTime * RestedGainFactor;
    }

    public void Enter(OnsenController onsen)
    {
        var spot = onsen.GetNextAvailableSpot();
        if (spot == null)
        {
            player.GameManager.RavenBot.SendMessage(player.PlayerName, Localization.MSG_ONSEN_FULL);
            return;
        }

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
        }

        player.Movement.Lock();
        InOnsen = true;

        var target = spot.Target;
        player.Teleporter.Teleport(target.position, false);
        player.transform.SetParent(target);
        player.transform.localRotation = Quaternion.identity;
        player.transform.localPosition = Vector3.zero;
        this.onsenParentID = target.GetInstanceID();

        onsen.UpdateDetailsLabel();
        player.GameManager.SaveNow();
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
            player.Teleporter.Teleport(prevOnsen.EntryPoint, true, true);
        }

        prevOnsen.UpdateDetailsLabel();
        InOnsen = false;
        player.GameManager.SaveNow();
    }
}
