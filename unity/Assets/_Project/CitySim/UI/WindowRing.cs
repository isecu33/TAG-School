using PieceBook.CitySim.World;
using UnityEngine;

namespace PieceBook.CitySim.UI
{
    /// <summary>
    /// The painting-window ring drawn in the world next to a surface (GD-02 §2: "anillo de
    /// tiempo visible junto a la superficie"). A LineRenderer arc whose swept angle = fraction
    /// of safe time left, tinted green→yellow→red by urgency. No art.
    /// </summary>
    public sealed class WindowRing : MonoBehaviour
    {
        private const int MaxSegments = 48;
        private const float Radius = 0.7f;

        private LineRenderer _lr;

        private void Awake()
        {
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.useWorldSpace = false;
            _lr.loop = false;
            _lr.widthMultiplier = 0.09f;
            _lr.numCapVertices = 2;
            _lr.textureMode = LineTextureMode.Stretch;
            _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lr.material = MaterialFactory.UnlitTransparent(Color.white);
            _lr.positionCount = 0;
            Hide();
        }

        public void Show(Vector3 worldPos)
        {
            transform.position = worldPos;
            transform.rotation = Quaternion.identity; // horizontal ring (readable from iso)
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetFraction(float fraction, Color color)
        {
            fraction = Mathf.Clamp01(fraction);
            int n = Mathf.Max(2, Mathf.CeilToInt(MaxSegments * fraction));
            _lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)MaxSegments) * Mathf.PI * 2f;
                _lr.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, 0f, Mathf.Sin(a) * Radius));
            }
            _lr.startColor = color;
            _lr.endColor = color;
            MaterialFactory.Tint(_lr.material, color);
        }
    }
}
