using UnityEngine;

public class ObjectManager<T> : MonoBehaviour
    where T : IPollable
{
    private const int MaxItemCount = 1_000;
    protected int itemCount;
    private T[] items = new T[MaxItemCount];

    public void Register(T item)
    {
        if (itemCount + 1 >= MaxItemCount)
        {
            throw new System.Exception("Max amount of items reached!!!");
        }

        items[itemCount++] = item;
    }

    // Update is called once per frame
    void Update()
    {
        for (var i = 0; i < itemCount; ++i)
        {
            items[i].Poll();
        }
    }

    void LateUpdate()
    {
        for (var i = 0; i < itemCount; ++i)
        {
            items[i].LatePoll();
        }
    }
}
