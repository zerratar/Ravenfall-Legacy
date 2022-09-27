using Shinobytes.Linq;
using System.Collections.Generic;
using UnityEngine;
using Debug = Shinobytes.Debug;

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
            Release(leased);
        }

        if (DestroyOnReturn)
        {
            return;
        }

        pool.Clear();
        foreach (var enemy in GetComponentsInChildren<EnemyController>(true))
        {
            pool.Push(enemy);
            enemy.gameObject.SetActive(false);
        }
    }

    protected override EnemyController OnLeasedFromPool(EnemyController component)
    {
        // Do any reset necessary as well or preperations before we can use this enemy.
        // then activate the component
        leasedEnemies.Add(component);

        component.Lock();
        component.ResetState();

        return component;
    }

    protected override EnemyController OnReturnedToPool(EnemyController component)
    {
        // Reset enemy status, etc. Move it into the pool container if it isnt already and 
        // disable the component.
        leasedEnemies.Remove(component);

        component.Lock();
        component.ResetState();

        component.transform.SetParent(objectContainer);
        component.transform.localPosition = Vector3.zero;
        component.gameObject.SetActive(false);

        return component;
    }
}

public abstract class ComponentPool<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private GameObject[] poolableObjects;
    [SerializeField] protected Transform objectContainer;
    protected readonly Stack<T> pool = new Stack<T>();

    protected abstract T OnLeasedFromPool(T component);
    protected abstract T OnReturnedToPool(T component);

    public bool DestroyOnReturn;

    public T Get()
    {
        if (!DestroyOnReturn && pool.Count > 0)
        {
            return OnLeasedFromPool(pool.Pop());
        }

        return OnLeasedFromPool(CreateInstance());
    }

    public void Release(T comp)
    {
        if (DestroyOnReturn)
        {
            Destroy(OnReturnedToPool(comp).gameObject);
            return;
        }

        pool.Push(OnReturnedToPool(comp));
    }

    private T CreateInstance()
    {

        var obj = Instantiate(poolableObjects.Random(), objectContainer);
        var comp = obj.GetComponent<T>();
        if (!comp)
        {
            Debug.LogError($"The expected component {typeof(T).Name} could not be found on the poolable object.");
            return null;
        }

        return comp;
    }

}
