using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TeerPuzzle
{
    /// <summary>One full color theme: UI colors plus the arrow palette.</summary>
    [Serializable]
    public class ThemePalette
    {
        public string name;
        public Color background;
        public Color board;
        public Color panel;
        public Color accent;
        public Color text;
        public Color[] arrowColors;

        public ThemePalette(string name, string background, string board, string panel,
            string accent, string text, string[] arrowHexColors)
        {
            this.name = name;
            this.background = Parse(background);
            this.board = Parse(board);
            this.panel = Parse(panel);
            this.accent = Parse(accent);
            this.text = Parse(text);

            arrowColors = new Color[arrowHexColors.Length];
            for (int i = 0; i < arrowHexColors.Length; i++)
            {
                arrowColors[i] = Parse(arrowHexColors[i]);
            }
        }

        private static Color Parse(string hex)
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(hex, out color))
            {
                return color;
            }
            Debug.LogWarning("[ThemePalette] Bad color value: " + hex);
            return Color.magenta;
        }
    }

    /// <summary>
    /// Holds the 6 built-in themes and applies the selected one: camera
    /// background, board and panel colors, and every arrow's tint.
    /// The choice is saved through SaveManager.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        // Assigned in the scene: the board background image.
        public Image boardImage;

        // Assigned in the scene: the panel behind the HUD / buttons.
        public Image panelImage;

        // Assigned in the scene (or found automatically by GameManager).
        public GridManager gridManager;

        public List<ThemePalette> themes = new List<ThemePalette>();

        public int CurrentThemeIndex { get; private set; }

        /// <summary>Raised after a theme has been applied.</summary>
        public event Action<int> OnThemeChanged;

        private void Awake()
        {
            BuildThemes();
            CurrentThemeIndex = 0;
        }

        /// <summary>The palette of the currently active theme.</summary>
        public ThemePalette GetCurrentPalette()
        {
            if (themes == null || themes.Count == 0)
            {
                return null;
            }
            return themes[Mathf.Clamp(CurrentThemeIndex, 0, themes.Count - 1)];
        }

        /// <summary>Applies theme i (clamped to the available range).</summary>
        public void ApplyTheme(int i)
        {
            if (themes == null || themes.Count == 0)
            {
                return;
            }

            CurrentThemeIndex = Mathf.Clamp(i, 0, themes.Count - 1);
            ThemePalette palette = themes[CurrentThemeIndex];

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = palette.background;
            }

            if (boardImage != null)
            {
                boardImage.color = palette.board;
            }

            if (panelImage != null)
            {
                panelImage.color = palette.panel;
            }

            if (gridManager != null)
            {
                gridManager.RetintAllArrows(palette);
            }

            if (OnThemeChanged != null)
            {
                OnThemeChanged(CurrentThemeIndex);
            }
        }

        /// <summary>Applies the theme with this name, if one exists.</summary>
        public void ApplyThemeByName(string n)
        {
            if (string.IsNullOrEmpty(n))
            {
                return;
            }

            for (int i = 0; i < themes.Count; i++)
            {
                if (string.Equals(themes[i].name, n, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyTheme(i);
                    return;
                }
            }

            Debug.LogWarning("[ThemeManager] No theme named '" + n + "'.");
        }

        private void BuildThemes()
        {
            themes = new List<ThemePalette>();

            themes.Add(new ThemePalette(
                "Classic",
                "#F2F0EA", "#FFFFFF", "#FFFFFF", "#0F766E", "#1F2937",
                new[] { "#0D9488", "#F59E0B", "#EF4444", "#3B82F6", "#22C55E", "#8B5CF6", "#EC4899", "#EAB308" }));

            themes.Add(new ThemePalette(
                "Black & White",
                "#111111", "#1F1F1F", "#1F1F1F", "#FFFFFF", "#F5F5F5",
                new[] { "#FFFFFF", "#E5E7EB", "#D4D4D4", "#A3A3A3", "#9CA3AF", "#6B7280", "#4B5563", "#374151" }));

            themes.Add(new ThemePalette(
                "Neon",
                "#0B0B1E", "#13132B", "#13132B", "#39FF14", "#F0F0FF",
                new[] { "#39FF14", "#FF2E88", "#00E5FF", "#FFE600", "#FF6B00", "#B967FF", "#2E7FFF", "#FF3131" }));

            themes.Add(new ThemePalette(
                "Sunset",
                "#FFF3E0", "#FFE0B2", "#FFE0B2", "#E65100", "#4E342E",
                new[] { "#FF7043", "#FFA726", "#FFCA28", "#EF5350", "#AB47BC", "#EC407A", "#FF8A65", "#FFD54F" }));

            themes.Add(new ThemePalette(
                "Ocean",
                "#E0F2F1", "#B2DFDB", "#B2DFDB", "#00695C", "#004D40",
                new[] { "#00897B", "#00ACC1", "#1E88E5", "#3949AB", "#5E35B1", "#00838F", "#0097A7", "#1565C0" }));

            themes.Add(new ThemePalette(
                "Candy",
                "#FCE4EC", "#F8BBD0", "#F8BBD0", "#C2185B", "#4A148C",
                new[] { "#EC407A", "#AB47BC", "#7E57C2", "#5C6BC0", "#42A5F5", "#26A69A", "#FFCA28", "#FF7043" }));
        }
    }
}
