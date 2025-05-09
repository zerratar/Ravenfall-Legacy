using RavenNest.Models;
using System.Text;

public static class ModelExtensions
{
    public static byte[] ToBytes(this SessionToken token)
    {
        if (token == null)
        {
            return null;
        }
        var tokenString = Newtonsoft.Json.JsonConvert.SerializeObject(token);
        return Encoding.UTF8.GetBytes(tokenString.Base64Encode());
    }

    public static string ToBase64(this SessionToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }
        var tokenString = Newtonsoft.Json.JsonConvert.SerializeObject(token);
        return tokenString.Base64Encode();
    }

    public static string ToBase64(this AuthToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }
        var tokenString = Newtonsoft.Json.JsonConvert.SerializeObject(token);
        return tokenString.Base64Encode();
    }

    public static string Base64Encode(this string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}