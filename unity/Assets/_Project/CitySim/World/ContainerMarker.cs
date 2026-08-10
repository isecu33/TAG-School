using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// A container hideout (GD-02 §3). Carries a trigger volume the player can step into and
    /// hide in by holding. Pure gray cube — no art.
    /// </summary>
    public sealed class ContainerMarker : MonoBehaviour
    {
        public Transform HidePoint => transform;
    }
}
