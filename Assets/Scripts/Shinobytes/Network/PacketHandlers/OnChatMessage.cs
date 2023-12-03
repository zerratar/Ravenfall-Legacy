using System;
using System.Collections.Generic;

public class OnChatMessage : ChatBotCommandHandler<string>
{
    private readonly static EmotionDetection emotionDetection = new EmotionDetection();

    public OnChatMessage(
    GameManager game,
    RavenBotConnection server,
    PlayerManager playerManager)
    : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        // return silently, no need to do anything with this message.
        if (string.IsNullOrEmpty(data) || data.StartsWith("!"))
        {
            return;
        }

        if (!player)
        {
            return;
        }

        var equippedPet = player.Inventory.GetEquipmentOfType(RavenNest.Models.ItemType.Pet);
        if (equippedPet == null)
        {
            return;
        }

        if (!equippedPet.Controller)
        {
            return;
        }

        // a bit slow, but lets go with this for now.
        var petController = equippedPet.Controller.GetComponent<PetController>();
        if (!petController || !petController.ReactToChatMessage)
        {
            return;
        }

        var analyzed = emotionDetection.AnalyzeText(data);
        var emotion = emotionDetection.DetermineDominantEmotion(analyzed);
        petController.SetEmotion(emotion);
    }
}

public class EmotionDetection
{
    Dictionary<string, EmotionScores> wordEmotionMap = new Dictionary<string, EmotionScores>
    {
        { "what?!", new EmotionScores { Surprised = 1, Confused = 1 } },
        { "what!!", new EmotionScores { Surprised = 1, Confused = 1 } },
        { "what??", new EmotionScores { Surprised = 1, Confused = 1 } },

        { "lol", new EmotionScores { Happy = 0.5f } },
        { "wow", new EmotionScores { Surprised = 1 } },
        { "haha", new EmotionScores { Happy = 0.5f } },
        { "hehe", new EmotionScores { Happy = 0.5f } },
        { "uhm", new EmotionScores { Confused = 0.5f } },
        { "yay", new EmotionScores { Happy = 1 } },
        { "omg", new EmotionScores { Surprised = 1 } },


        // Phrases
        { "no way!", new EmotionScores { Surprised = 1 } },
        { "no way that", new EmotionScores { Surprised = 0.5f } },
        { "oh no", new EmotionScores { Sad = 1, Surprised = 0.5f } },
        { "oh yes", new EmotionScores { Happy = 1 } },
        { "damn it", new EmotionScores { Angry = 1 } },
        { "oh my god", new EmotionScores { Surprised = 1 } },
        { "thank you", new EmotionScores { Happy = 0.5f } },
        { "thanks", new EmotionScores { Happy = 0.25f } },
        //{ "thanks to", new EmotionScores { Happy = 0.25f} },

        // Happy
        { "joy", new EmotionScores { Happy = 1 } },
        { "happy", new EmotionScores { Happy = 1 } },
        { "elated", new EmotionScores { Happy = 1 } },

        // Angry
        { "angry", new EmotionScores { Angry = 1 } },
        { "furious", new EmotionScores { Angry = 1 } },
        { "irate", new EmotionScores { Angry = 1 } },

        // Sad
        { "sad", new EmotionScores { Sad = 1 } },
        { "depressed", new EmotionScores { Sad = 1 } },
        { "unhappy", new EmotionScores { Sad = 1 } },

        // Disgust
        { "disgust", new EmotionScores { Disgust = 1 } },
        { "revolted", new EmotionScores { Disgust = 1 } },
        { "repulsed", new EmotionScores { Disgust = 1 } },

        // Surprised
        { "surprised", new EmotionScores { Surprised = 1 } },
        { "shocked", new EmotionScores { Surprised = 1 } },
        { "amazed", new EmotionScores { Surprised = 1 } },

        // Hate
        { "hate", new EmotionScores { Hate = 1 } },
        { "loathe", new EmotionScores { Hate = 1 } },
        { "detest", new EmotionScores { Hate = 1 } },

        // Love
        { "love", new EmotionScores { Love = 1 } },
        { "lovable", new EmotionScores { Love = 1 } },
        { "loveable", new EmotionScores { Love = 1 } },
        { "loving", new EmotionScores { Love = 1 } },
        { "adore", new EmotionScores { Love = 1 } },
        { "cherish", new EmotionScores { Love = 1 } },

        // Hungry
        { "hungry", new EmotionScores { Hungry = 1 } },
        { "ate", new EmotionScores { Hungry = 1 } },
        { "eat", new EmotionScores { Hungry = 1 } },
        { "dinner", new EmotionScores { Hungry = 1 } },
        { "breakfast", new EmotionScores { Hungry = 1 } },
        { "lunch", new EmotionScores { Hungry = 1 } },
        { "starving", new EmotionScores { Hungry = 1 } },
        { "ravenous", new EmotionScores { Hungry = 1 } },

        // Confused
        { "confused", new EmotionScores { Confused = 1 } },
        { "baffled", new EmotionScores { Confused = 1 } },
        { "perplexed", new EmotionScores { Confused = 1 } },

        // Embarrassed
        { "embarrassed", new EmotionScores { Embarrassed = 1 } },
        { "ashamed", new EmotionScores { Embarrassed = 1 } },
        { "mortified", new EmotionScores { Embarrassed = 1 } },
    };
    public EmotionScores AnalyzeText(string text)
    {
        EmotionScores totalScores = new EmotionScores();
        string[] words = text.Split(new char[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        // 1. check full message
        if (wordEmotionMap.TryGetValue(text.ToLower(), out EmotionScores wordScore))
        {
            totalScores.Add(wordScore);
        }

        // 2. loop through each known words and check if text contains it
        //    if it does, add a scaled total score of 0.5
        foreach (var w in wordEmotionMap)
        {
            if (text.Contains(w.Key, StringComparison.OrdinalIgnoreCase))
            {
                totalScores.Add(wordScore, 0.5f);
            }
        }

        // 3. loop through each word from the text and get the score.
        foreach (string word in words)
        {
            if (wordEmotionMap.TryGetValue(word.ToLower(), out wordScore))
            {
                totalScores.Add(wordScore);
            }
        }

        return totalScores;
    }

    public string DetermineDominantEmotion(EmotionScores scores)
    {
        var emotions = new Dictionary<string, float>
    {
        { "Happy", scores.Happy },
        { "Angry", scores.Angry },
        { "Sad", scores.Sad },
        { "Disgust", scores.Disgust },
        { "Surprised", scores.Surprised },
        { "Hate", scores.Hate },
        { "Love", scores.Love },
        { "Hungry", scores.Hungry },
        { "Confused", scores.Confused },
        { "Embarrassed", scores.Embarrassed }
    };

        string dominantEmotion = "Neutral";
        float maxScore = 0;

        foreach (var emotion in emotions)
        {
            if (emotion.Value > maxScore)
            {
                maxScore = emotion.Value;
                dominantEmotion = emotion.Key;
            }
        }

        return dominantEmotion;
    }
}

public class EmotionScores
{
    public float Happy;
    public float Angry;
    public float Sad;
    public float Disgust;
    public float Surprised;
    public float Hate;
    public float Love;
    public float Hungry;
    public float Confused;
    public float Embarrassed;

    public EmotionScores()
    {
        Happy = 0;
        Angry = 0;
        Sad = 0;
        Disgust = 0;
        Surprised = 0;
        Hate = 0;
        Love = 0;
        Hungry = 0;
        Confused = 0;
        Embarrassed = 0;
    }

    public void Add(EmotionScores other, float scale = 1f)
    {
        Happy += (other.Happy * scale);
        Angry += (other.Angry * scale);
        Sad += (other.Sad * scale);
        Disgust += (other.Disgust * scale);
        Surprised += (other.Surprised * scale);
        Hate += (other.Hate * scale);
        Love += (other.Love * scale);
        Hungry += (other.Hungry * scale);
        Confused += (other.Confused * scale);
        Embarrassed += (other.Embarrassed * scale);
    }
}
