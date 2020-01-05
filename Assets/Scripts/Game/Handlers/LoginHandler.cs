using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_InputField txtUsername;
    [SerializeField] private TMP_InputField txtPassword;

    [SerializeField] private TextMeshProUGUI loginBtnText;
    [SerializeField] private TextMeshProUGUI invalidUsername;
    [SerializeField] private Toggle rememberMeToggle;
    //[SerializeField] private LoadingHandler loading;

    void Start()
    {
        invalidUsername.gameObject.SetActive(false);
        var savedUsername = PlayerPrefs.GetString("LoginUsername", string.Empty);
        if (string.IsNullOrEmpty(savedUsername))
        {
            txtUsername.Select();
        }
        else
        {
            txtUsername.text = savedUsername;
            txtPassword.Select();
        }
    }

    public async void Login()
    {
        loginBtnText.text = "LOGGING IN...";
        if (txtUsername.text.Length == 0 || txtPassword.text.Length == 0) return;
        if (await gameManager.RavenNestLoginAsync(txtUsername.text, txtPassword.text))
        {
            gameObject.SetActive(false);
            invalidUsername.enabled = false;

            if (rememberMeToggle && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("LoginUsername", txtUsername.text);
            }
        }
        else
        {
            invalidUsername.gameObject.SetActive(true);
            invalidUsername.enabled = true;
            loginBtnText.text = "LOGIN";
        }
    }
}
