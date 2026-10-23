using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The background music: a playlist per situation (<see cref="MusicContext"/>), chosen from what the camera shows a
/// couple of times a second. Tracks are shuffled without playing the same song twice in a row, with a short pause
/// between them, and a change of situation crossfades. Sudden things (a war, a raid boss, a dungeon) switch within
/// a second; gentle ones (night falling, another island) only once they have lasted a few seconds, so the music does
/// not flick about while the camera moves between players.
/// </summary>
/// <remarks>
/// The tracks come from <see cref="MusicLibrary"/> (Resources, built from Assets/Sounds/Music in the editor). A
/// situation with no tracks falls through to the next one down; with no library at all the three clips set on this
/// component play as before (background, raid boss, war).
/// </remarks>
public class MusicManager : MonoBehaviour
{
    public AudioClip RaidBossMusic;
    public AudioClip StreamRaidMusic;
    public AudioClip BackgroundMusic;

    private AudioSource[] _player;
    private IEnumerator[] fader = new IEnumerator[2];
    private int ActivePlayer = 0;

    public float fadeDuration = 1.0f;

    // a change of situation crossfades over this; a new track in the same situation fades in over the shorter one
    private const float SwitchFade = 2.5f;
    private const float TrackFade = 1f;
    private const float PauseBetweenTracks = 2f;
    // how long a new situation must last before the music follows it
    private const float SuddenDelay = 1f;
    private const float GentleDelay = 8f;
    private const float CheckEvery = 0.5f;
    // night and rain need to be clearly there, not just starting
    private const float NightFrom = 0.6f;
    private const float RainFrom = 0.35f;
    // the camera is down in a dungeon below this height (as DungeonMood)
    private const float DungeonDepth = -30f;

    private GameManager game;
    private MusicLibrary library;
    private bool libraryLoaded;
    private MusicContext? playing;
    private MusicContext? pending;
    private float pendingSince;
    private float nextCheck;
    private float nextTrackAt = -1f;
    private bool forceSwitch;
    private AudioClip lastClip;
    private string lastSong;
    private readonly List<AudioClip> pool = new List<AudioClip>();

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

