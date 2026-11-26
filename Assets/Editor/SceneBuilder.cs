using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TeerPuzzle;

namespace TeerPuzzle.Editor
{
    /// <summary>
    /// Builds MainScene.unity from code, so the scene never has to be
    /// hand-made in the Editor: menu "Teer Puzzle" &gt; "Build Main Scene (v1.4)".
    ///
    /// Layout (reference resolution 1080x1920, portrait):
    /// - Main Camera (orthographic) + BoardCameraFit — board is world-space
    ///   sprites, so the camera fits it (see BoardCameraFit).
    /// - Canvas (Screen Space - Overlay) holds ONLY the HUD bars and the
    ///   overlays. The middle of the screen stays clear of UI raycast
    ///   targets, so taps reach the arrows (OnMouseDown + BoxCollider2D).
    /// - gamePanel is a plain container (no Image) for TopBar + BottomBar.
    /// - ThemeManager.panelImage = the top bar background; boardImage is
    ///   intentionally left empty (the board is world-space, not UI).
    /// </summary>
    public static class SceneBuilder
    {
        private static Font ArialFont
        {
            get { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }

        private static readonly Color DarkText = new Color(0.12f, 0.16f, 0.22f, 1f);
        private static readonly Color AccentTeal = new Color(0.06f, 0.46f, 0.43f, 1f);
        private static readonly Color HeartRed = new Color(0.90f, 0.22f, 0.27f, 1f);
        private static readonly Color CardGreen = new Color(0.08f, 0.33f, 0.18f, 1f);
        private static readonly Color DimBlack = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color BarWhite = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color MapWhite = new Color(1f, 1f, 1f, 0.97f);
        private static readonly Color GrayText = new Color(0.45f, 0.48f, 0.52f, 1f);

        [MenuItem("Teer Puzzle/Build Main Scene (v1.4)")]
        public static void BuildMainScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---- Camera: orthographic, classic-theme background. ----
            GameObject cameraGo = GameObject.Find("Main Camera");
            Camera cam = cameraGo != null ? cameraGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                cameraGo = new GameObject("Main Camera", typeof(Camera));
                cam = cameraGo.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.94f, 0.92f, 1f); // Classic bg #F2F0EA
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.tag = "MainCamera";

            // Remove the default 3D light — this is a 2D game.
            GameObject lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                Object.DestroyImmediate(lightGo);
            }

            // ---- EventSystem (for UI buttons). ----
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ---- Managers (9 objects, one script each). ----
            GameObject managersGo = new GameObject("Managers");
            GameManager gameManager = AddManager<GameManager>(managersGo, "GameManager");
            LevelManager levelManager = AddManager<LevelManager>(managersGo, "LevelManager");
            GridManager gridManager = AddManager<GridManager>(managersGo, "GridManager");
            LivesManager livesManager = AddManager<LivesManager>(managersGo, "LivesManager");
            HintSystem hintSystem = AddManager<HintSystem>(managersGo, "HintSystem");
            SaveManager saveManager = AddManager<SaveManager>(managersGo, "SaveManager");
            SoundManager soundManager = AddManager<SoundManager>(managersGo, "SoundManager");
            ThemeManager themeManager = AddManager<ThemeManager>(managersGo, "ThemeManager");
            UIManager uiManager = AddManager<UIManager>(managersGo, "UIManager");

            // ---- Board root (world space, slightly above center for the HUD bars). ----
            GameObject boardRootGo = new GameObject("BoardRoot");
            boardRootGo.transform.position = new Vector3(0f, 0.4f, 0f);

            // ---- Camera fit for boards from 4x4 up to 10x10. ----
            BoardCameraFit cameraFit = cam.gameObject.AddComponent<BoardCameraFit>();
            cameraFit.levelManager = levelManager;
            cameraFit.gridManager = gridManager;
            cameraFit.boardRoot = boardRootGo.transform;

            // ---- Wire the managers together. ----
            gameManager.levelManager = levelManager;
            gameManager.gridManager = gridManager;
            gameManager.livesManager = livesManager;
            gameManager.hintSystem = hintSystem;
            gameManager.saveManager = saveManager;
            gameManager.soundManager = soundManager;
            gameManager.themeManager = themeManager;
            gameManager.uiManager = uiManager;

            levelManager.gridManager = gridManager;
            gridManager.boardRoot = boardRootGo.transform;
            gridManager.themeManager = themeManager;
            hintSystem.gridManager = gridManager;
            themeManager.gridManager = gridManager;

            // ---- Canvas (Screen Space - Overlay). ----
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ================= Game screen (HUD) =================
            GameObject gamePanel = new GameObject("GamePanel", typeof(RectTransform));
            gamePanel.transform.SetParent(canvasGo.transform, false);
            SetFullRect(gamePanel.GetComponent<RectTransform>());

