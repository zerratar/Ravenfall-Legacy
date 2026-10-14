using System;
using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioClip RaidBossMusic;
    public AudioClip StreamRaidMusic;
    public AudioClip BackgroundMusic;

    private AudioSource[] _player;
    private IEnumerator[] fader = new IEnumerator[2];
    private int ActivePlayer = 0;

    public float fadeDuration = 1.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float _volume = 1.0f;
    public float volume
    {
        get
        {
            return _volume;
        }
        set
        {
            _volume = value;

            // Only the track that is playing follows the setting. Setting both brought back the
            // track that had faded out, which kept looping silently, every time the slider moved.
            // A fade in progress picks the new value up by itself.
            if (_player != null && _player.Length > 0 && fader[1] == null)
            {
                _player[ActivePlayer].volume = value;
            }
        }
    }

    /// <summary>
    /// Mutes all AudioSources, but does not stop them!
    /// </summary>
    public bool mute
    {
        set
        {
            foreach (AudioSource s in _player)
            {
                s.mute = value;
            }
        }
        get
        {
            return _player[ActivePlayer].mute;
        }
    }

    public void PlayStreamRaidMusic()
    {
        Play(StreamRaidMusic);
    }

    public void PlayBackgroundMusic()
    {
        Play(BackgroundMusic);
    }

    public void PlayRaidBossMusic()
    {
        Play(RaidBossMusic);
    }    

    /// <summary>
    /// Setup the AudioSources
    /// </summary>
    private void Awake()
    {
        //Generate the two AudioSources
        _player = new AudioSource[2]{
            gameObject.AddComponent<AudioSource>(),
            gameObject.AddComponent<AudioSource>()
        };

        //Set default values
        foreach (AudioSource s in _player)
        {
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0.0f;
        }
    }
    /// <summary>
    /// Starts the fading of the provided AudioClip and the running clip
    /// </summary>
    /// <param name="clip">AudioClip to fade-in</param>
    public void Play(AudioClip clip)
    {
        //Prevent fading the same clip on both players 
        if (clip == _player[ActivePlayer].clip)
        {
            return;
        }
        //Kill all playing
        for (var i = 0; i < fader.Length; ++i)
        {
            if (fader[i] != null)
            {
                StopCoroutine(fader[i]);
                fader[i] = null;
            }
        }

        //Fade-out the active play, if it is not silent (eg: first start)
        if (_player[ActivePlayer].volume > 0)
        {
            fader[0] = FadeAudioSource(_player[ActivePlayer], fadeDuration, false, () => { fader[0] = null; });
            StartCoroutine(fader[0]);
        }

        //Fade-in the new clip
        int NextPlayer = (ActivePlayer + 1) % _player.Length;
        _player[NextPlayer].clip = clip;
        _player[NextPlayer].Play();
        fader[1] = FadeAudioSource(_player[NextPlayer], fadeDuration, true, () => { fader[1] = null; });
        StartCoroutine(fader[1]);

        //Register new active player
        ActivePlayer = NextPlayer;
    }
    /// <summary>
    /// Fades a player in to the music volume or out to silence over <paramref name="duration"/>. The
    /// target is read every frame, so a volume change during a fade is kept. A player faded out is
    /// stopped, rather than left looping at zero.
    /// </summary>
    IEnumerator FadeAudioSource(AudioSource player, float duration, bool fadeIn, System.Action finishedCallback)
    {
        var start = player.volume;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            player.volume = Mathf.Lerp(start, fadeIn ? _volume : 0f, elapsed / duration);
            yield return null;
        }

        player.volume = fadeIn ? _volume : 0f;
        if (!fadeIn) player.Stop();

        finishedCallback?.Invoke();
    }
}
