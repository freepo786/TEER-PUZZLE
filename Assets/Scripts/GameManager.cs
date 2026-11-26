using System;
using UnityEngine;

namespace TeerPuzzle
{
    public enum GameState
    {
        Menu,
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// Central game controller (singleton). Wires the managers together
    /// and owns the win / lose flow, moves, stars, and level unlocking.
    ///
    /// Version 1.4 — CI scene-build step + Remove Ads par banner hide
    /// (testing only). Testing only. No Play Store upload yet,
    /// no analytics. Undo nahi hai — Rehan ka faisla.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Current build version, shown as "v1.4 — testing".</summary>
        public const string Version = "1.4";

        public static GameManager Instance { get; private set; }

        // Assign in the scene; missing ones are found automatically.
        public LevelManager levelManager;
        public GridManager gridManager;
        public LivesManager livesManager;
        public HintSystem hintSystem;
        public SaveManager saveManager;
        public SoundManager soundManager;
        public ThemeManager themeManager;
        public UIManager uiManager;
        public AdManager adManager;
        public IAPManager iapManager;

        public GameState State { get; private set; }

        public int Moves { get; private set; }
        public int HintsUsed { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find any manager the scene builder did not assign.
            if (levelManager == null)
            {
                levelManager = FindFirstObjectByType<LevelManager>();
            }
            if (gridManager == null)
            {
                gridManager = FindFirstObjectByType<GridManager>();
            }
            if (livesManager == null)
            {
                livesManager = FindFirstObjectByType<LivesManager>();
            }
            if (hintSystem == null)
            {
                hintSystem = FindFirstObjectByType<HintSystem>();
            }
            if (saveManager == null)
            {
                saveManager = FindFirstObjectByType<SaveManager>();
            }
            if (soundManager == null)
            {
                soundManager = FindFirstObjectByType<SoundManager>();
            }
            if (themeManager == null)
            {
                themeManager = FindFirstObjectByType<ThemeManager>();
            }
            if (uiManager == null)
            {
                uiManager = FindFirstObjectByType<UIManager>();
            }
            if (iapManager == null)
            {
                iapManager = FindFirstObjectByType<IAPManager>();
            }
            if (adManager == null)
            {
                adManager = FindFirstObjectByType<AdManager>();
            }

            // Ads/IAP scene mein na hon to runtime par bana lo, taake
            // purani scene se banay hue level bhi ads ke baghair na chhuten.
            // (SceneBuilder in ko abhi create nahi karta.)
            if (iapManager == null)
            {
                iapManager = new GameObject("IAPManager").AddComponent<IAPManager>();
            }
            if (adManager == null)
            {
                adManager = new GameObject("AdManager").AddComponent<AdManager>();
            }

            // Cross-wire the managers that need each other.
            if (levelManager != null && levelManager.gridManager == null)
            {
                levelManager.gridManager = gridManager;
            }
            if (hintSystem != null && hintSystem.gridManager == null)
            {
                hintSystem.gridManager = gridManager;
            }
            if (themeManager != null && themeManager.gridManager == null)
            {
                themeManager.gridManager = gridManager;
            }
            if (gridManager != null && gridManager.themeManager == null)
            {
                gridManager.themeManager = themeManager;
            }

            State = GameState.Menu;
        }

        private void Start()
        {
            if (gridManager != null)
            {
                gridManager.OnArrowExited += OnArrowExited;
                gridManager.OnArrowBlocked += OnArrowBlocked;
            }

            if (livesManager != null)
            {
                livesManager.OnLivesChanged += OnLivesChanged;
            }
            if (hintSystem != null)
            {
                hintSystem.OnHintsChanged += OnHintsChanged;
            }

            // Restore the saved theme and sound setting.
            if (saveManager != null && saveManager.Data != null)
            {
                if (themeManager != null)
                {
                    themeManager.ApplyTheme(saveManager.Data.themeIndex);
                }
                if (soundManager != null)
                {
                    soundManager.SetMuted(!saveManager.Data.soundOn);
                }
            }

            if (uiManager != null)
            {
                uiManager.ShowSplash();
            }
            else
            {
                ShowLevelMap();
            }
        }

