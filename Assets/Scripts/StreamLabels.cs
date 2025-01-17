﻿using System;
using System.Collections.Generic;
using System.Linq;

public class StreamLabels
{
    private readonly Dictionary<string, StreamLabel> labels
        = new Dictionary<string, StreamLabel>();
    private readonly GameSettings gameSettings;

    public StreamLabels(GameSettings gameSettings)
    {
        this.gameSettings = gameSettings;
    }

    public StreamLabel RegisterText(string name, Func<string> generator)
    {
        return labels[name] = new StreamLabel(gameSettings, name, ".txt", generator);
    }

    public StreamLabel Register<T>(string name, Func<T> model)
    {
        return labels[name] = new StreamLabel(gameSettings, name, ".json",
            () => Newtonsoft.Json.JsonConvert.SerializeObject(model()));
    }

    public IReadOnlyList<StreamLabel> GetAll()
    {
        return labels.Values.ToList();
    }

    public void UpdateAll()
    {
        foreach (var lbl in GetAll())
        {
            lbl.Update();
        }
    }
}

public class StreamLabel
{
    private readonly Func<string> valueGenerator;
    private readonly string extension;
    private readonly string fileName;
    private string lastSavedValue;

    public StreamLabel(GameSettings settings, string name, string extension, Func<string> valueGenerator)
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        this.valueGenerator = valueGenerator;
        this.extension = extension;
        var folder = settings.StreamLabelsFolder;
        folder = Shinobytes.IO.Path.Combine(folder, extension.Substring(1));
        this.fileName = Shinobytes.IO.Path.Combine(folder, name + extension);
        if (!Shinobytes.IO.Directory.Exists(folder))
            Shinobytes.IO.Directory.CreateDirectory(folder);
    }
    public void Update()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        var streamLabels = PlayerSettings.Instance.StreamLabels;
        if (!streamLabels.Enabled) return;
        if (!streamLabels.SaveTextFiles && extension == ".txt") return;
        if (!streamLabels.SaveJsonFiles && extension == ".json") return;

        Value = valueGenerator();
        SaveGameStat();
    }
    public string Value { get; private set; }
    private void SaveGameStat()
    {
        if (!Overlay.IsGame)
        {
            return;
        }

        try
        {
            // Check if text has changed, otherwise we dont save it.
            if (lastSavedValue == Value)
            {
                return;
            }

            lastSavedValue = Value;
            Shinobytes.IO.File.WriteAllText(fileName, Value);
        }
        catch
        {
            // Ignore: since we do not want this to interrupt any execution of the script.
        }
    }
}
