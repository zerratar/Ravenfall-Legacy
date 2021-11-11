using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : ComponentPool<EnemyController>
{
    private readonly List<EnemyController> leasedEnemies = new List<EnemyController>();
    public bool HasLeasedEnemies => leasedEnemies.Count > 0;
    public IReadOnlyList<EnemyController> GetLeasedEnemies()
    {
        return leasedEnemies;
    }
    public void ReturnAll()
    {
        foreach (var leased in leasedEnemies.ToArray())
        {
            Return(leased);
        }
    }
    protected override EnemyController OnLeasedFromPool(EnemyController component)
    {
        // Do any reset necessary as well or preperations before we can use this enemy.
        // then activate the component
        leasedEnemies.Add(component);

        component.Lock();

        return component;
    }
    protected override EnemyController OnReturnedToPool(EnemyController component)
    {
        // Reset enemy status, etc. Move it into the pool container if it isnt already and 
        // disable the component.
        leasedEnemies.Remove(component);

        component.Lock();
        component.transform.localPosition = Vector3.zero;
        component.gameObject.SetActive(false);

        return component;
    }
}

public abstract class ComponentPool<T> : MonoBehaviour where T : Component
{
    [SerializeField] private GameObject[] poolableObjects;
    [SerializeField] private Transform objectContainer;
    private readonly Stack<T> objects = new Stack<T>();

    protected abstract T OnLeasedFromPool(T component);
    protected abstract T OnReturnedToPool(T component);

    public T Lease()
    {
        if (objects.Count > 0)
        {
            return OnLeasedFromPool(objects.Pop());
        }

        var obj = Instantiate(poolableObjects.Random(), objectContainer);
        var comp = obj.GetComponent<T>();
        if (!comp)
        {
            UnityEngine.Debug.LogError($"The expected component {typeof(T).Name} could not be found on the poolable object.");
            return null;
        }

        return OnLeasedFromPool(comp);
    }

    public void Return(T comp)
    {
        objects.Push(OnReturnedToPool(comp));
    }
}

