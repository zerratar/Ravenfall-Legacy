using Shinobytes.OpenAI;
using Shinobytes.OpenAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;

public abstract class OpenAIController<T> : MonoBehaviour
{

#if UNITY_EDITOR
    public string AccessToken;
    private ConcurrentQueue<ChatGPTChoice> responses = new ConcurrentQueue<ChatGPTChoice>();
    private OpenAIClient openAI;
#endif

    private bool requestingData;
    private bool KeepDownloading;
    private readonly List<T> items = new List<T>();
    protected IReadOnlyList<T> Items => items;

    public abstract string GetName(T item);
    public abstract string GetPrompt();
    protected abstract string GetDirectory();

    public void Init()
    {
#if UNITY_EDITOR
        AccessToken = PlayerPrefs.GetString("openai_access_token", AccessToken);
        openAI = new OpenAIClient(GetAccessToken);
#endif
        var dir = Application.dataPath + "/" + GetDirectory();
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);


        var files = System.IO.Directory.GetFiles(dir, "*.json");
        foreach (var file in files)
        {
            try
            {
                var ep = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(file));
                items.Add(ep);
            }
            catch
            {
                UnityEngine.Debug.LogError("Failed to parse: " + file);
            }
        }

        OnItemsUpdated();
    }

    protected virtual void OnItemsUpdated() { }
#if UNITY_EDITOR
    private IOpenAIClientSettings GetAccessToken()
    {
        return new OpenAITokenString(AccessToken);
    }
#endif
    // Start is called before the first frame update
    void Awake()
    {
        Init();
    }

#if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        if (responses.TryDequeue(out var response))
        {
            HandleOpenAIResponse(response);
        }
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            KeepDownloading = false;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            KeepDownloading = true;
            RequestNextAsync();
        }
    }
    private void HandleOpenAIResponse(ChatGPTChoice choice)
    {
        var dir = Application.dataPath + "/" + GetDirectory();
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string filename = null;

        var content = EnsureValidJson(choice.Message.Content);

        Debug.LogWarning(content);

        try
        {
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
            if (obj != null)
            {
                items.Add(obj);
                filename = GetName(obj) + ".json";
                OnItemsUpdated();
            }
        }
        catch
        {
            UnityEngine.Debug.LogError("Failed to parse: " + content);
        }


        if (string.IsNullOrEmpty(filename) || System.IO.File.Exists(dir + "/" + filename))
        {
            filename = System.Guid.NewGuid().ToString() + ".json";
        }

        try
        {
            System.IO.File.WriteAllText(dir + "/" + filename, content);
        }
        catch
        {
            filename = System.Guid.NewGuid().ToString() + ".json";
            System.IO.File.WriteAllText(dir + "/" + filename, content);
        }

        if (KeepDownloading)
        {
            RequestNextAsync();
        }
    }

    private string EnsureValidJson(string content)
    {
        while (content[0] == '\n') content = content.Substring(1);
        return content
            .Replace("’", "'")
            .Replace("”", "\"")
            .Replace(" ( ", " ) ")
            .Replace("\"Josie\"", "\"Josefine\"")
            .Trim();
    }

    public async Task RequestNextAsync()
    {
        if (requestingData)
        {
            UnityEngine.Debug.Log("We are already requesting data. Please hold on :)");
            return;
        }

        requestingData = true;
        try
        {
            if (openAI == null)
            {
                Init();
            }

            if (string.IsNullOrEmpty(AccessToken))
            {
                return;
            }

            PlayerPrefs.SetString("openai_access_token", AccessToken);

            var prompt = GetPrompt();

            UnityEngine.Debug.Log("Requesting Completion using following prompt:\n" + prompt);

            var result = await openAI.GetCompletionAsync(prompt);

            if (result == null || result.Choices == null || result.Choices.Count == 0)
            {
                return; // aww
            }

            foreach (var c in result.Choices)
            {
                responses.Enqueue(c);
            }
        }
        finally
        {
            requestingData = false;
        }
    }

#endif

}

