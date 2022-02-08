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
    private readonly GameSettings settings;
    private readonly string name;
    private string lastSavedValue;

    public StreamLabel(GameSettings settings, string name, Func<string> valueGenerator)
    {
        this.valueGenerator = valueGenerator;
        this.settings = settings;
        this.name = name;
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
            if (!System.IO.Directory.Exists(settings.StreamLabelsFolder))
                System.IO.Directory.CreateDirectory(settings.StreamLabelsFolder);

            Shinobytes.IO.File.WriteAllText(System.IO.Path.Combine(settings.StreamLabelsFolder, name + ".txt"), Value);
        }
        catch
        {
            // Ignore: since we do not want this to interrupt any execution of the script.
        }
    }
}