            // Top bar (its background doubles as ThemeManager.panelImage).
            GameObject topBar = MakePanel(gamePanel, "TopBar", BarWhite,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -75f), new Vector2(0f, 150f));
            themeManager.panelImage = topBar.GetComponent<Image>();

            uiManager.levelLabel = MakeText(topBar, "LevelLabel", "Level 1", 44, DarkText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -75f), new Vector2(320f, 80f),
                TextAnchor.MiddleLeft);
            uiManager.movesLabel = MakeText(topBar, "MovesLabel", "Moves: 0", 40, DarkText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(380f, -75f), new Vector2(280f, 80f),
                TextAnchor.MiddleLeft);
            uiManager.livesLabel = MakeText(topBar, "LivesLabel", "♥♥♥", 44, HeartRed,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -75f), new Vector2(260f, 80f),
                TextAnchor.MiddleCenter);
            uiManager.themeButton = MakeButton(topBar, "ThemeButton", "▼ Rang", 38, AccentTeal, Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -75f), new Vector2(190f, 90f));
            uiManager.soundButton = MakeButton(topBar, "SoundButton", "Sound: On", 34, Color.white, DarkText,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -75f), new Vector2(210f, 90f));
            uiManager.soundLabel = uiManager.soundButton.GetComponentInChildren<Text>();

            // Bottom bar (v1.2: undo button hata diya gaya — Rehan ka faisla).
            GameObject bottomBar = MakePanel(gamePanel, "BottomBar", BarWhite,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 75f), new Vector2(0f, 150f));
            uiManager.hintButton = MakeButton(bottomBar, "HintButton", "Hint", 40, AccentTeal, Color.white,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 75f), new Vector2(220f, 90f));
            uiManager.hintsLabel = MakeText(bottomBar, "HintsLabel", "Hints: 3", 36, DarkText,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(280f, 75f), new Vector2(240f, 80f),
                TextAnchor.MiddleLeft);
            uiManager.mapButton = MakeButton(bottomBar, "MapButton", "Map", 40, Color.white, DarkText,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 75f), new Vector2(180f, 90f));

            uiManager.gamePanel = gamePanel;

            // ================= Theme dropdown =================
            GameObject themePanel = MakePanel(canvasGo, "ThemePanel", Color.white,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -180f), new Vector2(380f, 660f));
            themePanel.SetActive(false);
            GameObject themeButtonsParent = new GameObject("ThemeButtons", typeof(RectTransform));
            themeButtonsParent.transform.SetParent(themePanel.transform, false);
            SetFullRect(themeButtonsParent.GetComponent<RectTransform>());
            VerticalLayoutGroup themeLayout = themeButtonsParent.AddComponent<VerticalLayoutGroup>();
            themeLayout.padding = new RectOffset(16, 16, 16, 16);
            themeLayout.spacing = 8f;
            themeLayout.childControlWidth = true;
            themeLayout.childControlHeight = true;
            themeLayout.childForceExpandWidth = true;
            themeLayout.childForceExpandHeight = false;
            ContentSizeFitter themeFitter = themeButtonsParent.AddComponent<ContentSizeFitter>();
            themeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            uiManager.themePanel = themePanel;
            uiManager.themeButtonsParent = themeButtonsParent.transform;

            // ================= Level map =================
            GameObject levelMapPanel = MakePanel(canvasGo, "LevelMapPanel", MapWhite,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            levelMapPanel.SetActive(false);
            uiManager.levelMapTitle = MakeText(levelMapPanel, "LevelMapTitle", "Levels", 56, DarkText,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 90f),
                TextAnchor.MiddleCenter);

            // Scrollable list of level buttons.
            GameObject mapScroll = new GameObject("MapScroll", typeof(RectTransform));
            mapScroll.transform.SetParent(levelMapPanel.transform, false);
            RectTransform mapScrollRect = mapScroll.GetComponent<RectTransform>();
            mapScrollRect.anchorMin = new Vector2(0f, 0f);
            mapScrollRect.anchorMax = new Vector2(1f, 1f);
            mapScrollRect.offsetMin = new Vector2(40f, 180f);
            mapScrollRect.offsetMax = new Vector2(-40f, -220f);
            ScrollRect scrollRect = mapScroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(mapScroll.transform, false);
            SetFullRect(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            GameObject levelMapContent = new GameObject("LevelMapContent", typeof(RectTransform));
            levelMapContent.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = levelMapContent.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 100f);
            VerticalLayoutGroup mapLayout = levelMapContent.AddComponent<VerticalLayoutGroup>();
            mapLayout.padding = new RectOffset(10, 10, 10, 10);
            mapLayout.spacing = 12f;
            mapLayout.childControlWidth = true;
            mapLayout.childControlHeight = true;
            mapLayout.childForceExpandWidth = true;
            mapLayout.childForceExpandHeight = false;
            ContentSizeFitter mapFitter = levelMapContent.AddComponent<ContentSizeFitter>();
            mapFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            uiManager.levelMapPanel = levelMapPanel;
            uiManager.levelMapContent = levelMapContent.transform;
            uiManager.versionLabel = MakeText(levelMapPanel, "VersionLabel", "v1.4 — testing", 28, GrayText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 50f),
                TextAnchor.MiddleCenter);

            // Settings button (v1.2) — top-right of the level map.
            uiManager.settingsButton = MakeButton(levelMapPanel, "SettingsButton", "Settings", 38, Color.white, DarkText,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -110f), new Vector2(240f, 90f));

            // Pagination bar (v1.3) — bottom strip, bottom-center y=125,
            // height 80: VersionLabel (y 35..85) se upar aur MapScroll
            // (neeche se 180px) se neeche, dono se overlap nahi.
            uiManager.mapPrevButton = MakeButton(levelMapPanel, "MapPrevButton", "‹ Pichla", 38, Color.white, DarkText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-320f, 125f), new Vector2(260f, 80f));
            uiManager.mapPageLabel = MakeText(levelMapPanel, "MapPageLabel", "Page 1 / 20", 36, DarkText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(340f, 80f),
                TextAnchor.MiddleCenter);
            uiManager.mapNextButton = MakeButton(levelMapPanel, "MapNextButton", "Agla ›", 38, Color.white, DarkText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 125f), new Vector2(260f, 80f));

            // ================= Win overlay =================
            GameObject winPanel = MakePanel(canvasGo, "WinPanel", DimBlack,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            winPanel.SetActive(false);
            GameObject winCard = MakePanel(winPanel, "WinCard", CardGreen,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(640f, 720f));
            uiManager.winHeading = MakeText(winCard, "WinHeading", "Level 1 mukammal", 52, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(600f, 90f),
                TextAnchor.MiddleCenter);

            // Stars row (3 images, popped one by one by UIManager).
            GameObject starsRow = new GameObject("StarsRow", typeof(RectTransform));
            starsRow.transform.SetParent(winCard.transform, false);
            RectTransform starsRect = starsRow.GetComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.5f, 1f);
            starsRect.anchorMax = new Vector2(0.5f, 1f);
            starsRect.anchoredPosition = new Vector2(0f, -240f);
            starsRect.sizeDelta = new Vector2(420f, 120f);
            HorizontalLayoutGroup starsLayout = starsRow.AddComponent<HorizontalLayoutGroup>();
            starsLayout.spacing = 30f;
            starsLayout.childAlignment = TextAnchor.MiddleCenter;
            starsLayout.childControlWidth = true;
            starsLayout.childControlHeight = true;
            starsLayout.childForceExpandWidth = false;
            starsLayout.childForceExpandHeight = false;
            Sprite starEmpty = Resources.Load<Sprite>("Art/star_empty");
            Image[] starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject starGo = new GameObject("Star" + (i + 1), typeof(RectTransform));
                starGo.transform.SetParent(starsRow.transform, false);
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 110f);
                Image starImage = starGo.AddComponent<Image>();
                starImage.sprite = starEmpty;
                starImages[i] = starImage;
            }

            uiManager.winStatsText = MakeText(winCard, "WinStats", "Moves: 0\nLives bache: 3\nHints istemal: 0", 36, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(560f, 150f),
                TextAnchor.MiddleCenter);
            uiManager.nextLevelButton = MakeButton(winCard, "NextLevelButton", "Agla Level", 44, Color.white, CardGreen,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(420f, 100f));
            uiManager.starImages = starImages;
            uiManager.winPanel = winPanel;

            // ================= Lose overlay =================
            GameObject losePanel = MakePanel(canvasGo, "LosePanel", DimBlack,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            losePanel.SetActive(false);
            GameObject loseCard = MakePanel(losePanel, "LoseCard", CardGreen,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(640f, 660f));
            MakeText(loseCard, "LoseHeading", "Dil khatam!", 56, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 90f),
                TextAnchor.MiddleCenter);
            MakeText(loseCard, "LoseSub", "Saare dil khatam ho gaye.\nDobara koshish karo!", 36, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(560f, 130f),
                TextAnchor.MiddleCenter);
            // Rewarded ad (v1.2): ad dekh kar 1 dil le kar aage barho.
            uiManager.watchAdButton = MakeButton(loseCard, "WatchAdButton", "Ad dekh kar 1 dil", 40, Color.white, CardGreen,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(460f, 100f));
            uiManager.retryButton = MakeButton(loseCard, "RetryButton", "Dobara Khelo", 44, Color.white, CardGreen,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(420f, 100f));
            uiManager.losePanel = losePanel;

            // ================= Splash screen (v1.2) =================
            // Full-screen, sab se upar (sab se aakhir mein banta hai) —
            // shuru mein active; UIManager.ShowSplash 1.5s baad chhupata hai.
            GameObject splashPanel = MakePanel(canvasGo, "SplashPanel",
                new Color(0.95f, 0.94f, 0.92f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            GameObject splashLogoGo = new GameObject("SplashLogo", typeof(RectTransform));
            splashLogoGo.transform.SetParent(splashPanel.transform, false);
            RectTransform splashLogoRect = splashLogoGo.GetComponent<RectTransform>();
            splashLogoRect.anchorMin = new Vector2(0.5f, 0.5f);
            splashLogoRect.anchorMax = new Vector2(0.5f, 0.5f);
            splashLogoRect.anchoredPosition = new Vector2(0f, 140f);
            splashLogoRect.sizeDelta = new Vector2(420f, 420f);
            Image splashLogoImage = splashLogoGo.AddComponent<Image>();
            splashLogoImage.sprite = Resources.Load<Sprite>("Art/icon_1024");
            MakeText(splashPanel, "SplashTitle", "Teer Puzzle", 72, DarkText,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(700f, 110f),
                TextAnchor.MiddleCenter);
            MakeText(splashPanel, "SplashVersion", "v1.4 — testing", 32, GrayText,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(600f, 60f),
                TextAnchor.MiddleCenter);
            uiManager.splashPanel = splashPanel;
            uiManager.splashLogo = splashLogoImage;

            // ================= Settings screen (v1.2) =================
            GameObject settingsPanel = MakePanel(canvasGo, "SettingsPanel", DimBlack,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            settingsPanel.SetActive(false);
            GameObject settingsCard = MakePanel(settingsPanel, "SettingsCard", Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(680f, 900f));
            MakeText(settingsCard, "SettingsTitle", "Settings", 56, DarkText,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(600f, 90f),
                TextAnchor.MiddleCenter);
            uiManager.soundToggleButton = MakeButton(settingsCard, "SoundToggleButton", "Sound: On", 40, AccentTeal, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(560f, 100f));
            uiManager.soundToggleLabel = uiManager.soundToggleButton.GetComponentInChildren<Text>();
            uiManager.vibrationToggleButton = MakeButton(settingsCard, "VibrationToggleButton", "Vibration: On", 40, AccentTeal, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(560f, 100f));
            uiManager.vibrationToggleLabel = uiManager.vibrationToggleButton.GetComponentInChildren<Text>();
            uiManager.languageButton = MakeButton(settingsCard, "LanguageButton", "Language: English", 40, AccentTeal, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(560f, 100f));
            uiManager.languageLabel = uiManager.languageButton.GetComponentInChildren<Text>();
            uiManager.privacyButton = MakeButton(settingsCard, "PrivacyButton", "Privacy Policy", 40, Color.white, DarkText,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -600f), new Vector2(560f, 100f));
            uiManager.removeAdsButton = MakeButton(settingsCard, "RemoveAdsButton", "Remove Ads: Kharido", 40, AccentTeal, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -720f), new Vector2(560f, 100f));
            uiManager.removeAdsLabel = uiManager.removeAdsButton.GetComponentInChildren<Text>();
            uiManager.settingsCloseButton = MakeButton(settingsCard, "SettingsCloseButton", "Band karo", 40, Color.white, DarkText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 100f));
            uiManager.settingsPanel = settingsPanel;

            // ---- Save the scene and register it in Build Settings. ----
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true)
            };
            AssetDatabase.SaveAssets();

            Debug.Log("[Teer Puzzle] MainScene v1.4 build ho gayi: 10 managers, HUD (undo nahi), level map + pagination bar + settings button, win/lose overlays, theme dropdown, splash + settings screens.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static T AddManager<T>(GameObject parent, string name) where T : MonoBehaviour
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObject(go, "Build Main Scene");
            return go.AddComponent<T>();
        }

        private static void SetFullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static GameObject MakePanel(GameObject parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static Text MakeText(GameObject parent, string name, string text, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = ArialFont;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.resizeTextForBestFit = false;
            return label;
        }

        private static Button MakeButton(GameObject parent, string name, string label, int fontSize,
            Color bgColor, Color textColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image image = go.AddComponent<Image>();
            image.color = bgColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            SetFullRect(textGo.GetComponent<RectTransform>());
            Text text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = ArialFont;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            return button;
        }
    }
}
