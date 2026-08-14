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
            SetHeader(CodeOfConduct.Title ?? "Code of Conduct");
            SetMessage(CodeOfConduct.Message);
            SetVersion("Version " + CodeOfConduct.Revision);
            SetLastModified("Last Modified " + CodeOfConduct.LastModified);
        }

        if (Ravenfall.isBatchMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

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
