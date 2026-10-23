using System;
using System.Collections;
using System.Threading.Tasks;
using RavenNest.SDK;
using Shinobytes.UI;
using UnityEngine;

/// <summary>
/// Signing in: with Twitch through the website, or with a password; on its own with a saved
/// device token or an autologin file. The screen itself is <see cref="LoginScreenView"/>; this
/// keeps what was typed, what to remember and when to try.
/// </summary>
/// <remarks>
/// Remember me keeps a device token (<see cref="DeviceTokenStore"/>), never the password. A
/// password saved by an older version is used once more and then swapped for a token. A Twitch
/// sign in is always remembered: signing in again without typing anything is the point of it.
/// Logging out forgets the token here and on the server.
/// </remarks>
public class LoginHandler : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LoginScreenView screen;
    [Tooltip("Seconds between asking the server whether a Twitch sign in was approved.")]
    [SerializeField] private float pollSeconds = 2f;
    [Tooltip("Seconds between tries of a saved sign in while the server cannot be reached.")]
    [SerializeField] private float retrySeconds = 10f;

    const string AutoLoginFile = "autologin.conf";
    const string TmpAutoLoginFile = "tmpautologin.conf";
    const string LegacyUsernameKey = "LoginUsername";
    const string LegacyPasswordKey = "LoginPassword";

    private string username = string.Empty;
    private string password = string.Empty;
    private string deviceToken;
    private bool rememberMe;
    private bool loggingIn;
    private int twitchAttempt;

    private LoginScreenView Screen => screen ? screen : (screen = FindAnyObjectByType<LoginScreenView>(FindObjectsInactive.Include));

    void Start()
    {
        if (Ravenfall.isBatchMode)
        {
            Shinobytes.Debug.Log("Login handler started");
        }

        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (Screen)
        {
            Screen.Submitted += OnSubmitted;
            Screen.TwitchRequested += OnTwitchRequested;
            Screen.TwitchCancelled += CancelTwitch;
        }

        var savedUsername = PlayerPrefs.GetString(LegacyUsernameKey, string.Empty);
        var savedPassword = PlayerPrefs.GetString(LegacyPasswordKey, string.Empty);
        username = savedUsername;

        // a saved sign in for this computer comes first
        var saved = DeviceTokenStore.Load();
        if (!string.IsNullOrEmpty(saved))
        {
            rememberMe = true;
            FillScreen();
            StartCoroutine(DelayedDeviceLogin(saved));
            return;
        }

        // a password remembered by an older version: signs in once more, then becomes a token
        rememberMe = !string.IsNullOrEmpty(savedPassword);
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
        if (screen)
        {
            screen.Submitted -= OnSubmitted;
            screen.TwitchRequested -= OnTwitchRequested;
            screen.TwitchCancelled -= CancelTwitch;
        }
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

    /// <summary>Logging out: forgets the saved sign in, here and on the server.</summary>
    public void ClearPassword()
    {
        var token = deviceToken ?? DeviceTokenStore.Load();
        if (!string.IsNullOrEmpty(token) && gameManager && gameManager.RavenNest != null)
        {
            // fire and forget: the scene reloads right after, the request does not need it
            _ = gameManager.RavenNest.Auth.RevokeDeviceAsync(token);
        }

        DeviceTokenStore.Clear();
        PlayerPrefs.DeleteKey(LegacyUsernameKey);
        PlayerPrefs.DeleteKey(LegacyPasswordKey);
        deviceToken = null;
        username = string.Empty;
        password = string.Empty;
        rememberMe = false;
        FillScreen();
    }

    // ---- Password ------------------------------------------------------------------------------

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
                // the password itself is never kept: a device token stands in for it. One saved by an
                // older version stays until the token is stored, so a registration that fails does not
                // quietly end remember me (it signs in with the password once more next time)
                if (rememberMe)
                {
                    PlayerPrefs.SetString(LegacyUsernameKey, username);
                    if (await RememberThisDevice()) PlayerPrefs.DeleteKey(LegacyPasswordKey);
                }
                else
                {
                    PlayerPrefs.DeleteKey(LegacyPasswordKey);
                    PlayerPrefs.DeleteKey(LegacyUsernameKey);
                    DeviceTokenStore.Clear();
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

    /// <summary>Registers this computer for remember me; true once its device token is stored.</summary>
    private async Task<bool> RememberThisDevice()
    {
        try
        {
            var token = await gameManager.RavenNest.Auth.RegisterDeviceAsync(DeviceTokenStore.DeviceName);
            if (!string.IsNullOrEmpty(token))
            {
                deviceToken = token;
                DeviceTokenStore.Save(token);
                return true;
            }
        }
        catch (Exception exc)
        {
            // signed in all the same; it only will not sign in on its own next time
            Shinobytes.Debug.LogWarning("Could not remember this computer: " + exc.Message);
        }
        return false;
    }

    // ---- A saved sign in -----------------------------------------------------------------------

    private IEnumerator DelayedDeviceLogin(string token)
    {
        if (Screen) Screen.SetBusy(true);
        yield return new WaitForSeconds(1f);
        LoginWithDevice(token);
    }

    private async void LoginWithDevice(string token)
    {
        if (loggingIn || gameManager == null || gameManager.RavenNest == null)
        {
            return;
        }

        if (gameManager.RavenNest.Authenticated)
        {
            HideView();
            return;
        }

        loggingIn = true;
        if (Screen) Screen.SetBusy(true);
        var failure = LoginFailure.None;
        try
        {
            if (await gameManager.RavenNest.LoginWithDeviceAsync(token))
            {
                deviceToken = token;
                HideView();
                return;
            }

            failure = gameManager.RavenNest.LastLoginFailure;
        }
        finally
        {
            loggingIn = false;
            if (Screen) Screen.SetBusy(false);
        }

        if (failure == LoginFailure.Unreachable)
        {
            // keep the token, and try again in a while: the server or the connection is down
            if (Screen) Screen.ShowFailure(LoginFailure.Unreachable);
            StartCoroutine(RetryDeviceLogin(token));
            return;
        }

        // turned down: revoked from elsewhere, or unused for too long
        DeviceTokenStore.Clear();
        if (Screen) Screen.ShowNotice(Localization.UI_LOGIN_DEVICE_EXPIRED_TITLE, Localization.UI_LOGIN_DEVICE_EXPIRED);
    }

    private IEnumerator RetryDeviceLogin(string token)
    {
        yield return new WaitForSeconds(retrySeconds);
        if (gameManager && gameManager.RavenNest != null && !gameManager.RavenNest.Authenticated && DeviceTokenStore.Load() == token)
        {
            LoginWithDevice(token);
        }
    }

    // ---- Twitch, through the website -----------------------------------------------------------

    private async void OnTwitchRequested()
    {
        if (loggingIn || gameManager == null || gameManager.RavenNest == null)
        {
            return;
        }

        var attempt = ++twitchAttempt;
        var secret = DeviceTokenStore.NewSecret();
        AuthApiStart start;
        try
        {
            if (Screen) Screen.SetBusy(true);
            var result = await gameManager.RavenNest.Auth.StartDeviceLoginAsync(secret, DeviceTokenStore.DeviceName);
            start = result != null && !string.IsNullOrEmpty(result.Code)
                ? new AuthApiStart { Code = result.Code, DisplayCode = result.DisplayCode, Path = result.Path, Expires = Time.realtimeSinceStartup + result.ExpiresInSeconds }
                : null;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("Could not start a Twitch sign in: " + exc.Message);
            start = null;
        }
        finally
        {
            if (Screen) Screen.SetBusy(false);
        }

        if (attempt != twitchAttempt) return;
        if (start == null)
        {
            if (Screen) Screen.ShowFailure(LoginFailure.Unreachable);
            return;
        }

        var url = gameManager.RavenNest.WebsiteUrl + start.Path;
        if (Screen) Screen.ShowTwitchWaiting(start.DisplayCode, url);
        Application.OpenURL(url);
        StartCoroutine(PollTwitch(attempt, start, secret));
    }

    private sealed class AuthApiStart
    {
        public string Code;
        public string DisplayCode;
        public string Path;
        public float Expires;
    }

    private IEnumerator PollTwitch(int attempt, AuthApiStart start, string secret)
    {
        while (attempt == twitchAttempt)
        {
            yield return new WaitForSecondsRealtime(pollSeconds);
            if (attempt != twitchAttempt) yield break;

            if (Time.realtimeSinceStartup > start.Expires)
            {
                EndTwitch(Localization.UI_LOGIN_TWITCH_EXPIRED_TITLE, Localization.UI_LOGIN_TWITCH_EXPIRED);
                yield break;
            }

            var poll = gameManager.RavenNest.Auth.PollDeviceLoginAsync(start.Code, secret);
            while (!poll.IsCompleted) yield return null;
            if (attempt != twitchAttempt) yield break;

            var result = poll.Status == TaskStatus.RanToCompletion ? poll.Result : null;
            switch (result?.Status)
            {
                case null:
                case "pending":
                    // no answer this time, or not approved yet: ask again
                    continue;

                case "approved":
                    twitchAttempt++;
                    if (result.AuthToken != null && gameManager.RavenNest.CompleteLogin(result.AuthToken))
                    {
                        deviceToken = result.DeviceToken;
                        rememberMe = true;
                        DeviceTokenStore.Save(deviceToken);
                        PlayerPrefs.DeleteKey(LegacyPasswordKey);
                        if (!string.IsNullOrEmpty(result.UserName))
                        {
                            username = result.UserName;
                            PlayerPrefs.SetString(LegacyUsernameKey, username);
                        }
                        if (Screen) Screen.EndTwitchWaiting();
                        HideView();
                    }
                    else
                    {
                        EndTwitch(Localization.UI_LOGIN_TWITCH_EXPIRED_TITLE, Localization.UI_LOGIN_TWITCH_EXPIRED);
                    }
                    yield break;

                case "denied":
                    EndTwitch(Localization.UI_LOGIN_TWITCH_DENIED_TITLE, Localization.UI_LOGIN_TWITCH_DENIED);
                    yield break;

                default:
                    EndTwitch(Localization.UI_LOGIN_TWITCH_EXPIRED_TITLE, Localization.UI_LOGIN_TWITCH_EXPIRED);
                    yield break;
            }
        }
    }

    private void CancelTwitch()
    {
        twitchAttempt++;
        if (Screen) Screen.EndTwitchWaiting();
    }

    private void EndTwitch(string title, string text)
    {
        twitchAttempt++;
        if (!Screen) return;
        Screen.EndTwitchWaiting();
        Screen.ShowNotice(title, text);
    }

    // ---- Automatic sign in from a file ---------------------------------------------------------

    /// <summary>
    /// Written before the game restarts itself for an update, so it comes back signed in. Holds
    /// the device token when there is one, the name and password otherwise.
    /// </summary>
    public void ActivateTempAutoLogin()
    {
        if (!string.IsNullOrEmpty(deviceToken))
        {
            Shinobytes.IO.File.WriteAllLines(TmpAutoLoginFile, new string[] { "device=" + deviceToken });
            return;
        }

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
        if (!string.IsNullOrEmpty(loginInfo.Device))
        {
            StartCoroutine(DelayedDeviceLogin(loginInfo.Device));
            return;
        }

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
        var device = "";

        for (var i = 0; i < vs.Length; ++i)
        {
            var line = vs[i]?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var eq = line.IndexOf('=');
            if (eq > 0)
            {
                // the value may hold an '=' of its own
                var key = line.Substring(0, eq).ToLower().Trim();
                var value = line.Substring(eq + 1).Trim();
                if (key.IndexOf("device") >= 0)
                {
                    device = value;
                }
                else if (key.IndexOf("pass") >= 0)
                {
                    pass = value;
                }
                else if (key.IndexOf("user") >= 0)
                {
                    user = value;
                }
            }
            else
            {
                if (i == 0) user = vs[i]?.Trim();
                if (i == 1) pass = vs[i]?.Trim();
            }
        }

        return new AutoLoginInfo { Username = user, Password = pass, Device = device };
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
        public string Device { get; set; }
    }
}
