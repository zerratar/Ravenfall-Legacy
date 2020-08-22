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

    internal void LevelUp()
    {
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

    internal void ShootMagicProjectile(Vector3 target)
    {
        var direction = target - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        Instantiate(SpellProjectilePrefabs[0], spellSourceTransform.position, rotation);
    }

    internal void ShootRangedProjectile(Vector3 target)
    {
        var direction = target - gameObject.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        Instantiate(ArrowProjectilePrefabs[0], arrowSourceTransform.position, rotation);
    }
}
