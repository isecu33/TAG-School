namespace PieceBook.CitySim.AI
{
    /// <summary>
    /// Top-level session state for the bombing loop (GD-02 §2):
    /// Explore (iso) → Painting (1st person) → Chase (top-down) → Outcome.
    /// </summary>
    public enum LoopPhase
    {
        Explore = 0,
        Painting = 1,
        Chase = 2,
        Caught = 3,
        Escaped = 4
    }
}
