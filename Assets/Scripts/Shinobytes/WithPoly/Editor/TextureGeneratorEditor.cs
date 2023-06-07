using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Unity.EditorCoroutines.Editor;

public class TextureGeneratorEditor : EditorWindow
{
    private string folderPath = "Assets/GeneratedTextures";
    private SettingsData settingsData = new SettingsData
    {
        GenerationMode = "quick",
        PromptText = "Anime grass, extra detailed, cartoonish, stylistic, outdoor",
        UpscaleResolution = 1024,
        PbrMode = "general"
    };

    private Material materialPreview;

    [MenuItem("Window/Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<TextureGeneratorEditor>("Texture Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        settingsData.PromptText = EditorGUILayout.TextField("Prompt Text", settingsData.PromptText);
        folderPath = EditorGUILayout.TextField("Output Folder", folderPath);

        if (GUILayout.Button("Generate Texture"))
        {
            this.StartCoroutine(AsyncOperationHelper.AsIEnumerator(GenerateTexture()));
        }

        if (materialPreview != null)
        {
            EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(256, 256), materialPreview.mainTexture);
        }
    }

    
    public async Task<TextureResponse> GetTextureAsync(SettingsData settingsData)
    {
        using (var httpClient = new HttpClient())
        {
            string url = "https://withpoly.com/api/v1/assets/textures/texture/3yXiYGyK6L";
            
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en,sv;q=0.9,en-US;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Content-Type", "multipart/form-data; boundary=----WebKitFormBoundaryzGKdpdBt3hpxxjHu");
            httpClient.DefaultRequestHeaders.Add("DNT", "1");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://withpoly.com");
            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://withpoly.com/textures/edit/3yXiYGyK6L");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \"Not:A-Brand\";v=\"99\"");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");

            var settingsJson = JsonConvert.SerializeObject(settingsData);
            string requestBody = $"------WebKitFormBoundaryzGKdpdBt3hpxxjHu\r\nContent-Disposition: form-data; name=\"settings\"\r\n\r\n{settingsJson}\r\n------WebKitFormBoundaryzGKdpdBt3hpxxjHu--\r\n";
            
            HttpResponseMessage response = await httpClient.PostAsync(url, new StringContent(requestBody, Encoding.UTF8, "multipart/form-data"));

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                TextureResponse textureResponse = JsonConvert.DeserializeObject<TextureResponse>(jsonResponse);
                return textureResponse;
            }
            else
            {
                throw new HttpRequestException($"Error: {response.StatusCode}");
            }
        }
    }

    private async Task GenerateTexture()
    {
        EditorUtility.DisplayProgressBar("Generating Texture", "Requesting texture generation...", 0f);
        TextureResponse textureResponse = await GetTextureAsync(settingsData);

        // Create the folder if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Download and import textures
        WebClient client = new WebClient();
        string basePath = folderPath + "/" + textureResponse.Name;
        string colorTexturePath = basePath + "_Albedo.png";
        string normalTexturePath = basePath + "_Normal.png";
        string heightTexturePath = basePath + "_Height.png";
        string aoTexturePath = basePath + "_AO.png";
        string roughnessTexturePath = basePath + "_Roughness.png";

        EditorUtility.DisplayProgressBar("Generating Texture", "Downloading textures...", 0.2f);
        await Task.WhenAll(
            client.DownloadFileTaskAsync(textureResponse.ColorMapInfo["web"].Url, colorTexturePath),
            client.DownloadFileTaskAsync(textureResponse.NormalMapInfo["web"].Url, normalTexturePath),
            client.DownloadFileTaskAsync(textureResponse.HeightMapInfo["web"].Url, heightTexturePath),
            client.DownloadFileTaskAsync(textureResponse.AoMapInfo["web"].Url, aoTexturePath),
            client.DownloadFileTaskAsync(textureResponse.RoughnessMapInfo["web"].Url, roughnessTexturePath)
        );

        EditorUtility.DisplayProgressBar("Generating Texture", "Importing textures...", 0.4f);
        AssetDatabase.ImportAsset(colorTexturePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(normalTexturePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(heightTexturePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(aoTexturePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(roughnessTexturePath, ImportAssetOptions.ForceUpdate);

        // Create material
        EditorUtility.DisplayProgressBar("Generating Texture", "Creating material...", 0.6f);
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorTexturePath));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath));
        material.SetTexture("_HeightMap", AssetDatabase.LoadAssetAtPath<Texture2D>(heightTexturePath));
        material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(aoTexturePath));
        material.SetTexture("_SmoothnessMap", AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessTexturePath));

        AssetDatabase.CreateAsset(material, folderPath + "/" + textureResponse.Name + ".mat");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayProgressBar("Generating Texture", "Material created. Refreshing...", 0.8f);
        materialPreview = material;
        EditorUtility.ClearProgressBar();
    }
}