namespace PieceBook.Core.Services
{
    /// <summary>
    /// Core haptics facade (ARQUITECTURA §3 "Haptics"). Light impacts on stroke start and on
    /// rewards. No-op in the editor and on platforms without a vibrator, so callers never guard.
    /// </summary>
    public interface IHaptics
    {
        void Light();
        void Success();
    }
}
