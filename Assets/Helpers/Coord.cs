using UnityEngine;

namespace Helpers
{
    [System.Serializable]
    public class Coord
    {
        public float x;
        public float y;
        public Coord(float x, float y) { this.x = x; this.y = y; }

        public static implicit operator Coord((float x, float y) t) { return new Coord(t.x, t.y); }
        public static implicit operator (float, float)(Coord c) { return (c.x, c.y); }
        public static implicit operator Vector2(Coord c) { return new(c.x, c.y); }
        public static implicit operator Vector3(Coord c) { return new(c.x, c.y); }

        public static bool operator !=(Coord a, Coord b) => a.x != b.x || a.y != b.y;
        public static bool operator ==(Coord a, Coord b) => !(a != b);
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            var coord = obj as Coord;
            if (coord is not null)
                return this == coord;
            (float x, float y)? tuple = obj as (float x, float y)?;
            if (tuple is not null)
                return this == tuple;

            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() + y.GetHashCode();
        }

        public static Coord operator +(Coord a, Coord b) => new Coord(a.x + b.x, a.y + b.y);
        public static Coord operator +(Coord a, (float x, float y) b) => new Coord(a.x + b.x, a.y + b.y);
        public static Coord operator +((float x, float y) a, Coord b) => new Coord(a.x + b.x, a.y + b.y);
        public static Coord operator +(Coord a, float b) => new Coord(a.x + b, a.y + b);


        public static Coord operator -(Coord a, Coord b) => new Coord(a.x - b.x, a.y - b.y);
        public static Coord operator -(Coord a, (float x, float y) b) => new Coord(a.x - b.x, a.y - b.y);
        public static Coord operator -((float x, float y) a, Coord b) => new Coord(a.x - b.x, a.y - b.y);
        public static Coord operator -(Coord a, float b) => new Coord(a.x - b, a.y - b);
        public static Coord operator -(Coord a) => new Coord(-a.x, -a.y);

        public static Coord operator /(Coord a, int b) => new Coord(a.x / b, a.y / b);
        public static Coord operator /(Coord a, float b) => new Coord(a.x / b, a.y / b);
        public static Coord Clamp(Coord a, Coord min, Coord max) => new Coord(Mathf.Clamp(a.x, min.x, max.x), Mathf.Clamp(a.y, min.y, max.y));
        // public bool FitIn(Coord other) => x >= 0 && x <= other.x && y >= 0 && y <= other.y;
        public bool FitIn(Coord start, Coord end) => x >= start.x && x <= end.x && y >= start.y && y <= end.y;

        public static Coord Random(Coord max) => Random((0, 0), max);
        public static Coord Random(Coord min, Coord max) => new(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}
