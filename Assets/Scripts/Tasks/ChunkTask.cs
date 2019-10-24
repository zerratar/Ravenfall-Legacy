using UnityEngine;

public abstract class ChunkTask
{
    public abstract bool IsCompleted(PlayerController player, Transform target);

    public abstract Transform GetTarget(PlayerController player);

    public abstract bool Execute(PlayerController player, Transform target);

    public abstract bool CanExecute(PlayerController player, Transform target, out TaskExecutionStatus reason);    

    public virtual void TargetAcquired(PlayerController player, Transform target)
    {
    }
}