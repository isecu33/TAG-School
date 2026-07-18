using System;
using UnityEngine;

namespace PieceBook.CitySim.Data
{
    /// <summary>
    /// Per-agent alert tuning (ARQUITECTURA §9 PatrolDef.alertProfile). Drives the
    /// Calma→Sospecha→Persecución FSM (§7) and the GD-02 §3 alert table thresholds.
    /// All times in seconds. Kept as data so patrols are editable without code.
    /// </summary>
    [Serializable]
    public struct AlertProfile
    {
        [Tooltip("Suspicion accrued per second while the player is inside the cone with LOS.")]
        [Min(0f)] public float suspicionBuildRate;

        [Tooltip("Suspicion decay per second when the player is not seen.")]
        [Min(0f)] public float suspicionDecayRate;

        [Tooltip("Suspicion (0..1) at which Calma → Sospecha.")]
        [Range(0f, 1f)] public float suspectThreshold;

        [Tooltip("Suspicion (0..1) at which Sospecha → Persecución (or instant on seeing you paint).")]
        [Range(0f, 1f)] public float chaseThreshold;

        [Tooltip("Seconds without LOS before Persecución downgrades to a search, then Calma.")]
        [Min(0f)] public float loseSightTime;

        [Tooltip("Chase speed = patrol speed × this multiplier (GD-02: slightly faster than you).")]
        [Min(1f)] public float chaseSpeedMultiplier;

        public static AlertProfile Default => new AlertProfile
        {
            suspicionBuildRate = 1.6f,
            suspicionDecayRate = 0.5f,
            suspectThreshold = 0.35f,
            chaseThreshold = 1f,
            loseSightTime = 3.5f,
            chaseSpeedMultiplier = 1.12f
        };
    }
}
