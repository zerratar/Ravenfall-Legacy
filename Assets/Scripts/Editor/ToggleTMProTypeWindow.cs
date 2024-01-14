using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ToggleTMProTypeWindow : OdinEditorWindow
{
    [MenuItem("Ravenfall/Tools/Toggle TMPro")]
    public static void ShowWindow()
    {
        var editor = GetWindow<ToggleTMProTypeWindow>();
        editor.Show();
    }

    public GameObject Source;
    public class PropertyInstance
    {
        public System.Reflection.PropertyInfo Property;
        public object PropertyValue;
    }

    private Dictionary<string, PropertyInstance> GetProperties(Type type, bool includeParent, params string[] propertiesToIgnore)
    {
        var props = type
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
            .ToList();

        var names = props.Select(x => new { Name = x.Name, Property = x });
        props = names.GroupBy(x => x.Name).Select(x => x.FirstOrDefault().Property).ToList();

        var thisDictionary = props.Where(x => !propertiesToIgnore.Contains(x.Name)).ToDictionary(p => p.Name, x => new PropertyInstance { Property = x });
        if (includeParent)
        {
            var parentDictionary = GetProperties(type.BaseType, false, propertiesToIgnore);

            foreach (var prop in parentDictionary)
            {
                thisDictionary.TryAdd(prop.Key, prop.Value);
            }
        }

        return thisDictionary;
    }

    private Dictionary<string, PropertyInstance> GetProperties<T>(bool includeParent, params string[] propertiesToIgnore)
    {
        return GetProperties(typeof(T), includeParent, propertiesToIgnore);
    }

    public void ReplaceComponents<TOld, TNew>(GameObject hierarchy, bool includeParent, params string[] propertiesToIgnore)
        where TOld : UnityEngine.MonoBehaviour
        where TNew : UnityEngine.MonoBehaviour
    {
        var components = hierarchy.transform.GetComponentsInChildren<TOld>();
        var srcFields = GetProperties<TOld>(includeParent, propertiesToIgnore);
        var dstFields = GetProperties<TNew>(includeParent, propertiesToIgnore);

        var objects = components.Select(x => x.transform.gameObject).ToList();

        // get property values
        foreach (var c in components)
        {
            foreach (var f in srcFields)
            {
                // some properties will throw an exception when accessed.
                try
                {
                    if (f.Value.Property.CanRead)
                        f.Value.PropertyValue = f.Value.Property.GetValue(c);
                }
                catch
                {
                    // ignored
                }
            }

            GameObject.DestroyImmediate(c);
        }

        // set property values
        foreach (var c in objects)
        {
            var newObj = c.AddComponent<TNew>();

            foreach (var f in srcFields)
            {
                if (dstFields.TryGetValue(f.Key, out var dst))
                {
                    try
                    {
                        if (dst.Property.CanWrite && f.Value.PropertyValue != null)
                            dst.Property.SetValue(newObj, dst.PropertyValue);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            //if (newObj is TMPro.TextMeshProUGUI ugui)
            //{
            //    if (srcFields.TryGetValue("text", out var txt))
            //    {
            //        ugui.text = txt.PropertyValue?.ToString();
            //    }
            //}

            //if (newObj is TMPro.TextMeshPro pro)
            //{
            //    if (srcFields.TryGetValue("text", out var txt))
            //    {
            //        pro.text = txt.PropertyValue?.ToString();
            //    }
            //}
        }
    }

    public void AddToComponents<TFind, TAdd>(GameObject hierarchy)
      where TFind : UnityEngine.Component
      where TAdd : UnityEngine.Component
    {
        var components = hierarchy.transform.GetComponentsInChildren<TFind>();
        foreach (var c in components)
        {
            var existing = c.transform.gameObject.GetComponent<TAdd>();
            if (existing)
            {
                continue;
            }

            c.transform.gameObject.AddComponent<TAdd>();
        }
    }
    public void DestroyComponents<TFind, TDestroy>(GameObject hierarchy)
      where TFind : UnityEngine.Component
      where TDestroy : UnityEngine.Component
    {
        var components = hierarchy.transform.GetComponentsInChildren<TFind>();
        foreach (var c in components)
        {
            var existing = c.transform.gameObject.GetComponent<TDestroy>();
            if (existing)
            {
                GameObject.DestroyImmediate(existing);
            }
        }
    }


    [ShowIf("Source")]
    [Button("Make into TMPro Text Mesh GUI (UI)")]
    public void ChangeToUI()
    {

        var meshRenderer = Source.GetComponent<MeshRenderer>();
        var old = Source.GetComponent<TMPro.TextMeshPro>();

        var text = old.text;
        var style = old.textStyle;
        var size = old.fontSize;
        var color = old.color;
        var alignment = old.alignment;
        var horAlign = old.horizontalAlignment;
        var vertAlign = old.verticalAlignment;

        var wrap = old.textWrappingMode;

        GameObject.DestroyImmediate(old);
        GameObject.DestroyImmediate(meshRenderer);

        //var canvasRenderer = Source.AddComponent<CanvasRenderer>();
        var newText = Source.AddComponent<TMPro.TextMeshProUGUI>();

        newText.text = text;
        newText.textStyle = style;
        newText.fontSize = size / 10f;
        newText.color = color;
        newText.alignment = alignment;
        newText.horizontalAlignment = horAlign;
        newText.verticalAlignment = vertAlign;
        newText.textWrappingMode = wrap;

        //AddToComponents<TMPro.TextMeshPro, CanvasRenderer>(Source);
        //ReplaceComponents<TMPro.TextMeshPro, TMPro.TextMeshProUGUI>(Source, true, "renderer", "mesh", "meshfilter", "name", "tag", "layer");
        //DestroyComponents<TMPro.TextMeshProUGUI, MeshRenderer>(Source);
    }

    //[ShowIf("Source")]
    //[Button("Make into TMPro Text Mesh (3D)")]
    //public void ChangeTo3D()
    //{
    //    ReplaceComponents<TMPro.TextMeshProUGUI, TMPro.TextMeshPro>(Source, "renderer");
    //}
}
