using UnityEngine;

public class AnimationEventController : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    private SteeringWheelController steeringWheel;

    private void Awake()
    {
        if (!player) player = GetComponent<PlayerController>();
    }

    public void PlaySpinAnimation()
    {
        if (!steeringWheel)
            steeringWheel = GameObject.FindObjectOfType<SteeringWheelController>();

        if (steeringWheel)
            steeringWheel.PlaySpinAnimation();
    }

    public void Shoot()
    {
        //Debug.Log(this.name + " Shoot Projectile");
        if (!player.Target)
        {
            return;
        }

        if (player.TrainingMagic)
        {
            player.Effects.ShootMagicProjectile(player.Target.position);
        }

        if (player.TrainingRanged)
        {
            player.Effects.ShootRangedProjectile(player.Target.position);
        }
    }

    public void WeaponSwitch() { }
    public void FootL() { }
    public void FootR() { }
}
