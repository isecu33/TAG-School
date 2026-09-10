namespace PieceBook.ARModule
{
    /// <summary>
    /// Entry point for the optional AR module (ARQUITECTURA §3: se carga por Addressables solo si el
    /// dispositivo lo soporta). For now <see cref="CreateSession"/> returns the stub; when AR Foundation
    /// is installed, the loader resolves the real session (AR-00) and this factory returns it instead.
    /// Kept dependency-free so the assembly compiles without the AR/Addressables packages.
    /// </summary>
    public static class ArModule
    {
        /// <summary>Create an AR placement session. Returns a non-supported stub until AR is wired.</summary>
        public static IArPlacementSession CreateSession() => new StubArPlacementSession();
    }
}
