using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerLogoManager : MonoBehaviour
{
    private readonly Dictionary<string, Sprite> userLogos = new Dictionary<string, Sprite>();

    [SerializeField] private Sprite replacementLogo;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    public Sprite GetLogo(string raiderUserId)
    {
        if (gameManager.LogoCensor)
        {
            return replacementLogo;
        }

        if (userLogos.TryGetValue(raiderUserId, out var sprite))
        {
            return sprite;
        }

        StartCoroutine(DownloadTexture(raiderUserId));
        return null;
    }

    private IEnumerator DownloadTexture(string userId)
    {
        if (userLogos.ContainsKey(userId))
        {
            yield break;
        }

        userLogos[userId] = null;

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(
            "https://www.ravenfall.stream/api/twitch/logo/" + userId);

        yield return www.SendWebRequest();

        if (www.isNetworkError)
        {
            Debug.Log(www.error);
            userLogos.Remove(userId);
        }
        else
        {
            Texture2D webTexture = ((DownloadHandlerTexture)www.downloadHandler).texture as Texture2D;
            Sprite webSprite = SpriteFromTexture2D(webTexture);
            userLogos[userId] = webSprite;
        }
    }

    private Sprite SpriteFromTexture2D(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
    }
}
