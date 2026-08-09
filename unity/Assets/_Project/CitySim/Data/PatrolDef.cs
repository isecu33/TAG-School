using UnityEngine;

namespace PieceBook.CitySim.Data
{
    /// <summary>
    /// Patrol catalog entry (ARQUITECTURA §9):
    ///   PatrolDef {id, waypoints[], speed, visionCone {angle, range}, alertProfile}
    /// Data-driven so designers tune a patrol's route/speed/awareness without code
    /// (§7 "ScriptableObject como catálogo"). Waypoints are node indices into the
    /// zone's street graph — patrols navigate the GRAPH, never a navmesh (§7).
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/CitySim/Patrol Definition", fileName = "Patrol_New")]
    public sealed class PatrolDef : ScriptableObject
    {
        public string id = "patrol_new";

        [Tooltip("Ordered street-graph node indices the patrol loops through.")]
        public int[] waypoints = new int[0];

        [Tooltip("Walk speed in world units/second while in Calma.")]
        [Min(0.1f)] public float speed = 2.2f;

        [Tooltip("Seconds paused at each waypoint (a beat that widens the painting window).")]
        [Min(0f)] public float waypointPause = 0.6f;

        public VisionCone visionCone = new VisionCone(70f, 7f);

        public AlertProfile alertProfile = AlertProfile.Default;
    }
}
