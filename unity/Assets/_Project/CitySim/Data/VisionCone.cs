using System;
using UnityEngine;

namespace PieceBook.CitySim.Data
{
    /// <summary>
    /// Vision cone parameters (ARQUITECTURA §9 PatrolDef.visionCone {angle, range}).
    /// Angle is the FULL aperture in degrees; range is in world units.
    /// </summary>
    [Serializable]
    public struct VisionCone
    {
        [Range(10f, 170f)] public float angle;
        [Min(0.5f)] public float range;

        public VisionCone(float angle, float range)
        {
            this.angle = angle;
            this.range = range;
        }

        public float HalfAngleRad => angle * 0.5f * Mathf.Deg2Rad;
    }
}
