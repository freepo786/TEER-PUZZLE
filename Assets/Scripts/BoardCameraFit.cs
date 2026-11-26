using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Fits the orthographic camera to the current board whenever a level
    /// loads, so small and large boards (4x4 up to 10x10) all fit on
    /// portrait phones without cropping. Listens to LevelManager.
    /// Added during project assembly (v1.1) — not part of the original
    /// script brief, because no script owned camera fitting.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoardCameraFit : MonoBehaviour
    {
        public LevelManager levelManager;
        public GridManager gridManager;
        public Transform boardRoot;

        // Extra space around the board, in world units (room for HUD bars).
        public float margin = 1.4f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (levelManager != null)
            {
                levelManager.OnLevelLoaded += FitToLevel;
            }
        }

        private void OnDestroy()
        {
            if (levelManager != null)
            {
                levelManager.OnLevelLoaded -= FitToLevel;
            }
        }

        private void FitToLevel(LevelData level)
        {
            if (cam == null || level == null)
            {
                return;
            }

            float aspect = cam.aspect;
            if (aspect <= 0f)
            {
                aspect = 9f / 16f;
            }

            float needed = Mathf.Max(level.height, level.width / aspect);
            cam.orthographicSize = needed * 0.5f + margin;

            Vector3 target = boardRoot != null ? boardRoot.position : Vector3.zero;
            cam.transform.position = new Vector3(target.x, target.y, -10f);
        }
    }
}
