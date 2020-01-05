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

    //Note: If the volumeChangesPerSecond value is higher than the fps, the duration of the fading will be extended!
    private int volumeChangesPerSecond = 15;

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
            if (_player != null && _player.Length > 0)
            {
                foreach (var player in _player)
                {
                    player.volume = value;
                }
            }

            _volume = value;
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
        volume = PlayerPrefs.GetFloat("MusicVolume", volume);

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
        foreach (IEnumerator i in fader)
        {
            if (i != null)
            {
                StopCoroutine(i);
            }
        }

        //Fade-out the active play, if it is not silent (eg: first start)
        if (_player[ActivePlayer].volume > 0)
        {
            fader[0] = FadeAudioSource(_player[ActivePlayer], fadeDuration, 0.0f, () => { fader[0] = null; });
            StartCoroutine(fader[0]);
        }

        //Fade-in the new clip
        int NextPlayer = (ActivePlayer + 1) % _player.Length;
        _player[NextPlayer].clip = clip;
        _player[NextPlayer].Play();
        fader[1] = FadeAudioSource(_player[NextPlayer], fadeDuration, volume, () => { fader[1] = null; });
        StartCoroutine(fader[1]);

        //Register new active player
        ActivePlayer = NextPlayer;
    }
    /// <summary>
    /// Fades an AudioSource(player) during a given amount of time(duration) to a specific volume(targetVolume)
    /// </summary>
    /// <param name="player">AudioSource to be modified</param>
    /// <param name="duration">Duration of the fading</param>
    /// <param name="targetVolume">Target volume, the player is faded to</param>
    /// <param name="finishedCallback">Called when finshed</param>
    /// <returns></returns>
    IEnumerator FadeAudioSource(AudioSource player, float duration, float targetVolume, System.Action finishedCallback)
    {
        //Calculate the steps
        int Steps = (int)(volumeChangesPerSecond * duration);
        float StepTime = duration / Steps;
        float StepSize = (targetVolume - player.volume) / Steps;

        //Fade now
        for (int i = 1; i < Steps; i++)
        {
            player.volume += StepSize;
            yield return new WaitForSeconds(StepTime);
        }
        //Make sure the targetVolume is set
        player.volume = targetVolume;

        //Callback
        if (finishedCallback != null)
        {
            finishedCallback();
        }
    }
}
