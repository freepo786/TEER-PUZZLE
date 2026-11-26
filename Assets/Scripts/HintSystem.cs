using System;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Hints for the current level. 3 per level by default. Using a hint
    /// finds an arrow whose exit is currently clear and pulses it.
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        public int hintsPerLevel = 3;

        // Assigned in the scene (or found automatically by GameManager).
        public GridManager gridManager;

        /// <summary>Raised whenever the remaining hint count changes.</summary>
        public event Action<int> OnHintsChanged;

        private int hintsLeft;
        private Arrow currentHintedArrow;

        public int HintsLeft
        {
            get { return hintsLeft; }
        }

        private void Awake()
        {
            hintsLeft = hintsPerLevel;
        }

        /// <summary>Restores the full hint count (called when a level starts).</summary>
        public void ResetHints()
        {
            ClearHint();
            hintsLeft = hintsPerLevel;
            if (OnHintsChanged != null)
            {
                OnHintsChanged(hintsLeft);
            }
        }

        /// <summary>
        /// Adds one hint (rewarded ad se), hintsPerLevel tak.
        /// </summary>
        public void AddHint()
        {
            hintsLeft = Mathf.Min(hintsPerLevel, hintsLeft + 1);
            if (OnHintsChanged != null)
            {
                OnHintsChanged(hintsLeft);
            }
        }

        /// <summary>
        /// Uses one hint: highlights an arrow that can exit right now.
        /// Does nothing when no hints remain or no arrow is free.
        /// </summary>
        public void UseHint()
        {
            if (hintsLeft <= 0)
            {
                return;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("[HintSystem] gridManager is not assigned.");
                return;
            }

            Arrow freeArrow = gridManager.FindFreeArrow();
            if (freeArrow == null)
            {
                return;
            }

            ClearHint();
            currentHintedArrow = freeArrow;
            currentHintedArrow.Highlight(true);

            hintsLeft -= 1;
            if (OnHintsChanged != null)
            {
                OnHintsChanged(hintsLeft);
            }
        }

        /// <summary>Stops the current hint highlight, if any.</summary>
        public void ClearHint()
        {
            if (currentHintedArrow != null)
            {
                currentHintedArrow.Highlight(false);
                currentHintedArrow = null;
            }
        }
    }
}
