using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class DamageCounterManager : MonoBehaviour
{
    [SerializeField] private GameObject damageCounterPrefab;

    private readonly Stack<DamageCounter> availableDamageCounters
        = new Stack<DamageCounter>();

    private readonly ConcurrentDictionary<int, DamageCounter> damgeCounter = new ConcurrentDictionary<int, DamageCounter>();
    private readonly ConcurrentDictionary<int, int> targetDamage = new ConcurrentDictionary<int, int>();
    private readonly ConcurrentDictionary<int, int> targetHeal = new ConcurrentDictionary<int, int>();

    public void Add(Transform target, int damage, bool isHeal = false, bool allowMerge = false)
    {
        if (!allowMerge)
        {
            AddDamageCounter(target, damage, isHeal);
        }
        else
        {
            AddMergedDamageCounter(target, damage, isHeal);
        }
    }

    private void AddDamageCounter(Transform target, int damage, bool isHeal)
    {
        var id = target.GetInstanceID();
        var dc = GetAvailableDamageCounter();
        dc.Activate(target, damage, isHeal);

        var collider = target.GetComponent<CapsuleCollider>();
        if (collider)
        {
            dc.OffsetY = collider.height * target.localScale.y;
        }

        dc.UpdateBackgroundColor(isHeal);

        targetHeal.TryRemove(id, out _);
        targetDamage.TryRemove(id, out _);
        damgeCounter.TryRemove(id, out _);
    }

    private void AddMergedDamageCounter(Transform target, int damage, bool isHeal)
    {
        var id = target.GetInstanceID();
        int value = 0;
        if (isHeal)
        {
            targetHeal.TryGetValue(id, out value);
            value += damage;
            targetHeal[id] = value;
        }
        else
        {
            targetDamage.TryGetValue(id, out value);
            value += damage;
            targetDamage[id] = value;
        }

        if (!damgeCounter.TryGetValue(id, out var dc) || (dc && dc.FadeOutProgress > 0.5))
        {
            value = damage;

            dc = GetAvailableDamageCounter();
            dc.Activate(target, value, isHeal);

            var collider = target.GetComponent<CapsuleCollider>();
            if (collider)
            {
                dc.OffsetY = collider.height * target.localScale.y;
            }

            targetDamage[id] = value;
            targetHeal[id] = value;
            damgeCounter[id] = dc;
        }

        dc.Damage = value;
        dc.UpdateBackgroundColor(isHeal);
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
        availableDamageCounters.Push(damageCounter);
    }
}