    // The events call these as they start and end; the playlist sees them by itself, this only makes it switch now.
    public void PlayStreamRaidMusic() => forceSwitch = true;
    public void PlayBackgroundMusic() => forceSwitch = true;
    public void PlayRaidBossMusic() => forceSwitch = true;

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
            // the playlist moves on at the end of a track; a lone track is simply picked again
            s.loop = false;
            s.playOnAwake = false;
            s.volume = 0.0f;
        }
    }

    private void Update()
    {
        var now = Time.unscaledTime;
        if (now >= nextCheck || forceSwitch)
        {
            nextCheck = now + CheckEvery;
            var wanted = Resolve();
            if (playing != wanted)
            {
                if (pending != wanted)
                {
                    pending = wanted;
                    pendingSince = now;
                }
                var delay = forceSwitch ? 0f : IsSudden(wanted) || (playing.HasValue && IsSudden(playing.Value)) ? SuddenDelay : GentleDelay;
                if (!playing.HasValue || now - pendingSince >= delay)
                {
                    playing = wanted;
                    pending = null;
                    PlayNext(SwitchFade);
                }
            }
            else
            {
                pending = null;
            }
            forceSwitch = false;
        }

        // the end of a track: a short pause, then the next one
        var active = _player[ActivePlayer];
        if (playing.HasValue && fader[1] == null && !active.isPlaying)
        {
            if (nextTrackAt < 0f) nextTrackAt = now + PauseBetweenTracks;
            if (now >= nextTrackAt) PlayNext(TrackFade);
        }
    }

    private static bool IsSudden(MusicContext context)
    {
        switch (context)
        {
            case MusicContext.War:
            case MusicContext.RaidBoss:
            case MusicContext.DungeonBoss:
            case MusicContext.Dungeon:
            case MusicContext.Arena:
            case MusicContext.Menu:
                return true;
            default:
                return false;
        }
    }

    /// <summary>The most specific situation the game is in that has music, down to the day's.</summary>
    private MusicContext Resolve()
    {
        if (!game) game = GameManager.Instance;
        if (!libraryLoaded)
        {
            library = Resources.Load<MusicLibrary>(MusicLibrary.ResourceName);
            libraryLoaded = true;
        }

        foreach (var context in Candidates())
        {
            if (Pool(context).Count > 0) return context;
        }
        return MusicContext.Day;
    }

    private IEnumerable<MusicContext> Candidates()
    {
        if (!game)
        {
            yield return MusicContext.Menu;
            yield return MusicContext.Day;
            yield break;
        }

        var nest = game.RavenNest;
        if (nest == null || !nest.SessionStarted) yield return MusicContext.Menu;

        if (game.StreamRaid && game.StreamRaid.Started) yield return MusicContext.War;
        // a raid is on while its boss lives (RaidManager.Started is about the timer for the next raid)
        var boss = game.Raid ? game.Raid.Boss : null;
        if (boss && boss.Enemy && !boss.Enemy.Stats.IsDead) yield return MusicContext.RaidBoss;

        var camera = Camera.main;
        var dungeons = game.Dungeons;
        if (dungeons && dungeons.Started && camera && camera.transform.position.y < DungeonDepth)
        {
            var room = dungeons.Dungeon ? dungeons.Dungeon.Room : null;
            if (room && room.RoomType == DungeonRoomType.Boss) yield return MusicContext.DungeonBoss;
            yield return MusicContext.Dungeon;
        }

        if (game.Arena && game.Arena.Started && game.Camera && game.Camera.State == GameCameraType.Arena) yield return MusicContext.Arena;
        if (game.Tavern && game.Tavern.IsActivated) yield return MusicContext.Tavern;
        if (VillageBuildMode.Instance && VillageBuildMode.Instance.IsActive) yield return MusicContext.Village;

        var observed = game.Camera && game.Camera.Observer ? game.Camera.Observer.ObservedPlayer : null;
        if (observed && observed.onsenHandler && observed.onsenHandler.InOnsen) yield return MusicContext.Onsen;
        var atSea = observed ? observed.ferryHandler && observed.ferryHandler.OnFerry : Ambience.HasFocus && string.IsNullOrEmpty(Ambience.Island);
        if (atSea) yield return MusicContext.Sailing;

        if (Weather.Rain >= RainFrom) yield return MusicContext.Rain;
        if (Ambience.Darkness >= NightFrom) yield return MusicContext.Night;
        if (IslandContext(Ambience.Island, out var island)) yield return island;
        yield return MusicContext.Day;
    }

    private static bool IslandContext(string identifier, out MusicContext context)
    {
        switch (identifier)
        {
            case "Home": context = MusicContext.Home; return true;
            case "Away": context = MusicContext.Away; return true;
            case "Ironhill": context = MusicContext.Ironhill; return true;
            case "Kyo": context = MusicContext.Kyo; return true;
            case "Heim": context = MusicContext.Heim; return true;
            case "Atria": context = MusicContext.Atria; return true;
            case "Eldara": context = MusicContext.Eldara; return true;
            default: context = MusicContext.Day; return false;
        }
    }

    /// <summary>
    /// The tracks a situation plays. An island plays its own tracks and the day's, its own twice as often, so a
    /// couple of island tracks do not repeat for hours. The raid boss, the war and the day fall back to the clips set
    /// on this component.
    /// </summary>
    private List<AudioClip> Pool(MusicContext context)
    {
        pool.Clear();
        if (library)
        {
            var own = library.Get(context);
            switch (context)
            {
                case MusicContext.Home:
                case MusicContext.Away:
                case MusicContext.Ironhill:
                case MusicContext.Kyo:
                case MusicContext.Heim:
                case MusicContext.Atria:
                case MusicContext.Eldara:
                    if (own.Count == 0) break;
                    AddAll(own);
                    AddAll(own);
                    AddAll(library.Get(MusicContext.Day));
                    break;
                default:
                    AddAll(own);
                    break;
            }
        }

        if (pool.Count == 0)
        {
            if (context == MusicContext.RaidBoss && RaidBossMusic) pool.Add(RaidBossMusic);
            else if (context == MusicContext.War && StreamRaidMusic) pool.Add(StreamRaidMusic);
            else if (context == MusicContext.Day && BackgroundMusic) pool.Add(BackgroundMusic);
        }
        return pool;
    }

    private void AddAll(IReadOnlyList<AudioClip> clips)
    {
        foreach (var clip in clips) if (clip) pool.Add(clip);
    }

    /// <summary>The next track of the situation: not the last one, and if it can be, not the same song either.</summary>
    private void PlayNext(float fade)
    {
        nextTrackAt = -1f;
        var clips = Pool(playing ?? MusicContext.Day);
        if (clips.Count == 0) return;

        AudioClip pick = null;
        for (var attempt = 0; attempt < 8 && pick == null; ++attempt)
        {
            var clip = clips[Random.Range(0, clips.Count)];
            if (clips.Count > 1 && clip == lastClip) continue;
            // two versions of one song are two tracks; prefer another song next, but take it if there is none
            if (attempt < 6 && Song(clip) == lastSong && HasOtherSong(clips)) continue;
            pick = clip;
        }
        if (!pick) pick = clips[0];

        lastClip = pick;
        lastSong = Song(pick);
        Play(pick, fade);
    }

    private bool HasOtherSong(List<AudioClip> clips)
    {
        foreach (var clip in clips) if (Song(clip) != lastSong) return true;
        return false;
    }

    /// <summary>A track's song: its name without the prompt number in front and the version mark at the end.</summary>
    private static string Song(AudioClip clip)
    {
        var name = clip ? clip.name : "";
        var dot = name.IndexOf('.');
        if (dot > 0 && dot <= 3 && char.IsDigit(name[0])) name = name.Substring(dot + 1);
        var bracket = name.LastIndexOf(" (");
        if (bracket > 0 && name.EndsWith(")")) name = name.Substring(0, bracket);
        return name.Trim();
    }

    /// <summary>
    /// Starts the fading of the provided AudioClip and the running clip
    /// </summary>
    /// <param name="clip">AudioClip to fade-in</param>
    public void Play(AudioClip clip) => Play(clip, fadeDuration);

    private void Play(AudioClip clip, float fade)
    {
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
        if (_player[ActivePlayer].isPlaying && _player[ActivePlayer].volume > 0)
        {
            fader[0] = FadeAudioSource(_player[ActivePlayer], fade, false, () => { fader[0] = null; });
            StartCoroutine(fader[0]);
        }
        else
        {
            _player[ActivePlayer].Stop();
        }

        //Fade-in the new clip
        int NextPlayer = (ActivePlayer + 1) % _player.Length;
        _player[NextPlayer].clip = clip;
        _player[NextPlayer].volume = 0f;
        _player[NextPlayer].Play();
        fader[1] = FadeAudioSource(_player[NextPlayer], fade, true, () => { fader[1] = null; });
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
