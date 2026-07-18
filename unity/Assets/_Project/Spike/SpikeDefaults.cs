using PieceBook.DrawingEngine.Config;
using UnityEngine;

namespace PieceBook.Spike
{
    /// <summary>
    /// Canonical values for the three spike caps (skinny 4°, soft 12°, fat 25°) and the
    /// default paint. Single source of truth so the runtime fallback and the editor asset
    /// generator (SpikeSetup) stay identical.
    /// </summary>
    public static class SpikeDefaults
    {
        public static CapDef MakeSkinny()
        {
            var c = ScriptableObject.CreateInstance<CapDef>();
            c.name = "Cap_Skinny";
            c.id = "cap_skinny";
            c.displayName = "Skinny (4°)";
            c.coneAngle = 4f;
            c.density = 26f;
            c.falloff = 0.9f;
            c.flowRate = 0.22f;
            c.overspray = 0.06f;
            c.unlockCost = 0;
            return c;
        }

        public static CapDef MakeSoft()
        {
            var c = ScriptableObject.CreateInstance<CapDef>();
            c.name = "Cap_Soft";
            c.id = "cap_soft";
            c.displayName = "Soft (12°)";
            c.coneAngle = 12f;
            c.density = 18f;
            c.falloff = 0.55f;
            c.flowRate = 0.26f;
            c.overspray = 0.22f;
            c.unlockCost = 25;
            return c;
        }

        public static CapDef MakeFat()
        {
            var c = ScriptableObject.CreateInstance<CapDef>();
            c.name = "Cap_Fat";
            c.id = "cap_fat";
            c.displayName = "Fat (25°)";
            c.coneAngle = 25f;
            c.density = 12f;
            c.falloff = 0.3f;
            c.flowRate = 0.32f;
            c.overspray = 0.5f;
            c.unlockCost = 50;
            return c;
        }

        public static PaintDef MakePaint()
        {
            var p = ScriptableObject.CreateInstance<PaintDef>();
            p.name = "Paint_Default";
            p.id = "paint_default";
            p.displayName = "Default Aerosol";
            p.opacity = 1f;
            p.glossiness = 0.2f;
            p.dripThreshold = 1.2f;
            p.colorRange = new[] { Color.white, Color.black, new Color(1f, 0.2f, 0.2f) };
            return p;
        }

        public static CapDef[] MakeAllCaps() => new[] { MakeSkinny(), MakeSoft(), MakeFat() };
    }
}
