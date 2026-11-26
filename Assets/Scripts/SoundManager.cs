using System.Collections.Generic;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Plays short sound effects from Resources/Audio. Clips are loaded
    /// once by name; a missing clip only logs a warning, it never crashes.
    /// The mute setting is kept in PlayerPrefs ("teer_sound_muted").
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        /// <summary>True when all sounds are muted.</summary>
        public bool muted;

        /// <summary>Convenience access for UI buttons and other scripts.</summary>
        public static SoundManager Instance { get; private set; }

        private const string MutedPlayerPrefsKey = "teer_sound_muted";

        // Every clip the game uses. Files live in Resources/Audio as .wav.
        private static readonly string[] ClipNames =
        {
            "button_click",
            "arrow_tap",
            "slide_whoosh",
            "error_bump",
            "hint",
            "win_jingle",
            "lose",
            "star_pop",
            "coin",
            "out_of_lives",
            "level_start"
        };

        private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            muted = PlayerPrefs.GetInt(MutedPlayerPrefsKey, 0) == 1;

            foreach (var name in ClipNames)
            {
                AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
                if (clip != null)
                {
                    clips[name] = clip;
                }
                else
                {
                    Debug.LogWarning("[SoundManager] Missing audio clip: Resources/Audio/" + name);
                }
            }
        }

        /// <summary>Plays the clip with this name (no-op when muted or missing).</summary>
        public void Play(string n)
        {
            if (muted || string.IsNullOrEmpty(n))
            {
                return;
            }

            AudioClip clip;
            if (clips.TryGetValue(n, out clip) && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void PlayClick() { Play("button_click"); }
        public void PlayTap() { Play("arrow_tap"); }
        public void PlaySlide() { Play("slide_whoosh"); }
        public void PlayError() { Play("error_bump"); }
        public void PlayHint() { Play("hint"); }
        public void PlayWin() { Play("win_jingle"); }
        public void PlayLose() { Play("lose"); }
        public void PlayStar() { Play("star_pop"); }
        public void PlayCoin() { Play("coin"); }
        public void PlayLevelStart() { Play("level_start"); }

        /// <summary>Plays the out-of-lives sound (level lost).</summary>
        public void PlayOutOfLives() { Play("out_of_lives"); }

        /// <summary>Mutes or unmutes all sounds and remembers the choice.</summary>
        public void SetMuted(bool m)
        {
            muted = m;
            PlayerPrefs.SetInt(MutedPlayerPrefsKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
