using System;
using System.Collections.Generic;

namespace Commodore
{
    /// <summary>
    /// Represents the config.json structure from MechVibes sound packs.
    /// </summary>
    [Serializable]
    public class KeyboardAudioConfig
    {
        public string id;
        public string name;
        public string key_define_type;
        public bool includes_numpad;
        public string sound;
        public Dictionary<string, int[]> defines;
        public string[] tags;
    }

    /// <summary>
    /// Represents a single key sound definition [startTimeMs, durationMs].
    /// </summary>
    [Serializable]
    public class KeySoundDefinition
    {
        public float StartTime { get; private set; }
        public float Duration { get; private set; }

        public KeySoundDefinition(int startTimeMs, int durationMs)
        {
            StartTime = startTimeMs / 1000f; // Convert to seconds
            Duration = durationMs / 1000f;    // Convert to seconds
        }
    }
}