        // ------------------------------------------------------------------
        // Level flow
        // ------------------------------------------------------------------

        /// <summary>Starts level i (0-based) from scratch.</summary>
        public void StartLevel(int i)
        {
            if (levelManager == null || levelManager.GetLevel(i) == null)
            {
                Debug.LogWarning("[GameManager] Cannot start level " + i + ".");
                return;
            }

            Moves = 0;
            HintsUsed = 0;
            State = GameState.Playing;

            if (livesManager != null)
            {
                livesManager.ResetLives();
            }
            if (hintSystem != null)
            {
                hintSystem.ResetHints();
            }

            levelManager.LoadLevel(i);

            if (soundManager != null)
            {
                soundManager.PlayLevelStart();
            }

            UpdateHUD();
            if (uiManager != null)
            {
                uiManager.ShowGame();
            }
        }

        /// <summary>Restarts the current level.</summary>
        public void RestartLevel()
        {
            StartLevel(levelManager != null ? levelManager.CurrentLevelIndex : 0);
        }

        /// <summary>Starts the level after the current one (stays put on the last level).</summary>
        public void NextLevel()
        {
            if (levelManager == null)
            {
                return;
            }

            int next = levelManager.CurrentLevelIndex + 1;
            if (next < levelManager.TotalLevels)
            {
                // Level mukammal — har 3rd level ke baad interstitial (level ke beech mein kabhi nahi).
                if (adManager != null)
                {
                    adManager.ShowInterstitialIfDue();
                }
                StartLevel(next);
            }
            else
            {
                // No more levels: back to the map.
                ShowLevelMap();
            }
        }

        /// <summary>Shows the level map screen.</summary>
        public void ShowLevelMap()
        {
            State = GameState.Menu;
            if (uiManager != null && saveManager != null && saveManager.Data != null)
            {
                uiManager.ShowLevelMap(saveManager.Data.stars, saveManager.Data.unlockedLevel);
            }
            else if (uiManager != null)
            {
                uiManager.ShowLevelMap(null, 0);
            }
        }

        /// <summary>
        /// The daily level: rotates through all levels by days since epoch
        /// (1 Jan 1970), so every day offers a different level and all
        /// 2000 levels come around in rotation — DayOfYear (1..366) sirf
        /// pehle ~366 levels tak mehdood tha. Roz aik hi level, deterministic.
        /// </summary>
        public LevelData GetDailyLevel()
        {
            if (levelManager == null || levelManager.TotalLevels == 0)
            {
                return null;
            }

            int daysSinceEpoch = (int)(DateTime.Now.Date - new DateTime(1970, 1, 1)).TotalDays;
            int index = daysSinceEpoch % levelManager.TotalLevels;
            return levelManager.GetLevel(index);
        }

        // ------------------------------------------------------------------
        // Player actions
        // ------------------------------------------------------------------

        /// <summary>Uses one hint (finds and pulses a free arrow).</summary>
        public void UseHint()
        {
            if (State != GameState.Playing || hintSystem == null)
            {
                return;
            }

            // Hints khatam hon to rewarded ad dekh kar aik hint mil sakta hai.
            if (hintSystem.HintsLeft <= 0)
            {
                if (adManager != null)
                {
                    adManager.ShowRewarded(AdManager.RewardType.Hint, () =>
                    {
                        hintSystem.AddHint();
                        UseHint();
                    });
                }
                return;
            }

            int before = hintSystem.HintsLeft;
            hintSystem.UseHint();
            if (hintSystem.HintsLeft < before)
            {
                HintsUsed += 1;
                if (soundManager != null)
                {
                    soundManager.PlayHint();
                }
                UpdateHUD();
            }
        }

