using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StreamLabels
{
    private readonly Dictionary<string, StreamLabel> labels
        = new Dictionary<string, StreamLabel>();
    private readonly GameSettings gameSettings;

    public StreamLabels(GameSettings gameSettings)
    {
        this.gameSettings = gameSettings;
    }
    public StreamLabel Register(string name, Func<string> generator)
    {
        return labels[name] = new StreamLabel(gameSettings, name, generator);
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
    private readonly string fileName;
    private string lastSavedValue;

    public StreamLabel(GameSettings settings, string name, Func<string> valueGenerator)
    {
        this.valueGenerator = valueGenerator;
        this.fileName = Shinobytes.IO.Path.Combine(settings.StreamLabelsFolder, name + ".txt");
        if (!Shinobytes.IO.Directory.Exists(settings.StreamLabelsFolder))
            Shinobytes.IO.Directory.CreateDirectory(settings.StreamLabelsFolder);

    }
    public void Update()
    {
        Value = valueGenerator();
        SaveGameStat();
    }
    public string Value { get; private set; }
    private void SaveGameStat()
    {
        try
        {
            // CHeck if text has changed, otherwise we dont save it.            
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
