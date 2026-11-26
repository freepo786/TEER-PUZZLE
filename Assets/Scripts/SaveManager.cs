using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Everything persisted between sessions: unlocked levels, stars per
    /// level, selected theme, and the sound, vibration, and language settings.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Highest level index the player may open (0-based).
        public int unlockedLevel;

        // v1.3 — mukammal levels ki gintee (interstitial cadence ke liye).
        // JsonUtility purani save mein missing int ko 0 deta hai — theek hai.
        public int completedLevels;

        // Stars per level, by level index (0 = no stars yet).
        public List<int> stars = new List<int>();

        public int themeIndex;
        public bool soundOn = true;

        // v1.2 — false = vibration ON. JsonUtility purani save mein missing
        // bool field ko false deta hai, is liye ulta naam rakha hai taake
        // default ON rahe.
        public bool vibrationOff = false;

        // v1.2 — "en" ya "ur" (Urdu UI poori tarah V2 mein aayega).
        public string language = "en";

        public SaveData()
        {
            unlockedLevel = 0;
            themeIndex = 0;
            soundOn = true;
        }
    }

    /// <summary>
    /// Saves and loads SaveData as JSON at
    /// Application.persistentDataPath + "/teer_save.json".
    /// No ads, no analytics, no cloud: local file only.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public const string SaveFileName = "teer_save.json";

        public SaveData Data { get; private set; }

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        private void Awake()
        {
            Load();
        }

        /// <summary>Writes the current data to disk.</summary>
        public void Save()
        {
            if (Data == null)
            {
                Data = new SaveData();
            }

            try
            {
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Failed to save: " + e.Message);
            }
        }

        /// <summary>
        /// Reads the save file from disk. When none exists (or it is
        /// unreadable), fresh default data is used instead.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                    if (loaded != null)
                    {
                        Data = loaded;
                        if (Data.stars == null)
                        {
                            Data.stars = new List<int>();
                        }
                        // v1.2 — JsonUtility purani save mein missing string
                        // ko null deta hai.
                        if (Data.language == null)
                        {
                            Data.language = "en";
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Failed to load save file: " + e.Message);
            }

            Data = new SaveData();
        }

        /// <summary>Unlocks level i (and keeps any higher unlock).</summary>
        public void UnlockLevel(int i)
        {
            if (Data == null)
            {
                Data = new SaveData();
            }
            if (i > Data.unlockedLevel)
            {
                Data.unlockedLevel = i;
                Save();
            }
        }

        /// <summary>
        /// Records stars for a level, keeping the best result so far.
        /// </summary>
        public void SetStars(int level, int stars)
        {
            if (Data == null)
            {
                Data = new SaveData();
            }

            while (Data.stars.Count <= level)
            {
                Data.stars.Add(0);
            }

            if (stars > Data.stars[level])
            {
                Data.stars[level] = stars;
            }
            Save();
        }

        /// <summary>Stars earned for a level (0 when never completed).</summary>
        public int GetStars(int level)
        {
            if (Data == null || Data.stars == null)
            {
                return 0;
            }
            if (level < 0 || level >= Data.stars.Count)
            {
                return 0;
            }
            return Data.stars[level];
        }
    }
}
