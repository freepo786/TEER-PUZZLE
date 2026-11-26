using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Loads level JSON files from Resources/Levels and builds the
    /// selected level on the grid. Levels are sorted by file name
    /// (level_001, level_002, ...) so the order is stable.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // Assigned in the scene (or found automatically by GameManager).
        public GridManager gridManager;

        public int TotalLevels
        {
            get { return levels.Count; }
        }

        public int CurrentLevelIndex { get; private set; }

        /// <summary>Raised after a level has been built on the grid.</summary>
        public event Action<LevelData> OnLevelLoaded;

        private List<LevelData> levels = new List<LevelData>();

        private void Awake()
        {
            LoadAllLevels();
        }

        /// <summary>
        /// Loads every TextAsset under Resources/Levels, parses it as
        /// LevelData, and sorts by asset name. Returns the loaded list.
        /// </summary>
        public List<LevelData> LoadAllLevels()
        {
            levels.Clear();

            TextAsset[] assets = Resources.LoadAll<TextAsset>("Levels");
            Array.Sort(assets, (a, b) => string.CompareOrdinal(a.name, b.name));

            foreach (var asset in assets)
            {
                try
                {
                    LevelData level = JsonUtility.FromJson<LevelData>(asset.text);
                    if (level != null && level.arrows != null)
                    {
                        levels.Add(level);
                    }
                    else
                    {
                        Debug.LogWarning("[LevelManager] Skipped invalid level file: " + asset.name);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[LevelManager] Failed to parse level file " + asset.name + ": " + e.Message);
                }
            }

            return levels;
        }

        /// <summary>Level at index i, or null when i is out of range.</summary>
        public LevelData GetLevel(int i)
        {
            if (i < 0 || i >= levels.Count)
            {
                return null;
            }
            return levels[i];
        }

        /// <summary>
        /// Builds level i on the grid and remembers it as the current level.
        /// Does nothing (and logs a warning) when i is out of range.
        /// </summary>
        public void LoadLevel(int i)
        {
            LevelData level = GetLevel(i);
            if (level == null)
            {
                Debug.LogWarning("[LevelManager] No level at index " + i + ".");
                return;
            }

            CurrentLevelIndex = i;

            if (gridManager != null)
            {
                gridManager.BuildLevel(level);
            }
            else
            {
                Debug.LogWarning("[LevelManager] gridManager is not assigned; level not built.");
            }

            if (OnLevelLoaded != null)
            {
                OnLevelLoaded(level);
            }
        }

        /// <summary>Loads the level after the current one, if it exists.</summary>
        public void NextLevel()
        {
            if (CurrentLevelIndex + 1 < levels.Count)
            {
                LoadLevel(CurrentLevelIndex + 1);
            }
            else
            {
                Debug.Log("[LevelManager] Already on the last level.");
            }
        }

        /// <summary>
        /// Stars for a finished level:
        /// 3 stars when no life was lost and moves &lt;= arrows + 1,
        /// 2 stars when at least 2 lives remain, otherwise 1 star.
        /// </summary>
        public int CalculateStars(int moves, int arrows, int livesLeft)
        {
            if (livesLeft >= 3 && moves <= arrows + 1)
            {
                return 3;
            }
            if (livesLeft >= 2)
            {
                return 2;
            }
            return 1;
        }
    }
}
