using System;
using System.Collections;
using UnityEngine;

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
    private ProjectileMover activeArrowProjectile;
    private ProjectileMover activeMagicProjectile;

    internal void LevelUp()
    {
        // TODO: object pool these instances if we want to get some performance boost.
        try
        {
            if (!transform || transform == null || !this || this == null)
                return;

            var obj = Instantiate(LevelUpPrefab, transform.position, Quaternion.identity);

            obj.AddComponent<TimeoutDestroy>()
                .TimeoutSeconds = LevelUpGlowDuration;

            obj.AddComponent<FollowTarget>()
                .Target = gameObject;
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

            var obj = Instantiate(HealPrefab, transform.position, Quaternion.identity);

            obj.AddComponent<TimeoutDestroy>()
                .TimeoutSeconds = HealDuration;

            obj.AddComponent<FollowTarget>()
                .Target = gameObject;
        }
        catch { }
    }

    internal void ShootMagicProjectile(Transform target)
    {
        var direction = target.position - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        var magicProjectile = Instantiate(SpellProjectilePrefabs[0], spellSourceTransform.position, rotation);
        this.activeMagicProjectile = magicProjectile.GetComponent<ProjectileMover>();
        this.activeMagicProjectile.TargetTransform = target;
    }

    internal void ShootRangedProjectile(Transform target)
    {
        var direction = target.position - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        var arrowProjectile = Instantiate(ArrowProjectilePrefabs[0], arrowSourceTransform.position, rotation);
        this.activeArrowProjectile = arrowProjectile.GetComponent<ProjectileMover>();
        this.activeArrowProjectile.TargetTransform = target;
    }
    internal void DestroyProjectile()
    {
        DestroyArrowProjectile();
        DestroyMagicProjectile();
    }
    internal void DestroyMagicProjectile()
    {
        if (this.activeMagicProjectile && this.activeMagicProjectile.gameObject != null)
        {
            Destroy(this.activeMagicProjectile.gameObject, 0.1f);
        }
    }
    internal void DestroyArrowProjectile()
    {
        if (this.activeArrowProjectile && this.activeArrowProjectile.gameObject != null)
        {
            Destroy(this.activeArrowProjectile.gameObject, 0.1f);
        }
    }
}
