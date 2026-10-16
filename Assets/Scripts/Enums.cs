using UnityEngine;

namespace BusJam
{
    public enum GameColor { Red, Green, Blue, Yellow, Purple, Orange }
    public enum Direction { Up, Right, Down, Left }

    public static class Palette
    {
        public const int ColorCount = 6;
        static readonly Color[] colors =
        {
            new Color(0.93f, 0.27f, 0.27f), new Color(0.30f, 0.78f, 0.35f), new Color(0.25f, 0.50f, 0.95f),
            new Color(0.98f, 0.82f, 0.20f), new Color(0.65f, 0.38f, 0.88f), new Color(0.98f, 0.55f, 0.16f)
        };
        public static Color Get(GameColor c) => colors[(int)c];
        public static Color[] All => colors;

        public static Vector2Int ToVec(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Right: return new Vector2Int(1, 0);
                case Direction.Down: return new Vector2Int(0, -1);
                default: return new Vector2Int(-1, 0);
            }
        }

        /// <summary>Goc yaw quanh Y (mat phang XZ): Up = +Z.</summary>
        public static float ToAngle(this Direction d)
        {
            switch (d)
            {
                case Direction.Up: return 0f;
                case Direction.Right: return 90f;
                case Direction.Down: return 180f;
                default: return 270f;
            }
        }

        /// <summary>Toa do luoi (x,y) -> the gioi 3D (x,0,z).</summary>
        public static Vector3 ToWorld(this Vector2Int v) => new Vector3(v.x, 0f, v.y);
    }
}
