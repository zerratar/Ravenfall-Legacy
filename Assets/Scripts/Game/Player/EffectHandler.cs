using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EffectHandler : MonoBehaviour
{
    [Header("Spell Projectile effects")]
    [SerializeField] private Transform spellSourceTransform;
    [SerializeField] private GameObject[] SpellProjectilePrefabs;

    [Header("Arrow Projectile effects")]
    [SerializeField] private Transform arrowSourceTransform;
    [SerializeField] private GameObject[] ArrowProjectilePrefabs;

    [Header("Level Up Effect")]
    [SerializeField] private GameObject LevelUpPrefab;
    [SerializeField] private float LevelUpGlowDuration = 3f;

    [Header("Heal Effect")]
    [SerializeField] private GameObject HealPrefab;
    [SerializeField] private float HealDuration = 3f;

    private GameObject magicProjectileInstance;
    private GameObject arrowProjectileInstance;

    private ProjectileMover activeArrowProjectile;
    private ProjectileMover activeMagicProjectile;
    private GameObject container;

    private GameObject levelUpInstance;
    private DelayedDeactivate levelUpDelayedDeactivate;
    private FollowTarget levelUpFollowTarget;
    private GameObject healInstance;
    private DelayedDeactivate healDelayedDeactivate;
    private FollowTarget healFollowTarget;

    private void Start()
    {
        this.container = GameObject.Find("Effects and Projectiles");
        if (!this.container)
        {
            this.container = new GameObject("Effects and Projectiles");
        }

        // Instantiate and disable projectiles initially

    }

    internal void LevelUp()
    {
        // TODO: object pool these instances if we want to get some performance boost.
        try
        {
            if (!transform || transform == null || !this || this == null)
                return;

            if (!levelUpInstance)
            {
                levelUpInstance = Instantiate(LevelUpPrefab, transform.position, Quaternion.identity);
                levelUpInstance.transform.parent = this.container.transform;
                levelUpDelayedDeactivate = levelUpInstance.AddComponent<DelayedDeactivate>();
                levelUpFollowTarget = levelUpInstance.AddComponent<FollowTarget>();
            }

            levelUpFollowTarget.Target = gameObject;
            levelUpDelayedDeactivate.TimeoutSeconds = LevelUpGlowDuration;
            levelUpInstance.SetActive(true);
        }
        catch { }
    }

    internal void Heal()
    {
        try
        {
            if (!HealPrefab) return;
            if (!transform || transform == null || !this || this == null)
                return;

            if (!healInstance)
            {
                healInstance = Instantiate(HealPrefab, transform.position, Quaternion.identity);
                healInstance.transform.parent = this.container.transform;
                healDelayedDeactivate = healInstance.AddComponent<DelayedDeactivate>();
                healFollowTarget = healInstance.AddComponent<FollowTarget>();
            }

            healDelayedDeactivate.TimeoutSeconds = HealDuration;
            healFollowTarget.Target = gameObject;
            healInstance.SetActive(true);
        }
        catch { }
    }

    internal void ShootMagicProjectile(Transform target)
    {
        var direction = target.position - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);

        if (!magicProjectileInstance)
        {
            magicProjectileInstance = Instantiate(SpellProjectilePrefabs[0]);
            magicProjectileInstance.name = this.gameObject.name + " - magic projectile";
            magicProjectileInstance.transform.parent = container.transform;
            magicProjectileInstance.SetActive(false);
            activeMagicProjectile = magicProjectileInstance.GetComponent<ProjectileMover>();
            activeMagicProjectile.RecycleThisObject = true;
        }

        // Reuse magic projectile instance
        magicProjectileInstance.SetActive(false);
        magicProjectileInstance.transform.position = spellSourceTransform.position;
        magicProjectileInstance.transform.rotation = rotation;
        magicProjectileInstance.SetActive(true);

        if (!activeMagicProjectile)
            activeMagicProjectile = magicProjectileInstance.GetComponent<ProjectileMover>();

        activeMagicProjectile.Recycle();
        activeMagicProjectile.TargetTransform = target;
    }

    internal void ShootRangedProjectile(Transform target)
    {
        var direction = target.position - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);

        if (!arrowProjectileInstance)
        {
            arrowProjectileInstance = Instantiate(ArrowProjectilePrefabs[0]);
            arrowProjectileInstance.name = this.gameObject.name + " - arrow projectile";
            arrowProjectileInstance.transform.parent = container.transform;
            arrowProjectileInstance.SetActive(false);
            activeArrowProjectile = arrowProjectileInstance.GetComponent<ProjectileMover>();
            activeArrowProjectile.RecycleThisObject = true;
        }

        // Reuse arrow projectile instance
        arrowProjectileInstance.SetActive(false);
        arrowProjectileInstance.transform.position = arrowSourceTransform.position;
        arrowProjectileInstance.transform.rotation = rotation;
        arrowProjectileInstance.SetActive(true);

        if (!activeArrowProjectile)
            activeArrowProjectile = arrowProjectileInstance.GetComponent<ProjectileMover>();
        activeArrowProjectile.Recycle();
        activeArrowProjectile.TargetTransform = target;
    }

    internal void DestroyProjectile()
    {
        // Instead of destroying, disable the projectiles
        if (activeMagicProjectile)
        {
            activeMagicProjectile.gameObject.SetActive(false);
        }

        if (activeArrowProjectile)
        {
            activeArrowProjectile.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (activeMagicProjectile) Destroy(activeMagicProjectile);
        if (activeArrowProjectile) Destroy(activeArrowProjectile);
    }

}
