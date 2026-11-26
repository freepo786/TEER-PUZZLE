using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeerPuzzle
{
    /// <summary>
    /// All screens and HUD elements (legacy UnityEngine.UI).
    /// Every reference below is a public field so the scene builder can
    /// assign it in the Inspector. Buttons are wired in Start; the scene
    /// builder may also wire them by hand instead.
    ///
    /// Version 1.3 — Level map pagination (100 levels per page),
    /// Splash + Settings screens, vibration setting,
    /// language setting, Remove Ads (IAP), banner on the level map only,
    /// rewarded ad for an extra life on the lose screen.
    /// NOTE: Undo button hata diya gaya (Rehan ka faisla) — koi undo UI nahi.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ---------- HUD (game screen) ----------
        public Text levelLabel;
        public Text movesLabel;
        public Text livesLabel;
        public Text hintsLabel;

        // ---------- Panels ----------
        public GameObject gamePanel;
        public GameObject levelMapPanel;
        public GameObject winPanel;
        public GameObject losePanel;
        public GameObject themePanel;

        // ---------- Splash (v1.2) ----------
        public GameObject splashPanel;
        public Image splashLogo;

        // ---------- Settings (v1.2) ----------
        public GameObject settingsPanel;
        public Button settingsButton;
        public Button settingsCloseButton;
        public Button soundToggleButton;
        public Text soundToggleLabel;
        public Button vibrationToggleButton;
        public Text vibrationToggleLabel;
        public Button languageButton;
        public Text languageLabel;
        public Button privacyButton;
        public Button removeAdsButton;
        public Text removeAdsLabel;

        /// <summary>Privacy policy ka link — TODO: Rehan asal link dega.</summary>
        public const string PrivacyPolicyUrl = "https://example.com/teer-puzzle-privacy";

        // ---------- Win overlay ----------
        public Text winHeading;
        public Text winStatsText;
        public Image[] starImages;
        public Sprite starFullSprite;
        public Sprite starEmptySprite;
        public Button nextLevelButton;

        // ---------- Lose overlay ----------
        public Button retryButton;
        public Button watchAdButton;

        // ---------- Game buttons ----------
        public Button hintButton;
        public Button soundButton;
        public Text soundLabel;
        public Button themeButton;
        public Button mapButton;

        // ---------- Level map ----------
        public Transform levelMapContent;
        public Text levelMapTitle;

        // ---------- Level map pagination (v1.3) ----------
        /// <summary>Levels per map page — 2000 levels = 20 pages.</summary>
        public const int MapPageSize = 100;
        public Button mapPrevButton;
        public Button mapNextButton;
        public Text mapPageLabel;

        // ---------- Theme picker ----------
        public Transform themeButtonsParent;

        // ---------- Version label (Home / Level Map screen) ----------
        // Small label at the bottom: "v1.4 — testing".
        public Text versionLabel;

        private Coroutine starPopCoroutine;
        private List<Button> builtLevelButtons = new List<Button>();
        private List<Button> builtThemeButtons = new List<Button>();

        // Level map pagination state (v1.3) — ShowLevelMap se cache hota hai.
        private int currentMapPage;
        private List<int> lastMapStars;
        private int lastMapUnlocked;

        // Splash sirf aik dafa dikhana hai (dobara call se guard).
        private bool splashShown;

        private void Awake()
        {
            if (starFullSprite == null)
            {
                starFullSprite = Resources.Load<Sprite>("Art/star_full");
            }
            if (starEmptySprite == null)
            {
                starEmptySprite = Resources.Load<Sprite>("Art/star_empty");
            }

            // Version label for the v1.4 testing build.
            if (versionLabel != null)
            {
                versionLabel.text = "v" + GameManager.Version + " — testing";
            }
        }

        private void Start()
        {
            WireButton(nextLevelButton, OnNextLevelClicked);
            WireButton(retryButton, OnRetryClicked);
            WireButton(watchAdButton, OnWatchAdClicked);
            WireButton(hintButton, OnHintClicked);
            WireButton(soundButton, OnSoundClicked);
            WireButton(themeButton, ToggleThemePanel);
            WireButton(mapButton, OnMapClicked);

            // Settings (v1.2).
            WireButton(settingsButton, ShowSettings);
            WireButton(settingsCloseButton, HideSettings);
            WireButton(soundToggleButton, ToggleSoundSetting);
            WireButton(vibrationToggleButton, ToggleVibration);
            WireButton(languageButton, CycleLanguage);
            WireButton(privacyButton, OnPrivacyClicked);
            WireButton(removeAdsButton, BuyRemoveAds);

            // Level map pagination (v1.3): agar SceneBuilder ne bar
            // assign na kiya ho to runtime par bana lo, phir wire karo.
            EnsurePaginationControls();
            WireButton(mapPrevButton, () => ShowMapPage(currentMapPage - 1));
            WireButton(mapNextButton, () => ShowMapPage(currentMapPage + 1));
            UpdateMapPageLabel();

            // IAP: Remove Ads khareedne par label taaza karo.
            GameManager game = GameManager.Instance;
            if (game != null && game.iapManager != null)
            {
                game.iapManager.OnRemoveAdsChanged += RefreshRemoveAdsLabel;
            }

            RefreshSoundLabel();
            RefreshSettingsLabels();

            // Fallback: agar GameManager ne ShowSplash na bulaya ho to
            // khud bulao — splashShown guard ki wajah se double nahi chalega.
            if (!splashShown)
            {
                ShowSplash();
            }
        }

        // ------------------------------------------------------------------
        // HUD
        // ------------------------------------------------------------------

        /// <summary>Refreshes every HUD label for the current level state.</summary>
        public void SetHUD(int level, int moves, int lives, int hints)
        {
            if (levelLabel != null)
            {
                levelLabel.text = "Level " + level;
            }
            if (movesLabel != null)
            {
                movesLabel.text = "Moves: " + moves;
            }
            if (livesLabel != null)
            {
                int maxLives = 3;
                GameManager game = GameManager.Instance;
                if (game != null && game.livesManager != null)
                {
                    maxLives = game.livesManager.maxLives;
                }
                string hearts = string.Empty;
                for (int i = 0; i < maxLives; i++)
                {
                    hearts += i < lives ? "♥" : "♡";
                }
                livesLabel.text = hearts;
            }
            if (hintsLabel != null)
            {
                hintsLabel.text = "Hints: " + hints;
            }
        }

        // ------------------------------------------------------------------
        // Screens
        // ------------------------------------------------------------------

        /// <summary>Shows the game screen and hides every overlay.</summary>
        public void ShowGame()
        {
            SetPanel(gamePanel, true);
            SetPanel(levelMapPanel, false);
            SetPanel(winPanel, false);
            SetPanel(losePanel, false);
            SetPanel(themePanel, false);
            SetPanel(settingsPanel, false);
            SetPanel(splashPanel, false);

            // Banner sirf level map par — game board par nahi.
            if (GameManager.Instance != null && GameManager.Instance.adManager != null)
            {
                GameManager.Instance.adManager.HideBanner();
            }
        }

        /// <summary>
        /// Shows the level map, paginated (v1.3): sirf current page ke
        /// 100 level buttons bante hain. Default page woh hai jis mein
        /// highest unlocked level ho. Locked levels (index &gt; unlocked)
        /// cannot be tapped.
        /// </summary>
        public void ShowLevelMap(List<int> stars, int unlocked)
        {
            SetPanel(gamePanel, false);
            SetPanel(levelMapPanel, true);
            SetPanel(winPanel, false);
            SetPanel(losePanel, false);
            SetPanel(themePanel, false);
            SetPanel(settingsPanel, false);

            if (versionLabel != null)
            {
                versionLabel.text = "v" + GameManager.Version + " — testing";
            }

            // Cache karo taake page badalne par dobara bana sakein.
            lastMapStars = stars;
            lastMapUnlocked = unlocked;

            // Default page: highest unlocked level wala page.
            int totalPages = TotalMapPages();
            currentMapPage = Mathf.Clamp(unlocked / MapPageSize, 0, totalPages - 1);
            RefreshMapPage();

            // Banner sirf menu / level map par dikhta hai.
            GameManager game2 = GameManager.Instance;
            if (game2 != null && game2.adManager != null)
            {
                game2.adManager.ShowBanner();
            }
        }

        /// <summary>
        /// Dikhata hai map ka page (0-based, clamp hota hai): buttons
        /// dobara banta hai, page label aur Prev/Next update hote hain,
        /// aur scroll top par reset hota hai.
        /// </summary>
        public void ShowMapPage(int page)
        {
            int totalPages = TotalMapPages();
            currentMapPage = Mathf.Clamp(page, 0, totalPages - 1);
            RefreshMapPage();
            PlayClick();
        }

        /// <summary>Map ke kul pages (MapPageSize ke hisab se).</summary>
        private int TotalMapPages()
        {
            GameManager game = GameManager.Instance;
            int total = game != null && game.levelManager != null
                ? game.levelManager.TotalLevels : 0;
            if (total <= 0)
            {
                // LevelManager na mile to 2000 levels maan lo (20 pages).
                return 20;
            }
            return (total + MapPageSize - 1) / MapPageSize;
        }

        /// <summary>
        /// Current page ke buttons + label + Prev/Next state taaza karta
        /// hai aur scroll ko top par le jata hai.
        /// </summary>
        private void RefreshMapPage()
        {
            BuildLevelButtons(lastMapStars, lastMapUnlocked, currentMapPage);
            UpdateMapPageLabel();

            int totalPages = TotalMapPages();
            if (mapPrevButton != null)
            {
                mapPrevButton.interactable = currentMapPage > 0;
            }
            if (mapNextButton != null)
            {
                mapNextButton.interactable = currentMapPage < totalPages - 1;
            }

            if (levelMapContent != null)
            {
                ScrollRect scrollRect = levelMapContent.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 1f;
                }
            }
        }

        /// <summary>Page label: "Page X / 20".</summary>
        private void UpdateMapPageLabel()
        {
            if (mapPageLabel != null)
            {
                mapPageLabel.text = "Page " + (currentMapPage + 1) + " / " + TotalMapPages();
            }
        }

        /// <summary>
        /// Runtime fallback (v1.3): agar SceneBuilder ne pagination bar
        /// assign na kiya ho to levelMapPanel ke neeche Prev / label /
        /// Next bana lo. Bar bottom-center y=125, height 80 — VersionLabel
        /// (y 35..85) aur MapScroll (neeche se 180px) se overlap nahi karta.
        /// levelMapPanel null ho to kuch nahi karta.
        /// </summary>
        private void EnsurePaginationControls()
        {
            if (levelMapPanel == null)
            {
                return;
            }
            if (mapPrevButton != null && mapNextButton != null && mapPageLabel != null)
            {
                return;
            }

            GameObject barGo = new GameObject("MapPaginationBar", typeof(RectTransform));
            barGo.transform.SetParent(levelMapPanel.transform, false);
            RectTransform barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 125f);
            barRect.sizeDelta = new Vector2(900f, 80f);

            if (mapPrevButton == null)
            {
                mapPrevButton = CreateTextButton(barGo.transform, "‹ Pichla");
                SetBarChildRect(mapPrevButton.GetComponent<RectTransform>(), -320f, 260f);
            }
            if (mapNextButton == null)
            {
                mapNextButton = CreateTextButton(barGo.transform, "Agla ›");
                SetBarChildRect(mapNextButton.GetComponent<RectTransform>(), 320f, 260f);
            }
            if (mapPageLabel == null)
            {
                GameObject labelGo = new GameObject("MapPageLabel", typeof(RectTransform));
                labelGo.transform.SetParent(barGo.transform, false);
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(0f, 0f);
                labelRect.sizeDelta = new Vector2(340f, 80f);
                Text label = labelGo.AddComponent<Text>();
                label.text = "Page 1 / 20";
                label.alignment = TextAnchor.MiddleCenter;
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.color = Color.black;
                label.resizeTextForBestFit = true;
                mapPageLabel = label;
            }
        }

        private void SetBarChildRect(RectTransform rect, float x, float width)
        {
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 80f);
        }

        /// <summary>
        /// Win overlay: heading "Level {n} mukammal", stars popping one
        /// by one, stats, and only the "Agla Level" button.
        /// </summary>
        public void ShowWin(int level, int stars, int moves, int livesLeft, int hintsUsed)
        {
            SetPanel(winPanel, true);
            SetPanel(losePanel, false);

            if (winHeading != null)
            {
                winHeading.text = "Level " + level + " mukammal";
            }

            if (winStatsText != null)
            {
                winStatsText.text = "Moves: " + moves +
                                    "\nLives bache: " + livesLeft +
                                    "\nHints istemal: " + hintsUsed;
            }

            if (starPopCoroutine != null)
            {
                StopCoroutine(starPopCoroutine);
            }
            starPopCoroutine = StartCoroutine(PopStarsRoutine(stars));
        }

        /// <summary>Lose overlay with the "Dobara Khelo" (retry) button.</summary>
        public void ShowLose()
        {
            SetPanel(losePanel, true);
            SetPanel(winPanel, false);
        }

        /// <summary>Hides the lose overlay (e.g. after a rewarded extra life).</summary>
        public void HideLosePanel()
        {
            SetPanel(losePanel, false);
        }

        // ------------------------------------------------------------------
        // Splash (v1.2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Splash screen: logo + title, 1.5 second baad chhup kar level map
        /// dikhata hai. Dobara call hone se splashShown guard rokta hai.
        /// </summary>
        public void ShowSplash()
        {
            if (splashShown)
            {
                return;
            }
            splashShown = true;

            SetPanel(winPanel, false);
            SetPanel(losePanel, false);
            SetPanel(themePanel, false);
            SetPanel(settingsPanel, false);
            SetPanel(splashPanel, true);

            StartCoroutine(SplashRoutine());
        }

        private IEnumerator SplashRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            SetPanel(splashPanel, false);

            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.ShowLevelMap();
            }
        }

        // ------------------------------------------------------------------
        // Settings (v1.2)
        // ------------------------------------------------------------------

        /// <summary>Shows the settings panel and refreshes its labels.</summary>
        public void ShowSettings()
        {
            SetPanel(settingsPanel, true);
            RefreshSettingsLabels();
            PlayClick();
        }

        /// <summary>Hides the settings panel.</summary>
        public void HideSettings()
        {
            SetPanel(settingsPanel, false);
            PlayClick();
        }

        /// <summary>Refreshes every settings label from the current save.</summary>
        public void RefreshSettingsLabels()
        {
            RefreshSoundSettingLabel();
            RefreshVibrationLabel();
            RefreshLanguageLabel();
            RefreshRemoveAdsLabel();
        }

        private void RefreshSoundSettingLabel()
        {
            if (soundToggleLabel == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            bool muted = game != null && game.soundManager != null && game.soundManager.muted;
            soundToggleLabel.text = muted ? "Sound: Off" : "Sound: On";
        }

        private void RefreshVibrationLabel()
        {
            if (vibrationToggleLabel == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            bool off = game != null && game.saveManager != null && game.saveManager.Data != null
                && game.saveManager.Data.vibrationOff;
            vibrationToggleLabel.text = off ? "Vibration: Off" : "Vibration: On";
        }

        private void RefreshLanguageLabel()
        {
            if (languageLabel == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            string lang = game != null && game.saveManager != null && game.saveManager.Data != null
                && game.saveManager.Data.language != null
                ? game.saveManager.Data.language : "en";
            languageLabel.text = lang == "ur" ? "Language: Urdu (V2 mein)" : "Language: English";
        }

        private void RefreshRemoveAdsLabel()
        {
            if (removeAdsLabel == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            bool owned = game != null && game.iapManager != null && game.iapManager.RemoveAdsOwned;
            removeAdsLabel.text = owned ? "Remove Ads: Kharida hua" : "Remove Ads: Kharido";
        }

        /// <summary>Flips the mute setting (settings screen) and saves it.</summary>
        public void ToggleSoundSetting()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.soundManager == null)
            {
                return;
            }

            game.soundManager.SetMuted(!game.soundManager.muted);

            if (game.saveManager != null && game.saveManager.Data != null)
            {
                game.saveManager.Data.soundOn = !game.soundManager.muted;
                game.saveManager.Save();
            }

            RefreshSoundLabel();
            RefreshSettingsLabels();
            PlayClick();
        }

        /// <summary>Flips the vibration setting and saves it.</summary>
        public void ToggleVibration()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.saveManager == null || game.saveManager.Data == null)
            {
                return;
            }

            game.saveManager.Data.vibrationOff = !game.saveManager.Data.vibrationOff;
            game.saveManager.Save();
            RefreshVibrationLabel();
            PlayClick();
        }

        /// <summary>Toggles the language between English and Urdu.</summary>
        public void CycleLanguage()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.saveManager == null || game.saveManager.Data == null)
            {
                return;
            }

            string current = game.saveManager.Data.language;
            game.saveManager.Data.language = current == "ur" ? "en" : "ur";
            game.saveManager.Save();
            RefreshLanguageLabel();
            PlayClick();
        }

        /// <summary>Opens the privacy policy link (TODO: Rehan asal link dega).</summary>
        public void OnPrivacyClicked()
        {
            PlayClick();
            Application.OpenURL(PrivacyPolicyUrl);
        }

        /// <summary>Starts the Remove Ads one-time purchase (null-safe).</summary>
        public void BuyRemoveAds()
        {
            PlayClick();
            GameManager game = GameManager.Instance;
            if (game != null && game.iapManager != null)
            {
                game.iapManager.BuyRemoveAds();
            }
        }

        // ------------------------------------------------------------------
        // Theme picker
        // ------------------------------------------------------------------

        /// <summary>Shows or hides the theme picker panel.</summary>
        public void ToggleThemePanel()
        {
            if (themePanel == null)
            {
                return;
            }

            bool show = !themePanel.activeSelf;
            themePanel.SetActive(show);
            if (show)
            {
                BuildThemeButtons();
            }
            PlayClick();
        }

        /// <summary>Builds one button per theme inside themeButtonsParent.</summary>
        public void BuildThemeButtons()
        {
            GameManager game = GameManager.Instance;
            if (themeButtonsParent == null || game == null || game.themeManager == null)
            {
                return;
            }

            ClearButtons(builtThemeButtons);

            for (int i = 0; i < game.themeManager.themes.Count; i++)
            {
                int themeIndex = i;
                ThemePalette palette = game.themeManager.themes[i];
                Button button = CreateTextButton(themeButtonsParent, palette.name);
                builtThemeButtons.Add(button);

                // Tint the button with the theme accent color.
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = palette.accent;
                }

                button.onClick.AddListener(() =>
                {
                    game.ApplyTheme(themeIndex);
                    if (themePanel != null)
                    {
                        themePanel.SetActive(false);
                    }
                    PlayClick();
                });
            }
        }

        // ------------------------------------------------------------------
        // Sound toggle
        // ------------------------------------------------------------------

        /// <summary>Flips the mute setting and refreshes the sound label.</summary>
        public void ToggleSound()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.soundManager == null)
            {
                return;
            }

            game.soundManager.SetMuted(!game.soundManager.muted);

            // HUD se badla hua sound bhi save karo (v1.3 fix) — warna
            // restart par wapas aa jata tha (sirf settings wala save karta tha).
            if (game.saveManager != null && game.saveManager.Data != null)
            {
                game.saveManager.Data.soundOn = !game.soundManager.muted;
                game.saveManager.Save();
            }

            RefreshSoundLabel();
            PlayClick();
        }

        public void RefreshSoundLabel()
        {
            if (soundLabel == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            bool muted = game != null && game.soundManager != null && game.soundManager.muted;
            soundLabel.text = muted ? "Sound: Off" : "Sound: On";
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private IEnumerator PopStarsRoutine(int stars)
        {
            if (starImages != null)
            {
                foreach (var image in starImages)
                {
                    if (image != null)
                    {
                        image.sprite = starEmptySprite;
                        image.transform.localScale = Vector3.one;
                    }
                }
            }

            for (int i = 0; i < stars; i++)
            {
                yield return new WaitForSeconds(0.35f);

                if (starImages == null || i >= starImages.Length || starImages[i] == null)
                {
                    continue;
                }

                starImages[i].sprite = starFullSprite;
                yield return StartCoroutine(PopScaleRoutine(starImages[i].transform));
                PlayStar();
            }
        }

        private IEnumerator PopScaleRoutine(Transform target)
        {
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot: grow past 1, then settle back.
                float scale = 1f + 0.5f * Mathf.Sin(t * Mathf.PI);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>Current page ke level buttons banata hai.</summary>
        private void BuildLevelButtons(List<int> stars, int unlocked)
        {
            BuildLevelButtons(stars, unlocked, currentMapPage);
        }

        /// <summary>
        /// Sirf diye gaye page ke level buttons banata hai
        /// (indices [page*MapPageSize, min((page+1)*MapPageSize, total))).
        /// Label / lock / star / click logic pehle jaisa hi hai.
        /// </summary>
        private void BuildLevelButtons(List<int> stars, int unlocked, int page)
        {
            GameManager game = GameManager.Instance;
            if (levelMapContent == null || game == null || game.levelManager == null)
            {
                return;
            }

            ClearButtons(builtLevelButtons);

            int total = game.levelManager.TotalLevels;
            int totalPages = (total + MapPageSize - 1) / MapPageSize;
            page = Mathf.Clamp(page, 0, Mathf.Max(0, totalPages - 1));
            int start = page * MapPageSize;
            int end = Mathf.Min(start + MapPageSize, total);
            for (int i = start; i < end; i++)
            {
                int levelIndex = i;
                bool isLocked = i > unlocked;
                int levelStars = stars != null && i < stars.Count ? stars[i] : 0;

                string label = "Level " + (i + 1);
                if (!isLocked && levelStars > 0)
                {
                    label += "  " + new string('★', levelStars);
                }
                if (isLocked)
                {
                    label += " (Locked)";
                }

                Button button = CreateTextButton(levelMapContent, label);
                button.interactable = !isLocked;
                builtLevelButtons.Add(button);

                button.onClick.AddListener(() =>
                {
                    PlayClick();
                    game.StartLevel(levelIndex);
                });
            }
        }

        private Button CreateTextButton(Transform parent, string label)
        {
            var buttonGo = new GameObject("Button_" + label, typeof(RectTransform));
            buttonGo.transform.SetParent(parent, false);

            var image = buttonGo.AddComponent<Image>();
            image.color = Color.white;

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            // LayoutElement taake runtime mein banne wale buttons
            // VerticalLayoutGroup (level map / theme list) mein saaf dikhen.
            var layout = buttonGo.AddComponent<LayoutElement>();
            layout.minHeight = 96f;
            layout.preferredHeight = 96f;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.color = Color.black;
            text.resizeTextForBestFit = true;

            return button;
        }

        private void ClearButtons(List<Button> buttons)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            buttons.Clear();
        }

        private void SetPanel(GameObject panel, bool show)
        {
            if (panel != null)
            {
                panel.SetActive(show);
            }
        }

        private void WireButton(Button button, Action onClick)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => onClick());
            }
        }

        private void OnNextLevelClicked()
        {
            PlayClick();
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.NextLevel();
            }
        }

        private void OnRetryClicked()
        {
            PlayClick();
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.RestartLevel();
            }
        }

        private void OnWatchAdClicked()
        {
            PlayClick();
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.WatchRewardedForExtraLife();
            }
        }

        private void OnHintClicked()
        {
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.UseHint();
            }
        }

        private void OnSoundClicked()
        {
            ToggleSound();
        }

        private void OnMapClicked()
        {
            PlayClick();
            GameManager game = GameManager.Instance;
            if (game != null)
            {
                game.ShowLevelMap();
            }
        }

        /// <summary>Halki vibration feedback — sirf jab setting ON ho.</summary>
        private void Vibrate()
        {
            if (GameManager.Instance != null && GameManager.Instance.saveManager != null
                && GameManager.Instance.saveManager.Data != null
                && !GameManager.Instance.saveManager.Data.vibrationOff)
            {
                Handheld.Vibrate();
            }
        }

        private void PlayClick()
        {
            Vibrate();
            GameManager game = GameManager.Instance;
            if (game != null && game.soundManager != null)
            {
                game.soundManager.PlayClick();
            }
        }

        private void PlayStar()
        {
            GameManager game = GameManager.Instance;
            if (game != null && game.soundManager != null)
            {
                game.soundManager.PlayStar();
            }
        }
    }
}
