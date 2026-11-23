using UnityEditor;
using UnityEngine;

/// <summary>
///     The April 1st hat fitter: shows the party hat on the model (in the prefab editor or the scene), lets it be moved
///     by hand, and writes where it sits back into the HatFit.
/// </summary>
[CustomEditor(typeof(HatFit))]
public sealed class HatFitEditor : Editor
{
    private const string PreviewName = "HatFit Preview (not saved)";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var fit = (HatFit)target;
        EditorGUILayout.Space();

        var preview = FindPreview(fit);
        if (!preview)
        {
            using (new EditorGUI.DisabledScope(fit.NoHat))
                if (GUILayout.Button("Show hat preview", GUILayout.Height(26))) ShowPreview(fit, false);
            if (GUILayout.Button("Show traffic cone preview")) ShowPreview(fit, true);
            return;
        }

        EditorGUILayout.HelpBox("Move, turn and scale the preview hat in the scene view, then take its values. The preview is never saved.", MessageType.Info);
        if (GUILayout.Button("Select preview hat")) Selection.activeGameObject = preview.gameObject;
        if (GUILayout.Button("Take values from preview", GUILayout.Height(26)))
        {
            Undo.RecordObject(fit, "Hat fit");
            fit.TakeFrom(preview, TemplateScale(preview.name.Contains("Cone")));
            EditorUtility.SetDirty(fit);
            Object.DestroyImmediate(preview.gameObject);
            Selection.activeGameObject = fit.gameObject;
        }
        if (GUILayout.Button("Remove preview")) Object.DestroyImmediate(preview.gameObject);
    }

    private static Transform FindPreview(HatFit fit)
    {
        foreach (var t in fit.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(PreviewName)) return t;
        return null;
    }

    private static GameObject Template(bool cone)
    {
        var gags = Resources.Load<GameObject>(SillySurprises.WorldGagsResource);
        if (!gags) return null;
        var t = gags.transform.Find(cone ? "Templates/Cone" : "Templates/Party Hat");
        return t ? t.gameObject : null;
    }

    private static Vector3 TemplateScale(bool cone)
    {
        var t = Template(cone);
        return t ? t.transform.lossyScale : Vector3.one;
    }

    private static void ShowPreview(HatFit fit, bool cone)
    {
        var template = Template(cone);
        if (!template)
        {
            EditorUtility.DisplayDialog("Hat Fit", "The hats live in Resources/" + SillySurprises.WorldGagsResource + ".prefab, which is missing on this machine. Run tools/easter-eggs/world-gags.cs once to make it.", "OK");
            return;
        }
        var hat = Object.Instantiate(template);
        hat.name = PreviewName + (cone ? " Cone" : " Hat");
        hat.SetActive(true);
        foreach (var t in hat.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
        fit.Place(hat.transform, template.transform.lossyScale);
        Selection.activeGameObject = hat;
        SceneView.lastActiveSceneView?.FrameSelected();
    }
}
