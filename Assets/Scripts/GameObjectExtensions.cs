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

}