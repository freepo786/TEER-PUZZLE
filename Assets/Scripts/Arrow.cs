using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// One arrow on the board. Built from ArrowData: a path of cells from
    /// tail to head. Tapping it slides it off the board when its exit
    /// path is clear, otherwise it bumps and costs a life (handled by
    /// GameManager via GridManager.OnArrowBlocked).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Arrow : MonoBehaviour
    {
        public ArrowData data;

        // Last cell of the path (the head).
        public Vector2Int headCell;

        // Exit direction: from the second-last cell towards the head.
        public Vector2Int direction;

        private GridManager grid;
        private Transform cellsContainer;
        private List<SpriteRenderer> cellRenderers = new List<SpriteRenderer>();
        private Color tint = Color.white;
        private bool isSliding;
        private bool isHighlighted;
        private Coroutine highlightCoroutine;

        /// <summary>True while the arrow is sliding off the board.</summary>
        public bool IsSliding
        {
            get { return isSliding; }
        }

        /// <summary>
        /// Builds the arrow's visuals from its path data. Called once by
        /// GridManager right after the GameObject is created.
        /// </summary>
        public void Init(ArrowData d, GridManager gridManager)
        {
            data = d;
            grid = gridManager;

            var points = d.nodalPoints;
            int count = points.Count;

            // Head is the last cell; direction comes from the last step.
            headCell = new Vector2Int(points[count - 1].column, points[count - 1].row);
            if (count >= 2)
            {
                Vector2Int secondLast = new Vector2Int(points[count - 2].column, points[count - 2].row);
                direction = headCell - secondLast;
            }
            else
            {
                direction = Vector2Int.up;
            }

            // Center of the arrow, used as the pivot for the highlight pulse.
            Vector3 center = Vector3.zero;
            foreach (var cell in points)
            {
                center += grid.CellToWorld(cell.column, cell.row);
            }
            center /= count;

            // Container holds all cell sprites; pulsing scales this only.
            var containerGo = new GameObject("Cells");
            cellsContainer = containerGo.transform;
            cellsContainer.SetParent(transform, false);
            cellsContainer.localPosition = center - grid.boardRoot.position;

            Sprite sprite = grid.GetSpriteForDirection(direction);
            float angle = DirectionAngle(direction);

            for (int i = 0; i < count; i++)
            {
                var cellGo = new GameObject("Cell_" + i);
                cellGo.transform.SetParent(cellsContainer, false);
                cellGo.transform.localPosition = grid.CellToWorld(points[i].column, points[i].row) - center;
                cellGo.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

                // The head cell is full size; body cells are slightly smaller
                // so the arrow reads as one shape with a clear head.
                bool isHead = i == count - 1;
                float scale = isHead ? 1f : 0.92f;
                cellGo.transform.localScale = new Vector3(scale, scale, 1f);

                var renderer = cellGo.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
                cellRenderers.Add(renderer);
            }

            ApplyTint();

            // Tap target covering the whole arrow.
            var collider = GetComponent<BoxCollider2D>();
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var cell in points)
            {
                Vector3 world = grid.CellToWorld(cell.column, cell.row);
                min = Vector2.Min(min, (Vector2)world);
                max = Vector2.Max(max, (Vector2)world);
            }
            Vector2 center2D = (min + max) * 0.5f;
            Vector2 size = (max - min) + Vector2.one * grid.cellSize;
            Vector3 rootWorld = grid.boardRoot != null ? grid.boardRoot.position : Vector3.zero;
            collider.offset = center2D - (Vector2)rootWorld;
            collider.size = size;
        }

        /// <summary>
        /// Tap handler: slide off the board when the exit is clear,
        /// otherwise bump and report the blocked tap.
        /// </summary>
        public void TryExit()
        {
            if (isSliding || grid == null)
            {
                return;
            }

            if (grid.IsExitClear(this))
            {
                Highlight(false);
                StartCoroutine(SlideOut());
            }
            else
            {
                Bump();
                grid.NotifyBlocked(this);
            }
        }

        private void OnMouseDown()
        {
            TryExit();
        }

        /// <summary>
        /// Slides the arrow out of the board in its exit direction
        /// (0.25s, ease-out), then removes it via the grid.
        /// </summary>
        private IEnumerator SlideOut()
        {
            isSliding = true;

            // Distance from the head to the far board edge, plus a margin
            // so the arrow fully leaves the screen area.
            Vector3 start = transform.position;
            float cellsToEdge = 0f;
            Vector2Int cell = headCell + direction;
            while (grid.IsInsideBoard(cell.x, cell.y))
            {
                cellsToEdge += 1f;
                cell += direction;
            }
            float distance = (cellsToEdge + 2f) * grid.cellSize;
            Vector3 target = start + new Vector3(direction.x, direction.y, 0f) * distance;

            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out: fast start, gentle stop.
                float eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(start, target, eased);
                yield return null;
            }
            transform.position = target;

            grid.RemoveArrow(this);
        }

        /// <summary>Short shake (0.08s) for a blocked tap.</summary>
        public void Bump()
        {
            if (!isSliding)
            {
                StartCoroutine(BumpRoutine());
            }
        }

        private IEnumerator BumpRoutine()
        {
            Vector3 basePosition = transform.position;
            float duration = 0.08f;
            float elapsed = 0f;
            float strength = grid.cellSize * 0.06f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin((elapsed / duration) * Mathf.PI * 2f) * strength;
                transform.position = basePosition + new Vector3(direction.x, direction.y, 0f) * offset;
                yield return null;
            }
            transform.position = basePosition;
        }

        /// <summary>
        /// Hint pulse: gently scales the arrow up and down until turned off.
        /// </summary>
        public void Highlight(bool on)
        {
            if (on == isHighlighted)
            {
                return;
            }

            isHighlighted = on;

            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }

            if (cellsContainer != null)
            {
                cellsContainer.localScale = Vector3.one;
            }

            if (on)
            {
                highlightCoroutine = StartCoroutine(HighlightRoutine());
            }
        }

        private IEnumerator HighlightRoutine()
        {
            while (isHighlighted && cellsContainer != null)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
                cellsContainer.localScale = new Vector3(pulse, pulse, 1f);
                yield return null;
            }

            if (cellsContainer != null)
            {
                cellsContainer.localScale = Vector3.one;
            }
        }

        /// <summary>Tints every cell sprite with the theme color.</summary>
        public void SetTint(Color c)
        {
            tint = c;
            ApplyTint();
        }

        private void ApplyTint()
        {
            foreach (var renderer in cellRenderers)
            {
                if (renderer != null)
                {
                    renderer.color = tint;
                }
            }
        }

        private static float DirectionAngle(Vector2Int dir)
        {
            if (dir == Vector2Int.up)
            {
                return 0f;
            }
            if (dir == Vector2Int.right)
            {
                return -90f;
            }
            if (dir == Vector2Int.down)
            {
                return 180f;
            }
            if (dir == Vector2Int.left)
            {
                return 90f;
            }
            return 0f;
        }
    }
}
