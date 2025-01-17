using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

public static class AsyncOperationHelper
{
    public static IEnumerator AsIEnumerator(Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            foreach (var exception in task.Exception.InnerExceptions)
            {
                UnityEngine.Debug.LogError("AsyncOperationHelper.AsIEnumerator: " + exception);
            }
        }
    }
}

public class SettingsData
{
    public string GenerationMode { get; set; }
    public string PromptText { get; set; }
    public int UpscaleResolution { get; set; }
    public string PbrMode { get; set; }
}

public class TextureResponse
{
    public string SchemaVersion { get; set; }
    public string Type { get; set; }
    public string AssetId { get; set; }
    public string VersionId { get; set; }
    public string AnonymousId { get; set; }
    public string CollectionId { get; set; }
    public string Name { get; set; }
    public bool AutogeneratedName { get; set; }
    public string ModifiedAt { get; set; }

    [JsonProperty("preview")]
    public Preview PreviewInfo { get; set; }

    public bool IsDeleted { get; set; }

    [JsonProperty("patches")]
    public List<Patch> PatchList { get; set; }

    [JsonProperty("render_map")]
    public Dictionary<string, MapInfo> RenderMapInfo { get; set; }

    [JsonProperty("color_map")]
    public Dictionary<string, MapInfo> ColorMapInfo { get; set; }

    [JsonProperty("height_map")]
    public Dictionary<string, MapInfo> HeightMapInfo { get; set; }

    [JsonProperty("normal_map")]
    public Dictionary<string, MapInfo> NormalMapInfo { get; set; }

    [JsonProperty("roughness_map")]
    public Dictionary<string, MapInfo> RoughnessMapInfo { get; set; }

    [JsonProperty("ao_map")]
    public Dictionary<string, MapInfo> AoMapInfo { get; set; }

    [JsonProperty("generation_settings")]
    public GenerationSettings GenerationSettingsInfo { get; set; }
}

public class Preview
{
    public string Url { get; set; }
    public string ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class Patch
{
    public string Ext { get; set; }
    public string Dtype { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Url { get; set; }
    public string PatchId { get; set; }
}

public class MapInfo
{
    public string Ext { get; set; }
    public string Dtype { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Url { get; set; }
}

public class GenerationSettings
{
    public string PromptText { get; set; }
    public Patch Patch { get; set; }
    public string SeamlessPromptText { get; set; }
    public double SeamlessPatchScale { get; set; }
    public string UpscalePromptText { get; set; }
    public int UpscaleResolution { get; set; }
    public bool IsSeamless { get; set; }
    public string PbrMode { get; set; }
    public bool PbrGenerateColor { get; set; }
    public bool PbrGenerateNormal { get; set; }
    public bool PbrGenerateHeight { get; set; }
    public bool PbrGenerateAo { get; set; }
    public bool PbrGenerateRoughness { get; set; }
}