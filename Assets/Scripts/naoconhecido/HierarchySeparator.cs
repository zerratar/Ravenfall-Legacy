using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;


[InitializeOnLoad]
#endif
public class HierarchySeparator : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private Color m_OutlineColor = Color.black;
    public Color OutlineColor
    {
        get => m_OutlineColor;
        set
        {
            value.a = 1f;
            m_OutlineColor = value;
        }
    }

    [HideInInspector]
    [SerializeField]
    private Color m_BarColor = Color.black;
    public Color BarColor
    {
        get => m_BarColor;
        set
        {
            value.a = 1f;
            m_BarColor = value;
        }
    }

    [HideInInspector]
    [SerializeField]
    private Color m_TextColor = Color.white;
    public Color TextColor
    {
        get => m_TextColor;
        set
        {
            value.a = 1f;
            m_TextColor = value;
        }
    }

    [HideInInspector]
    [SerializeField]
    private int m_OutlineSize = 0;
    public int OutlineSize
    {
        get => m_OutlineSize;
        set
        {
            m_OutlineSize = value;
        }
    }

#if UNITY_EDITOR

    static HierarchySeparator()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
    }

    [MenuItem("GameObject/Separator", false, 30)]
    public static void CreateSeparator(MenuCommand menuCommand)
    {
        GameObject separator = new GameObject("Separator");
        separator.AddComponent<HierarchySeparator>();
        GameObjectUtility.SetParentAndAlign(separator, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(separator, "Create " + separator.name);
        Selection.activeObject = separator;
    }

    static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        try
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null) return;
            if (!gameObject.TryGetComponent(out HierarchySeparator hierarchy)) return;

            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.normal.textColor = hierarchy.TextColor;
            guiStyle.alignment = TextAnchor.MiddleCenter;

            UnityEngine.Random.InitState(instanceID);

            var targetColor = Random.ColorHSV();
            targetColor.a = 1f;

            if (Selection.activeObject && Selection.activeObject.GetInstanceID() == gameObject.GetInstanceID())
            {
                Color.RGBToHSV(targetColor, out var h, out var s, out var v);
                v *= 0.5f;
                targetColor = Color.HSVToRGB(h, s, v);
                EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height), targetColor);
                EditorGUI.DropShadowLabel(selectionRect, $"{gameObject.name.ToUpperInvariant()}", guiStyle);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height), targetColor);
                EditorGUI.DropShadowLabel(selectionRect, $"{gameObject.name.ToUpperInvariant()}", guiStyle);
            }
        }
        catch (System.Exception)
        {

        }
    }

    void OnValidate()
    {
        EditorApplication.RepaintHierarchyWindow();
    }
#endif
}