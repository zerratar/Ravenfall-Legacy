using System;
using UnityEngine;

public static class GameObjectExtensions
{
    public static T EnsureComponent<T>(this GameObject go, Action<T> onAddOrGet = null) where T : Component
    {
        var c = go.GetComponent<T>();
        if (!c)
        {
            c = go.AddComponent<T>();
        }

        if (onAddOrGet != null)
        {
            onAddOrGet(c);
        }
        return c;
    }


    public static string GetHierarchyPath(this MonoBehaviour obj)
    {
        return obj.transform.GetHierarchyPath();
    }

    public static string GetHierarchyPath(this GameObject obj)
    {
        return obj.transform.GetHierarchyPath();
    }

    public static string GetHierarchyPath(this Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

}