        /// <summary>
        /// Rewarded ad dekh kar aik extra dil: haarne ke baad khel jaari rehta hai.
        /// (Game Over screen ka "ad dekh kar 1 dil" button isi ko call karega.)
        /// </summary>
        public void WatchRewardedForExtraLife()
        {
            if (adManager == null)
            {
                return;
            }

            adManager.ShowRewarded(AdManager.RewardType.ExtraLife, () =>
            {
                if (livesManager != null)
                {
                    livesManager.AddLife();
                }
                State = GameState.Playing;
                if (uiManager != null)
                {
                    uiManager.HideLosePanel();
                }
            });
        }

        /// <summary>Applies a theme and saves the choice.</summary>
        public void ApplyTheme(int themeIndex)
        {
            if (themeManager == null)
            {
                return;
            }

            themeManager.ApplyTheme(themeIndex);

            if (saveManager != null && saveManager.Data != null)
            {
                saveManager.Data.themeIndex = themeManager.CurrentThemeIndex;
                saveManager.Save();
            }
        }

        // ------------------------------------------------------------------
        // Win / lose
        // ------------------------------------------------------------------

        private void OnArrowExited(Arrow arrow)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Moves += 1;

            if (soundManager != null)
            {
                soundManager.PlaySlide();
            }

            if (hintSystem != null)
            {
                hintSystem.ClearHint();
            }

            UpdateHUD();

            // Win check: no arrows left on the board.
            if (gridManager != null && gridManager.Arrows.Count == 0)
            {
                WinLevel();
            }
        }

        private void OnArrowBlocked(Arrow arrow)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            if (soundManager != null)
            {
                soundManager.PlayError();
            }

            if (livesManager != null && livesManager.LoseLife())
            {
                LoseLevel();
            }

            UpdateHUD();
        }

        private void WinLevel()
        {
            State = GameState.Won;

            LevelData level = levelManager != null
                ? levelManager.GetLevel(levelManager.CurrentLevelIndex)
                : null;
            int arrowCount = level != null && level.arrows != null ? level.arrows.Count : 0;
            int livesLeft = livesManager != null ? livesManager.Lives : 0;

            int stars = levelManager != null
                ? levelManager.CalculateStars(Moves, arrowCount, livesLeft)
                : 1;

            if (saveManager != null)
            {
                saveManager.SetStars(levelManager.CurrentLevelIndex, stars);
                saveManager.UnlockLevel(levelManager.CurrentLevelIndex + 1);
            }

            if (soundManager != null)
            {
                soundManager.PlayWin();
                soundManager.PlayCoin();
            }

            if (uiManager != null)
            {
                int levelNumber = level != null ? level.id : levelManager.CurrentLevelIndex + 1;
                uiManager.ShowWin(levelNumber, stars, Moves, livesLeft, HintsUsed);
            }
        }

        private void LoseLevel()
        {
            State = GameState.Lost;

            if (soundManager != null)
            {
                soundManager.PlayOutOfLives();
                soundManager.PlayLose();
            }

            if (uiManager != null)
            {
                uiManager.ShowLose();
            }
        }

        // ------------------------------------------------------------------
        // HUD refresh
        // ------------------------------------------------------------------

        private void OnLivesChanged(int lives)
        {
            UpdateHUD();
        }

        private void OnHintsChanged(int hints)
        {
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (uiManager == null || levelManager == null)
            {
                return;
            }

            LevelData level = levelManager.GetLevel(levelManager.CurrentLevelIndex);
            int levelNumber = level != null ? level.id : levelManager.CurrentLevelIndex + 1;

            uiManager.SetHUD(
                levelNumber,
                Moves,
                livesManager != null ? livesManager.Lives : 0,
                hintSystem != null ? hintSystem.HintsLeft : 0);
        }
    }
}
