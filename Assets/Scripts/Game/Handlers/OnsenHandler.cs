using UnityEngine;

public class OnsenHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private OnsenPositionType positionType;
    private int onsenParentID;

    public bool InOnsen { get; private set; }

    private void Update()
    {
        if (!InOnsen)
        {
            return;
        }

        if (!this.transform.parent)
        {
            this.InOnsen = false;
            return;
        }

        if (this.transform.parent.GetInstanceID() != onsenParentID)
        {
            this.InOnsen = false;
            return;
        }
    }

    public void Enter(OnsenPositionType positionType, Transform target)
    {
        // used for determing which animation to use
        this.positionType = positionType;

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

        player.Lock();
        InOnsen = true;

        player.Teleporter.Teleport(target.position);
        player.transform.SetParent(target);
        player.transform.localRotation = Quaternion.identity;
        player.transform.localPosition = Vector3.zero;
        this.onsenParentID = target.GetInstanceID();
    }

    public void Exit(Vector3 teleportPoint)
    {
        player.Animations.ClearOnsenAnimations();
        onsenParentID = -1;
        player.Unlock();
        InOnsen = false;

        player.transform.SetParent(null);
        player.Teleporter.Teleport(teleportPoint, true);
    }
}
