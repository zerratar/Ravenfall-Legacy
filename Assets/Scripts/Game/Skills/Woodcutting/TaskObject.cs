using UnityEngine;

public abstract class TaskObject : MonoBehaviour, IPollable
{
    private TaskObjectManager manager;

    public virtual void LatePoll() { }

    public abstract void Poll();

    private void Awake()
    {
        if (!manager)
        {
            manager = FindFirstObjectByType<TaskObjectManager>();
        }

        manager.Register(this);
    }
}
