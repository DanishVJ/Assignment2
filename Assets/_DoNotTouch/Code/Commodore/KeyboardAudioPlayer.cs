using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Commodore
{
    /// <summary>
    /// Plays audio sprites from MechVibes sound packs.
    /// Loads config.json and plays portions of the audio file based on timestamps.
    /// Maps actual keyboard keys to their specific sounds.
    /// </summary>
    public class KeyboardAudioPlayer : MonoBehaviour
    {
        [Header("Audio Setup")]
        [SerializeField] private AudioClip _soundSheet;
        [SerializeField] private TextAsset _configJson;

        [Header("Typewriter Audio (Optional)")]
        [SerializeField] private AudioClip _typewriterSound;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Header("Advanced")]
        [SerializeField] private int _polyphony = 8;

        private Dictionary<int, KeySoundDefinition> _keySoundMap = new Dictionary<int, KeySoundDefinition>();
        private AudioSource[] _audioSources;
        private AudioSource _typewriterAudioSource;
        private int _currentSourceIndex = 0;

        private void Awake()
        {
            // Create multiple audio sources for polyphony
            _audioSources = new AudioSource[_polyphony];
            for (int i = 0; i < _polyphony; i++)
            {
                _audioSources[i] = gameObject.AddComponent<AudioSource>();
                _audioSources[i].playOnAwake = false;
                _audioSources[i].clip = _soundSheet;
                _audioSources[i].volume = _volume;
            }

            // Create dedicated typewriter audio source
            _typewriterAudioSource = gameObject.AddComponent<AudioSource>();
            _typewriterAudioSource.playOnAwake = false;
            _typewriterAudioSource.clip = _typewriterSound;
            _typewriterAudioSource.volume = _volume;

            LoadConfig();
        }

        private void LoadConfig()
        {
            if (_configJson == null)
            {
                Debug.LogError("KeyboardAudioPlayer: config.json TextAsset not assigned!");
                return;
            }

            if (_soundSheet == null)
            {
                Debug.LogError("KeyboardAudioPlayer: Sound sheet AudioClip not assigned!");
                return;
            }

            try
            {
                // Manual JSON parsing since Unity's JsonUtility doesn't support dictionaries
                string jsonText = _configJson.text;

                // Find the "defines" section
                int definesStart = jsonText.IndexOf("\"defines\"");
                if (definesStart == -1)
                {
                    Debug.LogError("KeyboardAudioPlayer: Could not find 'defines' in config.json");
                    return;
                }

                // Parse each key definition manually
                ParseDefines(jsonText);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"KeyboardAudioPlayer: Failed to parse config.json: {e.Message}");
            }
        }

        private void ParseDefines(string jsonText)
        {
            // Use regex to find all "keycode": [startTime, duration] patterns
            // Pattern: "number": [number, number]
            Regex regex = new Regex(@"""(\d+)""\s*:\s*\[\s*(\d+)\s*,\s*(\d+)\s*\]");
            MatchCollection matches = regex.Matches(jsonText);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    int keyCode = int.Parse(match.Groups[1].Value);
                    int startMs = int.Parse(match.Groups[2].Value);
                    int durationMs = int.Parse(match.Groups[3].Value);

                    _keySoundMap[keyCode] = new KeySoundDefinition(startMs, durationMs);
                }
            }
        }

        /// <summary>
        /// Plays the key press sound by detecting which physical key was just pressed.
        /// </summary>
        public void PlayKeyPressFromKeyboard(Keyboard keyboard)
        {
            if (keyboard == null) return;

            // Find which key was just pressed
            foreach (var key in keyboard.allKeys)
            {
                if (key != null && key.wasPressedThisFrame)
                {
                    int scanCode = GetMechVibesScanCode(key.keyCode);
                    if (scanCode > 0 && _keySoundMap.TryGetValue(scanCode, out KeySoundDefinition sound))
                    {
                        PlaySound(sound);
                        return;
                    }
                }
            }

            // Fallback: use any available sound
            if (_keySoundMap.Count > 0)
            {
                var enumerator = _keySoundMap.Values.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    PlaySound(enumerator.Current);
                }
                enumerator.Dispose();
            }
        }

        /// <summary>
        /// Maps Unity KeyCode to MechVibes scan code.
        /// Based on standard QWERTY keyboard layout scan codes.
        /// </summary>
        private int GetMechVibesScanCode(Key key)
        {
            // Numbers row (top of keyboard)
            if (key == Key.Backquote) return 41;  // `~
            if (key == Key.Digit1) return 2;
            if (key == Key.Digit2) return 3;
            if (key == Key.Digit3) return 4;
            if (key == Key.Digit4) return 5;
            if (key == Key.Digit5) return 6;
            if (key == Key.Digit6) return 7;
            if (key == Key.Digit7) return 8;
            if (key == Key.Digit8) return 9;
            if (key == Key.Digit9) return 10;
            if (key == Key.Digit0) return 11;
            if (key == Key.Minus) return 12;
            if (key == Key.Equals) return 13;
            if (key == Key.Backspace) return 14;

            // Top letter row (QWERTY)
            if (key == Key.Tab) return 15;
            if (key == Key.Q) return 16;
            if (key == Key.W) return 17;
            if (key == Key.E) return 18;
            if (key == Key.R) return 19;
            if (key == Key.T) return 20;
            if (key == Key.Y) return 21;
            if (key == Key.U) return 22;
            if (key == Key.I) return 23;
            if (key == Key.O) return 24;
            if (key == Key.P) return 25;
            if (key == Key.LeftBracket) return 26;
            if (key == Key.RightBracket) return 27;
            if (key == Key.Backslash) return 43;

            // Middle letter row (ASDF)
            if (key == Key.CapsLock) return 58;
            if (key == Key.A) return 30;
            if (key == Key.S) return 31;
            if (key == Key.D) return 32;
            if (key == Key.F) return 33;
            if (key == Key.G) return 34;
            if (key == Key.H) return 35;
            if (key == Key.J) return 36;
            if (key == Key.K) return 37;
            if (key == Key.L) return 38;
            if (key == Key.Semicolon) return 39;
            if (key == Key.Quote) return 40;
            if (key == Key.Enter) return 28;

            // Bottom letter row (ZXCV)
            if (key == Key.LeftShift) return 42;
            if (key == Key.Z) return 44;
            if (key == Key.X) return 45;
            if (key == Key.C) return 46;
            if (key == Key.V) return 47;
            if (key == Key.B) return 48;
            if (key == Key.N) return 49;
            if (key == Key.M) return 50;
            if (key == Key.Comma) return 51;
            if (key == Key.Period) return 52;
            if (key == Key.Slash) return 53;
            if (key == Key.RightShift) return 54;

            // Bottom row
            if (key == Key.LeftCtrl) return 29;
            if (key == Key.LeftAlt) return 56;
            if (key == Key.Space) return 57;
            if (key == Key.RightAlt) return 56;
            if (key == Key.RightCtrl) return 29;

            return 0; // Unknown key
        }


        private void PlaySound(KeySoundDefinition sound)
        {
            // Get next available audio source (round-robin)
            AudioSource source = _audioSources[_currentSourceIndex];
            _currentSourceIndex = (_currentSourceIndex + 1) % _polyphony;

            // Play from specific time
            source.time = sound.StartTime;
            source.Play();

            // Schedule stop after duration
            StartCoroutine(StopAfterDelay(source, sound.Duration));
        }

        private System.Collections.IEnumerator StopAfterDelay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (source.isPlaying)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// Plays a simple typewriter sound for computer responses.
        /// </summary>
        public void PlayTypewriterSound()
        {
            if (_typewriterAudioSource && _typewriterSound)
            {
                _typewriterAudioSource.PlayOneShot(_typewriterSound);
            }
        }
    }
}
