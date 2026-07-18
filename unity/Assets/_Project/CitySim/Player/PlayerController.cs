using UnityEngine;

namespace PieceBook.CitySim.Player
{
    /// <summary>
    /// Player avatar for the spike: a gray capsule moved with a CharacterController on the
    /// XZ plane, camera-relative (iso). Handles hiding inside containers (GD-02 §3). No art,
    /// no animation — just readable movement for the loop.
    ///
    /// Put on the "Ignore Raycast" layer so patrol vision rays never self-hit the player;
    /// detection is done by cone+range+LOS in <see cref="AI.VisionSensor"/>, not by ray-to-player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private CharacterController _cc;
        private Transform _camT;

        public bool IsHidden { get; private set; }
        public bool ControlEnabled = true;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            gameObject.layer = 2; // Ignore Raycast (built-in; no ProjectSettings change)
        }

        public void Init(Transform cameraTransform) => _camT = cameraTransform;

        /// <summary>Move the player. <paramref name="moveInput"/> is raw stick/WASD in [-1,1].</summary>
        public void Drive(Vector2 moveInput, float speed)
        {
            if (!ControlEnabled || IsHidden) { _cc.SimpleMove(Vector3.zero); return; }
            Vector3 dir = CameraRelative(moveInput);
            _cc.SimpleMove(dir * speed);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 12f * Time.deltaTime);
        }

        /// <summary>Enter/leave a hide. When hiding, tuck toward the container so the capsule
        /// reads as "inside" (GD-02 §3). Detection treats a hidden player as unseen.</summary>
        public void SetHidden(bool hidden, Vector3 hidePos)
        {
            IsHidden = hidden;
            if (hidden)
            {
                Vector3 p = hidePos;
                p.y = transform.position.y;
                transform.position = Vector3.Lerp(transform.position, p, 0.5f);
            }
        }

        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
        }

        private Vector3 CameraRelative(Vector2 m)
        {
            if (_camT == null) return new Vector3(m.x, 0f, m.y);
            Vector3 f = _camT.forward; f.y = 0f; f.Normalize();
            Vector3 r = _camT.right; r.y = 0f; r.Normalize();
            return r * m.x + f * m.y;
        }
    }
}
