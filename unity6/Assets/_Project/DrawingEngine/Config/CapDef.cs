using UnityEngine;

namespace PieceBook.DrawingEngine.Config
{
    /// <summary>
    /// Spray cap catalog entry (ARQUITECTURA §9 CapDef + §4.1 spray parameters).
    /// Data-driven: design adds caps without touching code (§7 "ScriptableObject como catálogo").
    ///
    /// Field set matches §9 exactly:
    ///   {id, name, coneAngle, density, falloff, flowRate, overspray, unlockCost}
    /// Note: dripThreshold lives on <see cref="PaintDef"/> per §9 (drips are a property of the
    /// paint/material, not the cap). The brief's ENG-02 lists dripThreshold under the caps; it is
    /// reconciled here by keeping it on PaintDef — see INFORME-GO-NOGO.md.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Cap Definition", fileName = "Cap_New")]
    public sealed class CapDef : ScriptableObject
    {
        [Tooltip("Stable string id used by catalogs / recordings. Never a magic string in code (§7).")]
        public string id = "cap_new";

        [Tooltip("Human-readable name shown in UI.")]
        public string displayName = "New Cap";

        [Header("Spray geometry (§4.1)")]
        [Tooltip("Spray cone aperture in degrees. Skinny ≈ 4°, Soft ≈ 12°, Fat ≈ 25°.")]
        [Range(1f, 45f)] public float coneAngle = 12f;

        [Tooltip("Particles stamped per unit of path length. Higher = more opaque, heavier build-up.")]
        [Range(1f, 64f)] public float density = 18f;

        [Tooltip("Edge hardness [0..1]. 1 = crisp skinny line, 0 = fully feathered cloud.")]
        [Range(0f, 1f)] public float falloff = 0.6f;

        [Tooltip("Paint flow: per-stamp alpha contribution. Low values build up gradually (realistic).")]
        [Range(0.01f, 1f)] public float flowRate = 0.25f;

        [Tooltip("Amount of outer 'mist' particles sprayed around the core. Grows with nozzle distance.")]
        [Range(0f, 1f)] public float overspray = 0.25f;

        [Header("Economy (§9)")]
        [Tooltip("Cost to unlock in the meta-game. Not used by the Phase-0 spike.")]
        public int unlockCost = 0;

        /// <summary>Base core radius in normalized canvas units, derived from cone aperture.</summary>
        public float BaseRadius01 => Mathf.Lerp(0.004f, 0.05f, Mathf.InverseLerp(1f, 45f, coneAngle));
    }
}
