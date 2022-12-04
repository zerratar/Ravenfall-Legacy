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

    public static RavenNest.Models.CodeOfConduct CodeOfConduct;

    public void Awake()
    {
        if (CodeOfConduct != null)
        {
            lblHeader.text = CodeOfConduct.Title ?? "Code of Conduct";
            lblMessage.text = CodeOfConduct.Message;
            lblVersion.text = "Version " + CodeOfConduct.Revision;
            lblModified.text = "Last Modified " + CodeOfConduct.LastModified;
        }

        if (Application.isBatchMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
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
