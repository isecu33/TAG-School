using System;

namespace PieceBook.DrawingEngine
{
    /// <summary>
    /// Opaque handle to a canvas layer. A value type so passing it around never
    /// allocates and never leaks the internal layer list (ARQUITECTURA §4.1 "capas").
    /// </summary>
    [Serializable]
    public readonly struct LayerId : IEquatable<LayerId>
    {
        public static readonly LayerId None = new LayerId(-1);

        public readonly int Value;
        public LayerId(int value) { Value = value; }

        public bool IsValid => Value >= 0;

        public bool Equals(LayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is LayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Layer#{Value}";

        public static bool operator ==(LayerId a, LayerId b) => a.Value == b.Value;
        public static bool operator !=(LayerId a, LayerId b) => a.Value != b.Value;
    }
}
