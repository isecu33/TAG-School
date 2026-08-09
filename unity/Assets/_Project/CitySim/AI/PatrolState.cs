namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// Per-agent alert states (ARQUITECTURA §7 FSM, GD-02 §3 alert table).
    /// Calma → patrols its route · Sospecha → investigates a hunch · Persecución → chases.
    /// </summary>
    public enum PatrolState
    {
        Calm = 0,       // Calma
        Suspicious = 1, // Sospecha
        Chase = 2       // Persecución
    }
}
