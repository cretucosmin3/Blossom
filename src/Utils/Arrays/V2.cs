namespace Blossom.Utils.Arrays
{
    public class V2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public V2(float x, float y)
        {
            X = x; Y = y;
        }

        public V2(int x, float y)
        {
            X = x; Y = y;
        }

        // Operators
        #region V2 to V2
        public static V2 operator -(V2 a, V2 b)
        {
            return new V2(a.X - b.X, a.Y - b.Y);
        }

        public static V2 operator +(V2 a, V2 b)
        {
            return new V2(a.X + b.X, a.Y + b.Y);
        }

        public static V2 operator *(V2 a, V2 b)
        {
            return new V2(a.X * b.X, a.Y * b.Y);
        }

        public static V2 operator /(V2 a, V2 b)
        {
            return new V2(a.X / b.X, a.Y / b.Y);
        }
        #endregion

        #region V2 to float
        public static V2 operator -(V2 a, float b)
        {
            return new V2(a.X - b, a.Y - b);
        }
        public static V2 operator +(V2 a, float b)
        {
            return new V2(a.X + b, a.Y + b);
        }
        public static V2 operator *(V2 a, float b)
        {
            return new V2(a.X * b, a.Y * b);
        }
        public static V2 operator /(V2 a, float b)
        {
            return new V2(a.X / b, a.Y / b);
        }
        #endregion

        /// <summary>
        /// Converts V2 to System.Numberics.Vector2
        /// </summary>
        /// <returns>System.Numerics.Vector2</returns>
        public System.Numerics.Vector2 ToNumerics()
        {
            return new System.Numerics.Vector2(X, Y);
        }
    }
}
