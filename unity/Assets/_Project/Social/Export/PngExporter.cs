using System;
using System.IO;
using PieceBook.DrawingEngine;
using UnityEngine;

namespace PieceBook.Social.Export
{
    /// <summary>
    /// Exports the finished artwork to PNG (ARQUITECTURA §2 "export imagen"). The canvas is
    /// flattened via the pure §4.3 API, so this module never reaches into the engine internals.
    /// The CPU encode step (<see cref="EncodePng"/>) is what tests exercise; the GPU flatten is
    /// verified in-editor.
    /// </summary>
    public static class PngExporter
    {
        /// <summary>Encode a texture to PNG bytes. Deterministic for identical pixels.</summary>
        public static byte[] EncodePng(Texture2D tex)
        {
            if (tex == null) throw new ArgumentNullException(nameof(tex));
            return tex.EncodeToPNG();
        }

        /// <summary>Flatten the canvas (§4.3) and encode it to PNG bytes.</summary>
        public static byte[] ExportArtwork(IDrawingCanvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            var flat = canvas.Flatten();
            return EncodePng(flat);
        }

        /// <summary>Write PNG bytes to disk atomically (temp file + move).</summary>
        public static void WritePng(byte[] png, string absolutePath)
        {
            if (png == null) throw new ArgumentNullException(nameof(png));
            var dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var tmp = absolutePath + ".tmp";
            File.WriteAllBytes(tmp, png);
            if (File.Exists(absolutePath)) File.Delete(absolutePath);
            File.Move(tmp, absolutePath);
        }
    }
}
