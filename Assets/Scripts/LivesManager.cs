using System;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// Lives (hearts) for the current level. 3 per level by default.
    /// A blocked tap costs one life; at 0 lives the level is lost.
    /// </summary>
    public class LivesManager : MonoBehaviour
    {
        public int maxLives = 3;

        /// <summary>Raised whenever the life count changes.</summary>
        public event Action<int> OnLivesChanged;

        private int lives;

        public int Lives
        {
            get { return lives; }
        }

        private void Awake()
        {
            lives = maxLives;
        }

        /// <summary>Restores full lives (called when a level starts).</summary>
        public void ResetLives()
        {
            lives = maxLives;
            if (OnLivesChanged != null)
            {
                OnLivesChanged(lives);
            }
        }

        /// <summary>
        /// Adds one life (rewarded ad se extra dil), maxLives tak.
        /// Pehle se full hon to bhi true return karta hai.
        /// </summary>
        public bool AddLife()
        {
            lives = Mathf.Min(maxLives, lives + 1);
            if (OnLivesChanged != null)
            {
                OnLivesChanged(lives);
            }
            return true;
        }

        /// <summary>
        /// Removes one life. Returns true when lives have reached 0
        /// (level lost), false otherwise.
        /// </summary>
        public bool LoseLife()
        {
            if (lives > 0)
            {
                lives -= 1;
            }

            if (OnLivesChanged != null)
            {
                OnLivesChanged(lives);
            }

            return lives <= 0;
        }
    }
}
