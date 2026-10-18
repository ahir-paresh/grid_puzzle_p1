using Solo.MOST_IN_ONE;
using UnityEngine;

namespace GridPuzzle.Haptics
{
    /// <summary>
    /// Tactile feedback coordinator powered by the Mobile Haptic Feedback asset.
    /// Bridges mobile hardware vibration motors (CoreHaptics on iOS, VibrationEffect on Android)
    /// to discrete gameplay actions (swipes, merges, combo surges, ice cracking, tactical power-ups).
    /// </summary>
    public class PuzzleHapticManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField] private float swipeCooldown = 0.08f;
        [SerializeField] private float buttonCooldown = 0.05f;

        public bool HapticsEnabled
        {
            get => hapticsEnabled && MOST_HapticFeedback.HapticsEnabled;
            set
            {
                hapticsEnabled = value;
                MOST_HapticFeedback.HapticsEnabled = value;
            }
        }

        private void Awake()
        {
            // Sync with global haptic toggle
            MOST_HapticFeedback.HapticsEnabled = hapticsEnabled;
        }

        /// <summary>
        /// Subtle, crisp tactile pulse when player performs a valid swipe.
        /// </summary>
        public void TriggerSwipe()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.LightImpact, swipeCooldown);
        }

        /// <summary>
        /// Tiered tactile impact scaled to the numerical value of the merged tile.
        /// </summary>
        public void TriggerMerge(int tileValue)
        {
            if (!HapticsEnabled) return;

            if (tileValue >= 2048)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
            }
            else if (tileValue >= 512)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
            }
            else if (tileValue >= 64)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
            }
            else
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.LightImpact);
            }
        }

        /// <summary>
        /// Physical feedback when cracking or shattering frozen ice obstacles.
        /// </summary>
        public void TriggerIceDamage(bool shattered)
        {
            if (!HapticsEnabled) return;

            if (shattered)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
            }
            else
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.RigidImpact);
            }
        }

        /// <summary>
        /// Escalating tactile surges when chaining consecutive move combos.
        /// </summary>
        public void TriggerCombo(int comboCount)
        {
            if (!HapticsEnabled) return;

            if (comboCount >= 4)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
            }
            else if (comboCount >= 2)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
            }
            else
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.LightImpact);
            }
        }

        /// <summary>
        /// Explosive heavy pulse when bomb detonates on a tile or obstacle.
        /// </summary>
        public void TriggerBomb()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
        }

        /// <summary>
        /// Precise, mechanical UI tick when selecting a tile or targeting mode.
        /// </summary>
        public void TriggerSelection()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        }

        /// <summary>
        /// Satisfying medium tactile click when two tiles swap positions.
        /// </summary>
        public void TriggerSwap()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
        }

        /// <summary>
        /// Mechanical UI button click pulse.
        /// </summary>
        public void TriggerButtonClick()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.Selection, buttonCooldown);
        }

        /// <summary>
        /// Celebratory success haptic cadence when winning the challenge.
        /// </summary>
        public void TriggerWin()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
        }

        /// <summary>
        /// Warning / failure vibration when out of steps or game over.
        /// </summary>
        public void TriggerGameOver()
        {
            if (!HapticsEnabled) return;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
        }
    }
}
