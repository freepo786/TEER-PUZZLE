using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Owns the board: builds arrows from level data, converts cells to
    /// world positions, and tracks which cells are occupied so exit
    /// checks are cheap. The board is centered on boardRoot.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // Size of one cell in world units.
        public float cellSize = 1f;

        // The board's center transform. If left empty, this object is used.
        public Transform boardRoot;

        // Optional: used to tint new arrows with the active theme.
        public ThemeManager themeManager;

        /// <summary>Raised when an arrow has fully slid off the board.</summary>
        public event Action<Arrow> OnArrowExited;

        /// <summary>Raised when a tap was blocked (path not clear).</summary>
        public event Action<Arrow> OnArrowBlocked;

        private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        private List<Arrow> arrows = new List<Arrow>();
        private int currentWidth;
        private int currentHeight;

        // Arrow sprites loaded from Resources/Art (white base, tinted at runtime).
        private Sprite spriteUp;
        private Sprite spriteDown;
        private Sprite spriteLeft;
        private Sprite spriteRight;

        /// <summary>All arrows currently on the board (read-only view).</summary>
        public IReadOnlyList<Arrow> Arrows
        {
            get { return arrows; }
        }

        public int Width
        {
            get { return currentWidth; }
        }

        public int Height
        {
            get { return currentHeight; }
        }

        private void Awake()
        {
            if (boardRoot == null)
            {
                boardRoot = transform;
            }

            spriteUp = Resources.Load<Sprite>("Art/arrow_up");
            spriteDown = Resources.Load<Sprite>("Art/arrow_down");
            spriteLeft = Resources.Load<Sprite>("Art/arrow_left");
            spriteRight = Resources.Load<Sprite>("Art/arrow_right");
        }

        /// <summary>
        /// Clears the board and spawns every arrow of the given level.
        /// </summary>
        public void BuildLevel(LevelData level)
        {
            ClearBoard();

            currentWidth = level.width;
            currentHeight = level.height;

            if (level.arrows != null)
            {
                foreach (var arrowData in level.arrows)
                {
                    SpawnArrow(arrowData);
                }
            }
        }

        /// <summary>
        /// Spawns one arrow on the board and marks its cells occupied.
        /// Used by BuildLevel to spawn every arrow of a level.
        /// Returns null if the data is invalid or the cells are taken.
        /// </summary>
        private Arrow SpawnArrow(ArrowData data)
        {
            if (data == null || data.nodalPoints == null || data.nodalPoints.Count == 0)
            {
                Debug.LogWarning("[GridManager] Tried to spawn an arrow with no cells.");
                return null;
            }

            var go = new GameObject("Arrow_" + data.color);
            go.transform.SetParent(boardRoot, false);
            go.transform.localPosition = Vector3.zero;

            var arrow = go.AddComponent<Arrow>();
            arrow.Init(data, this);

            // Tint with the active theme color for this arrow.
            arrow.SetTint(GetArrowColor(data.color));

            // Mark the arrow's cells as occupied.
            foreach (var cell in data.nodalPoints)
            {
                occupied.Add(new Vector2Int(cell.column, cell.row));
            }

            arrows.Add(arrow);
            return arrow;
        }

        /// <summary>
        /// True when every cell from the arrow's head to the board edge,
        /// in the arrow's exit direction, is empty.
        /// </summary>
        public bool IsExitClear(Arrow arrow)
        {
            if (arrow == null)
            {
                return false;
            }

            Vector2Int cell = arrow.headCell + arrow.direction;
            while (cell.x >= 0 && cell.x < currentWidth &&
                   cell.y >= 0 && cell.y < currentHeight)
            {
                if (occupied.Contains(cell))
                {
                    return false;
                }
                cell += arrow.direction;
            }
            return true;
        }

        /// <summary>
        /// First arrow whose exit path is currently clear, or null.
        /// Used by the hint system.
        /// </summary>
        public Arrow FindFreeArrow()
        {
            foreach (var arrow in arrows)
            {
                if (arrow != null && IsExitClear(arrow))
                {
                    return arrow;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes an arrow from the board: frees its cells and destroys
        /// its GameObject. Called by Arrow after its slide animation.
        /// </summary>
        public void RemoveArrow(Arrow arrow)
        {
            if (arrow == null)
            {
                return;
            }

            if (arrow.data != null && arrow.data.nodalPoints != null)
            {
                foreach (var cell in arrow.data.nodalPoints)
                {
                    occupied.Remove(new Vector2Int(cell.column, cell.row));
                }
            }

            arrows.Remove(arrow);

            // Fire the event BEFORE destroying the GameObject: listeners
            // (GameManager) still need to read arrow.data, and a destroyed
            // Unity object compares equal to null.
            if (OnArrowExited != null)
            {
                OnArrowExited(arrow);
            }

            Destroy(arrow.gameObject);
        }

        /// <summary>Called by Arrow when a tap was blocked, so the game can lose a life.</summary>
        public void NotifyBlocked(Arrow arrow)
        {
            if (OnArrowBlocked != null)
            {
                OnArrowBlocked(arrow);
            }
        }

        /// <summary>World position of the center of a cell.</summary>
        public Vector3 CellToWorld(int col, int row)
        {
            float x = (col - (currentWidth - 1) * 0.5f) * cellSize;
            float y = ((currentHeight - 1) * 0.5f - row) * cellSize;
            Vector3 origin = boardRoot != null ? boardRoot.position : Vector3.zero;
            return origin + new Vector3(x, y, 0f);
        }

        /// <summary>True when the cell is inside the board bounds.</summary>
        public bool IsInsideBoard(int col, int row)
        {
            return col >= 0 && col < currentWidth && row >= 0 && row < currentHeight;
        }

        /// <summary>True when the cell is occupied by any arrow.</summary>
        public bool IsOccupied(int col, int row)
        {
            return occupied.Contains(new Vector2Int(col, row));
        }

        /// <summary>
        /// Re-tints every arrow on the board with the given theme palette
        /// (used when the player switches theme mid-level).
        /// </summary>
        public void RetintAllArrows(ThemePalette palette)
        {
            if (palette == null || palette.arrowColors == null)
            {
                return;
            }

            foreach (var arrow in arrows)
            {
                if (arrow != null && arrow.data != null)
                {
                    arrow.SetTint(GetArrowColor(arrow.data.color, palette));
                }
            }
        }

        /// <summary>Sprite for an exit direction (white base, tinted at runtime).</summary>
        public Sprite GetSpriteForDirection(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
            {
                return spriteUp;
            }
            if (direction == Vector2Int.down)
            {
                return spriteDown;
            }
            if (direction == Vector2Int.left)
            {
                return spriteLeft;
            }
            if (direction == Vector2Int.right)
            {
                return spriteRight;
            }
            return spriteUp;
        }

        private Color GetArrowColor(int colorIndex)
        {
            ThemePalette palette = themeManager != null ? themeManager.GetCurrentPalette() : null;
            return GetArrowColor(colorIndex, palette);
        }

        private Color GetArrowColor(int colorIndex, ThemePalette palette)
        {
            if (palette != null && palette.arrowColors != null && palette.arrowColors.Length > 0)
            {
                int index = Mathf.Clamp(colorIndex, 0, palette.arrowColors.Length - 1);
                return palette.arrowColors[index];
            }
            // Fallback teal when no theme is available yet.
            return new Color(0.05f, 0.58f, 0.53f, 1f);
        }

        private void ClearBoard()
        {
            foreach (var arrow in arrows)
            {
                if (arrow != null)
                {
                    Destroy(arrow.gameObject);
                }
            }
            arrows.Clear();
            occupied.Clear();
        }
    }
}
