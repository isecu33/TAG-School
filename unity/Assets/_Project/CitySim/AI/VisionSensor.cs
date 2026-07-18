using PieceBook.CitySim.Data;
using UnityEngine;

namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// Cone-of-vision test in 2D (XZ): range + half-angle + line-of-sight (GD-02 §2 "conos de
    /// visión estilo Mark of the Ninja"). LOS is a physics raycast that only hits buildings /
    /// containers — the player and patrol live on the Ignore Raycast layer, so rays never
    /// self-hit. A hidden player (inside a container) is never seen.
    /// </summary>
    public static class VisionSensor
    {
        public static bool CanSee(Vector3 eye, Vector3 forward, in VisionCone cone,
                                  Vector3 target, bool targetHidden, out float distance)
        {
            distance = 0f;
            if (targetHidden) return false;

            Vector3 to = target - eye; to.y = 0f;
            distance = to.magnitude;
            if (distance < 0.01f || distance > cone.range) return false;

            Vector3 fwd = forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) return false;
            fwd.Normalize();

            float ang = Vector3.Angle(fwd, to / distance);
            if (ang > cone.angle * 0.5f) return false;

            // Line of sight: a building or container between eye and target blocks it.
            Vector3 dir = (target - eye);
            float d = dir.magnitude;
            dir /= d;
            if (Physics.Raycast(eye, dir, d - 0.15f)) return false;

            return true;
        }
    }
}
