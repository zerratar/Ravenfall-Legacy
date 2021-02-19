using UnityEngine;

public class AnimationEventController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private EnemyController enemy;

    private SteeringWheelController steeringWheel;

    private void Awake()
    {
        if (!player) player = GetComponent<PlayerController>();
        if (!enemy) enemy = GetComponent<EnemyController>();
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
        if (!player || !player.Target)
            return;

        if (player.TrainingHealing)
        {
            var targetPlayer = player.Target.GetComponent<PlayerController>();
            if (targetPlayer)
                targetPlayer.Effects.Heal();
        }

        if (player.TrainingMagic)
            player.Effects.ShootMagicProjectile(player.Target.position);

        if (player.TrainingRanged)
            player.Effects.ShootRangedProjectile(player.Target.position);
    }

    public void Hit() { }
    public void WeaponSwitch() { }
    public void FootL() { }
    public void FootR() { }
}