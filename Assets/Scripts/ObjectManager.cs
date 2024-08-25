using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectManager<T> : MonoBehaviour
    where T : MonoBehaviour, IPollable
{
    private const int MaxItemCount = 1_000;
    //protected int itemCount;
    //private T[] items = new T[MaxItemCount];
    private List<T> items = new List<T>(MaxItemCount);
    private readonly object objMutex = new object();

    public void Register(T item)
    {
        lock (objMutex)
        {
            if (items.Count + 1 + 1 >= MaxItemCount)
            {
                throw new System.Exception("Max amount of items reached!!! This should never happen!!");
            }

            items.Add(item);
        }
    }

    public void Unregister(int index)
    {
        lock (objMutex)
        {
            items.RemoveAt(index);
        }
    }

    // Update is called once per frame
    void Update()
    {
        lock (objMutex)
        {
            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item != null && item)
                {
                    items[i].Poll();
                }
                else
                {
                    // if the item has been destroyed, remove it from the list, its okay if we stop execution of the rest of the items here.
                    Unregister(i);
                    return;
                }
            }
        }
    }

    void LateUpdate()
    {
        lock (objMutex)
        {
            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item != null && item)
                {
                    items[i].LatePoll();
                }
                else
                {
                    // if the item has been destroyed, remove it from the list, its okay if we stop execution of the rest of the items here.
                    Unregister(i);
                    return;
                }
            }
        }
    }
}
