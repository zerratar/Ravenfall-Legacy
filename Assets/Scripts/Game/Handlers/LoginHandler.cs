using System.Collections;
using System.Threading.Tasks;
using RavenNest.SDK;
using Shinobytes.UI;
using UnityEngine;

/// <summary>
/// Signing in: a remembered account or an autologin file signs in on its own, otherwise the
/// login screen asks. The screen itself is <see cref="LoginScreenView"/>; this keeps what it was
/// typed, what to remember and when to try.
/// </summary>
public class LoginHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LoginScreenView screen;

    const string AutoLoginFile = "autologin.conf";
    const string TmpAutoLoginFile = "tmpautologin.conf";

    private string username = string.Empty;
    private string password = string.Empty;
    private bool rememberMe;
    private bool loggingIn;

    private LoginScreenView Screen => screen ? screen : (screen = FindAnyObjectByType<LoginScreenView>(FindObjectsInactive.Include));

    void Start()
    {
        if (Ravenfall.isBatchMode)
        {
            Shinobytes.Debug.Log("Login handler started");
        }

        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (Screen) Screen.Submitted += OnSubmitted;

        var savedUsername = PlayerPrefs.GetString("LoginUsername", string.Empty);
        var savedPassword = PlayerPrefs.GetString("LoginPassword", string.Empty);

        // an account that was remembered stays remembered unless the box is cleared
        rememberMe = !string.IsNullOrEmpty(savedPassword);
        username = savedUsername;
        if (!string.IsNullOrEmpty(savedUsername) && !string.IsNullOrEmpty(savedPassword))
        {
            password = savedPassword;
            FillScreen();
            StartCoroutine(DelayedLogin());
            return;
        }

        FillScreen();
        HandleAutoLogin();
    }

    private void OnDestroy()
    {
        if (screen) screen.Submitted -= OnSubmitted;
    }

    private void OnSubmitted()
    {
        username = Screen.Username?.Trim() ?? string.Empty;
        password = Screen.Password ?? string.Empty;
        rememberMe = Screen.RememberMe;
        Login();
    }

    public async void Login()
    {
        await LoginImplementation();
    }

    public void ClearPassword()
    {
        PlayerPrefs.SetString("LoginUsername", null);
        PlayerPrefs.SetString("LoginPassword", null);
        username = string.Empty;
        password = string.Empty;
        rememberMe = false;
        FillScreen();
    }

    private async Task LoginImplementation()
    {
        if (loggingIn)
        {
            return;
        }

        if (Ravenfall.isBatchMode)
        {
            Shinobytes.Debug.Log("Attempting to login...");
        }

        if (username.Length == 0 || password.Length == 0)
        {
            if (Ravenfall.isBatchMode)
                Shinobytes.Debug.Log("User/pass not set, login cannot proceed.");
            return;
        }

        loggingIn = true;
        if (Screen) Screen.SetBusy(true);
        try
        {
            if (await gameManager.RavenNestLoginAsync(username, password))
            {
                if (rememberMe)
                {
                    PlayerPrefs.SetString("LoginUsername", username);
                    PlayerPrefs.SetString("LoginPassword", password);
                }
                else
                {
                    // unticking the box forgets an account that was remembered before
                    PlayerPrefs.DeleteKey("LoginUsername");
                    PlayerPrefs.DeleteKey("LoginPassword");
                }

                HideView();
                return;
            }

            if (Screen)
            {
                var failure = gameManager.RavenNest != null ? gameManager.RavenNest.LastLoginFailure : LoginFailure.Unreachable;
                Screen.ShowFailure(failure == LoginFailure.None ? LoginFailure.WrongCredentials : failure);
            }
        }
        finally
        {
            loggingIn = false;
            if (Screen) Screen.SetBusy(false);
        }
    }

    public void ActivateTempAutoLogin()
    {
        Shinobytes.IO.File.WriteAllLines(TmpAutoLoginFile, new string[] {
            "user=" + username,
            "pass=" + password
        });
    }

    private void HandleAutoLogin()
    {
        if (!Shinobytes.IO.File.Exists(AutoLoginFile) && !Shinobytes.IO.File.Exists(TmpAutoLoginFile))
        {
            if (Ravenfall.isBatchMode)
                Shinobytes.Debug.Log("autologin.conf not present, automatic login has been cancelled.");
            return;
        }

        if (Ravenfall.isBatchMode)
        {
            Shinobytes.Debug.Log("HandleAutoLogin");
        }

        if (Shinobytes.IO.File.Exists(TmpAutoLoginFile))
        {
            AutoLogin(TmpAutoLoginFile);
            Shinobytes.IO.File.Delete(TmpAutoLoginFile);
            return;
        }

        AutoLogin(AutoLoginFile);
    }

    private void AutoLogin(string AutoLoginFile)
    {
        if (Ravenfall.isBatchMode)
        {
            Shinobytes.Debug.Log("AutoLogin " + AutoLoginFile);
        }

        var loginInfo = ParseAutoLoginConfig(Shinobytes.IO.File.ReadAllLines(AutoLoginFile));
        if (string.IsNullOrEmpty(loginInfo.Username) || string.IsNullOrEmpty(loginInfo.Password))
        {
            if (Ravenfall.isBatchMode)
                Shinobytes.Debug.Log("login config file does not include both Username and Password. Login cannot proceed.");
            return;
        }

        username = loginInfo.Username;
        password = loginInfo.Password;
        FillScreen();

        if (Ravenfall.isBatchMode)
        {
            Login();
            return;
        }

        StartCoroutine(DelayedLogin());
    }

    private IEnumerator DelayedLogin()
    {
        if (Screen) Screen.SetBusy(true);
        yield return new WaitForSeconds(1f);
        Login();
    }

    private void FillScreen()
    {
        if (Screen) Screen.Fill(username, password, rememberMe);
    }

    private AutoLoginInfo ParseAutoLoginConfig(string[] vs)
    {
        var user = "";
        var pass = "";

        for (var i = 0; i < vs.Length; ++i)
        {
            var line = vs[i]?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.IndexOf('=') > 0)
            {
                var data = line.Split('=');
                var key = data[0];
                var value = data[1];
                if (key.ToLower().Trim().IndexOf("pass") >= 0)
                {
                    pass = value.Trim();
                }
                else if (key.ToLower().Trim().IndexOf("user") >= 0)
                {
                    user = value.Trim();
                }
            }
            else
            {
                if (i == 0) user = vs[i]?.Trim();
                if (i == 1) pass = vs[i]?.Trim();
            }
        }

        return new AutoLoginInfo { Username = user, Password = pass };
    }

    internal void ShowView()
    {
        if (Screen) Screen.Show();
    }

    internal void HideView()
    {
        if (Screen) Screen.Hide();
    }

    private struct AutoLoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
