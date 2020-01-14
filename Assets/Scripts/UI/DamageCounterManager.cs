using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageCounterManager : MonoBehaviour
{
    [SerializeField] private GameObject damageCounterPrefab;

    private readonly Stack<DamageCounter> availableDamageCounters
        = new Stack<DamageCounter>();

    public DamageCounter Add(Transform target, int damage)
    {
        var dc = GetAvailableDamageCounter();

        dc.Activate(target, damage);

        var collider = target.GetComponent<CapsuleCollider>();
        if (collider)
        {
            dc.OffsetY = collider.height * target.localScale.y;
        }

        return dc;
    }

    private DamageCounter GetAvailableDamageCounter()
    {
        DamageCounter dc;

        if (availableDamageCounters.Count > 0)
        {
            dc = availableDamageCounters.Pop();
        }
        else
        {
            var counter = Instantiate(damageCounterPrefab, transform);
            dc = counter.GetComponent<DamageCounter>();
            dc.Manager = this;
        }

        return dc;
    }

    internal void Return(DamageCounter damageCounter)
    {
        damageCounter.gameObject.SetActive(false);
        availableDamageCounters.Push(damageCounter);
    }
}
