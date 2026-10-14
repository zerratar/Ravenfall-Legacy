using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// Keeps the device token that signs the game in on its own, in place of the password that
/// remember me used to save in plain text. On Windows it is encrypted for the signed in Windows
/// user (DPAPI), so a copy of the registry or of the settings on another account or computer
/// cannot be used. The token is also revoked on the server when the game logs out.
/// </summary>
public static class DeviceTokenStore
{
    private const string Key = "LoginDevice";
    private const string Entropy = "Ravenfall device token";

    public static bool HasToken => PlayerPrefs.HasKey(Key) && !string.IsNullOrEmpty(PlayerPrefs.GetString(Key));

    public static string Load()
    {
        var stored = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(stored))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(stored);
            var plain = Unprotect(bytes);
            return plain != null ? Encoding.UTF8.GetString(plain) : null;
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogWarning("The saved sign in could not be read and was dropped: " + exc.Message);
            Clear();
            return null;
        }
    }

    public static void Save(string deviceToken)
    {
        if (string.IsNullOrEmpty(deviceToken))
        {
            Clear();
            return;
        }

        var protectedBytes = Protect(Encoding.UTF8.GetBytes(deviceToken));
        PlayerPrefs.SetString(Key, Convert.ToBase64String(protectedBytes));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    /// <summary>A name for this computer, shown on the website when approving and in the list of devices.</summary>
    public static string DeviceName
    {
        get
        {
            try
            {
                var name = Environment.MachineName;
                return string.IsNullOrWhiteSpace(name) ? "Ravenfall" : "Ravenfall on " + name;
            }
            catch
            {
                return "Ravenfall";
            }
        }
    }

    /// <summary>A random secret for one sign in through the website, kept only in memory.</summary>
    public static string NewSecret()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // ---- DPAPI ---------------------------------------------------------------------------------
    // ProtectedData is not in Unity's profile, so crypt32 directly. Elsewhere than Windows the
    // token is stored as it is, as the password was before.

    private static byte[] Protect(byte[] data)
    {
        if (!IsWindows) return data;
        return Crypt(data, true) ?? throw new InvalidOperationException("CryptProtectData failed");
    }

    private static byte[] Unprotect(byte[] data)
    {
        if (!IsWindows) return data;
        return Crypt(data, false);
    }

    private static bool IsWindows =>
        Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string description, ref DataBlob entropy,
        IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, ref DataBlob entropy,
        IntPtr reserved, IntPtr prompt, int flags, ref DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr mem);

    private const int UiForbidden = 0x1;

    private static byte[] Crypt(byte[] data, bool protect)
    {
        var entropyBytes = Encoding.UTF8.GetBytes(Entropy);
        var input = new DataBlob { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        var entropy = new DataBlob { cbData = entropyBytes.Length, pbData = Marshal.AllocHGlobal(entropyBytes.Length) };
        var output = new DataBlob();
        try
        {
            Marshal.Copy(data, 0, input.pbData, data.Length);
            Marshal.Copy(entropyBytes, 0, entropy.pbData, entropyBytes.Length);

            var ok = protect
                ? CryptProtectData(ref input, "Ravenfall", ref entropy, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output)
                : CryptUnprotectData(ref input, IntPtr.Zero, ref entropy, IntPtr.Zero, IntPtr.Zero, UiForbidden, ref output);
            if (!ok)
            {
                return null;
            }

            var result = new byte[output.cbData];
            Marshal.Copy(output.pbData, result, 0, output.cbData);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(input.pbData);
            Marshal.FreeHGlobal(entropy.pbData);
            if (output.pbData != IntPtr.Zero) LocalFree(output.pbData);
        }
    }
}
