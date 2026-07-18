using UnityEngine;

namespace PieceBook.DrawingEngine.Config
{
    /// <summary>
    /// Paint / material catalog entry (ARQUITECTURA §9 PaintDef).
    ///   {id, name, opacity, glossiness, dripThreshold, colorRange}
    /// dripThreshold is the accumulated-paint level (per canvas cell) that triggers a
    /// procedural drip (ENG-04 / §4.1). Kept on the paint, not the cap, per §9.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Paint Definition", fileName = "Paint_New")]
    public sealed class PaintDef : ScriptableObject
    {
        public string id = "paint_new";
        public string displayName = "New Paint";

        [Header("Material (§9)")]
        [Tooltip("Max coverage a single stroke can reach [0..1].")]
        [Range(0f, 1f)] public float opacity = 1f;

        [Tooltip("Specular sheen. Not rendered by the Phase-0 spike; reserved for §4 shaders.")]
        [Range(0f, 1f)] public float glossiness = 0.2f;

        [Tooltip("Accumulated paint (0..1, per canvas cell) above which a drip is spawned. " +
                 "Lower = drips easily (wet paint / close nozzle). ENG-04.")]
        [Range(0.2f, 4f)] public float dripThreshold = 1.2f;

        [Header("Color range")]
        [Tooltip("Optional tint palette this paint ships with. Purely informational for the spike.")]
        public Color[] colorRange = new[] { Color.white };
    }
}
