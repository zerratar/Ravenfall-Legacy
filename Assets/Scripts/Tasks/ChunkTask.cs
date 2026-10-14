using System;
using System.Collections.Concurrent;
using UnityEngine;
public abstract class ChunkTask
{
    // by the target itself: plain targets such as ResourceTarget have no entity id
    private readonly ConcurrentDictionary<object, bool> targetLookup = new ConcurrentDictionary<object, bool>();
    public abstract bool IsCompleted(PlayerController player, object target);

    public abstract bool Execute(PlayerController player, object target);

    public abstract object GetTarget(PlayerController player);

    public abstract bool CanExecute(PlayerController player, object target, out TaskExecutionStatus reason);

    public virtual void TargetAcquired(PlayerController player, object target)
    {
    }
    internal bool TargetExists(object target)
    {
        if (target == null) return false;
        if (targetLookup.TryGetValue(target, out var result))
        {
            return result;
        }

        result = TargetExistsImpl(target);
        targetLookup[target] = result;
        return result;
    }

    internal abstract bool TargetExistsImpl(object target);

    internal EntityId GetTargetEntityId(object target)
    {
        if (target == null) return default;
        if (target is MonoBehaviour behaviour)
        {
            return behaviour.GetEntityId();
        }
        if (target is Transform transform)
        {
            return transform.GetEntityId();
        }
        return default;
    }

    internal abstract void SetTargetInvalid(object taskTarget);
}