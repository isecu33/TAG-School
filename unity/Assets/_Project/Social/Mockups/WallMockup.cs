using UnityEngine;

namespace PieceBook.Social.Mockups
{
    /// <summary>
    /// A photographic wall backdrop the finished artwork is "plasmada" onto, no AR required
    /// (ARQUITECTURA §6 "Fallback sin RA: mockups fotográficos … con perspectiva"). Works on any
    /// device. The four corners place the artwork rectangle into the photo with perspective; real
    /// photos (persianas, trenes, halls) replace the placeholder background later.
    /// </summary>
    [CreateAssetMenu(menuName = "PieceBook/Wall Mockup", fileName = "Mockup_New")]
    public sealed class WallMockup : ScriptableObject
    {
        public string id = "mockup_new";
        public string displayName = "New Wall";
        public Texture2D background;

        [Header("Artwork placement (normalized background space [0,1])")]
        public Vector2 cornerBL = new Vector2(0.2f, 0.2f);
        public Vector2 cornerBR = new Vector2(0.8f, 0.2f);
        public Vector2 cornerTL = new Vector2(0.2f, 0.8f);
        public Vector2 cornerTR = new Vector2(0.8f, 0.8f);
    }
}
