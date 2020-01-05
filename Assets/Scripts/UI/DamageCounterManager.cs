using UnityEngine;

public class DamageCounterManager : MonoBehaviour
{
    [SerializeField] private GameObject damageCounterPrefab;

    public void Add(Transform target, int damage)
    {
        var counter = Instantiate(damageCounterPrefab, transform);
        var dc = counter.GetComponent<DamageCounter>();
        dc.Damage = damage;
        dc.Target = target;

        var collider = target.GetComponent<CapsuleCollider>();
        if (collider)
        {
            dc.OffsetY = collider.height * target.localScale.y;
        }
    }
}
