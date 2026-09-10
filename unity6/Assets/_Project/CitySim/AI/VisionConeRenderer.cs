using PieceBook.CitySim.Data;
using PieceBook.CitySim.World;
using UnityEngine;

namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// Draws the patrol's vision cone as a flat fan on the ground, clipped by occluders via
    /// per-sample raycasts (so it visibly hugs building corners) and tinted by alert state:
    /// white (Calma) → yellow (Sospecha) → red (Persecución). Makes the FSM legible (§ brief:
    /// "cono de visión visible"). Zero art — a runtime mesh.
    /// </summary>
    public sealed class VisionConeRenderer : MonoBehaviour
    {
        private const int Segments = 28;

        private Mesh _mesh;
        private Material _mat;
        private Vector3[] _verts;
        private int[] _tris;

        private void Awake()
        {
            _mesh = new Mesh { name = "VisionCone" };
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = _mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _mat = MaterialFactory.UnlitTransparent(new Color(1f, 1f, 1f, 0.22f));
            mr.sharedMaterial = _mat;

            _verts = new Vector3[Segments + 2];
            _tris = new int[Segments * 3];
            for (int i = 0; i < Segments; i++)
            {
                _tris[i * 3] = 0;
                _tris[i * 3 + 1] = i + 1;
                _tris[i * 3 + 2] = i + 2;
            }
        }

        public void SetColor(Color c)
        {
            c.a = 0.22f;
            MaterialFactory.Tint(_mat, c);
        }

        /// <summary>Rebuild the fan for this frame. eye/forward in world; cone from PatrolDef.</summary>
        public void Redraw(Vector3 eye, Vector3 forward, in VisionCone cone)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 basePos = new Vector3(eye.x, 0.08f, eye.z); // above road strips to avoid z-fighting
            Vector3 rayOrigin = new Vector3(eye.x, 0.3f, eye.z);
            _verts[0] = basePos;

            float half = cone.angle * 0.5f;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                float ang = Mathf.Lerp(-half, half, t);
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * forward;
                float dist = cone.range;
                if (Physics.Raycast(rayOrigin, dir, out RaycastHit hit, cone.range))
                    dist = hit.distance;
                _verts[i + 1] = basePos + dir * dist;
            }

            _mesh.Clear();
            _mesh.vertices = _verts;
            _mesh.triangles = _tris;
            _mesh.RecalculateBounds();
        }
    }
}
