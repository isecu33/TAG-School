using UnityEngine;

namespace PieceBook.CitySim.World
{
    /// <summary>
    /// Orthographic isometric camera (ARQUITECTURA §5-GD02: "todo el sandbox en isométrico
    /// 2D"). Follows a target along a fixed iso angle. Because it is orthographic, the view
    /// distance doesn't change apparent size — only <see cref="OrthoSize"/> zooms.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsoCameraRig : MonoBehaviour
    {
        [SerializeField] private float distance = 22f;
        [SerializeField] private float follow = 10f;
        public float OrthoSize = 7.5f;

        private Camera _cam;
        private Transform _target;

        public Camera Camera => _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = OrthoSize;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 120f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.09f, 0.09f, 0.11f);
            transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        }

        public void Follow(Transform target)
        {
            _target = target;
            if (target != null)
                transform.position = target.position - transform.forward * distance;
        }

        private void LateUpdate()
        {
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, OrthoSize, 1f - Mathf.Exp(-12f * Time.deltaTime));
            if (_target == null) return;
            Vector3 desired = _target.position - transform.forward * distance;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-follow * Time.deltaTime));
        }
    }
}
