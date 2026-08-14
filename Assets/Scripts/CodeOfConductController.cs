using TMPro;

using UnityEngine;

public class CodeOfConductController : MonoBehaviour
{
    public const string CoCLastAcceptedVersion_SettingsName = "coc_last_accepted_version";
    public const int CoCLastAcceptedVersion_DefaultValue = -1;

    [SerializeField] private TextMeshProUGUI lblHeader;
    [SerializeField] private TextMeshProUGUI lblMessage;
    [SerializeField] private TextMeshProUGUI lblVersion;
    [SerializeField] private TextMeshProUGUI lblModified;

    /// <summary>
    /// Optional UI Toolkit replacement for the labels above. When assigned, all output goes there
    /// and the buttons are wired from code; the uGUI hierarchy stays in the scene and keeps working
    /// so this can be reverted by clearing the field. See docs/ui-toolkit-migration.md.
    /// </summary>
    [SerializeField] private MonoBehaviour uiToolkitScreen;

    public static RavenNest.Models.CodeOfConduct CodeOfConduct;

    private Shinobytes.UI.ICodeOfConductScreen screen;

    public void Awake()
    {
        screen = uiToolkitScreen as Shinobytes.UI.ICodeOfConductScreen;

        if (screen != null)
        {
            // The old screen wired these through UnityEvents on the buttons in the scene. The UI
            // Toolkit one has no scene wiring, so the actions are handed over here.
            screen.BindActions(Accept, Decline);
        }

        if (CodeOfConduct != null)
        {
            Populate();
        }
#if UNITY_EDITOR
        else if (Application.isPlaying && !Ravenfall.isBatchMode)
        {
            // Playing this scene on its own means GameUpdater never ran, so the static is null and
            // the screen would sit empty. Editor only: fetch the live document so the screen can be
            // checked against real content rather than against nothing.
            StartCoroutine(FetchForPreview());
        }
#endif

        if (Ravenfall.isBatchMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    private void Populate()
    {
        SetHeader(CodeOfConduct.Title ?? "Code of Conduct");
        SetMessage(CodeOfConduct.Message);
        SetVersion("Version " + CodeOfConduct.Revision);
        SetLastModified("Last Modified " + CodeOfConduct.LastModified);
    }

#if UNITY_EDITOR
    private System.Collections.IEnumerator FetchForPreview()
    {
        SetHeader("Code of Conduct");
        SetMessage("Loading the current code of conduct from the server...");

        const string url = "https://www.ravenfall.stream/api/version/check";
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                SetMessage("Preview fetch failed: " + req.error +
                           "\r\n\r\nThis only affects previewing the scene on its own. The real " +
                           "flow receives the document from GameUpdater.");
                yield break;
            }

            RavenNest.Models.CodeOfConduct coc = null;
            try
            {
                var data = Newtonsoft.Json.JsonConvert
                    .DeserializeObject<PreviewUpdateData>(req.downloadHandler.text);
                coc = data?.CodeOfConduct;
            }
            catch (System.Exception exc)
            {
                SetMessage("Preview parse failed: " + exc.Message);
                yield break;
            }

            if (coc == null)
            {
                SetMessage("Server returned no code of conduct.");
                yield break;
            }

            CodeOfConduct = coc;
            Populate();
        }
    }

    /// <summary>Only the part of the update payload the preview needs.</summary>
    private class PreviewUpdateData
    {
        [Newtonsoft.Json.JsonProperty("codeOfConduct")]
        public RavenNest.Models.CodeOfConduct CodeOfConduct { get; set; }
    }
#endif

    private void SetHeader(string value)
    {
        if (screen != null) { screen.Header = value; return; }
        if (lblHeader) lblHeader.text = value;
    }

    private void SetMessage(string value)
    {
        if (screen != null) { screen.Message = value; return; }
        if (lblMessage) lblMessage.text = value;
    }

    private void SetVersion(string value)
    {
        if (screen != null) { screen.Version = value; return; }
        if (lblVersion) lblVersion.text = value;
    }

    private void SetLastModified(string value)
    {
        if (screen != null) { screen.LastModified = value; return; }
        if (lblModified) lblModified.text = value;
    }

    public void Accept()
    {
        if (CodeOfConduct != null)
        {
            PlayerPrefs.SetInt(CodeOfConductController.CoCLastAcceptedVersion_SettingsName, CodeOfConduct.Revision);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    public void Decline()
    {
        Application.Quit();
    }
}
