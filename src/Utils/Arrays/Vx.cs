using Microsoft.Win32.SafeHandles;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Blossom.Utils.Arrays
{
    public class Vx
    {
        private Dictionary<object, float> Elements = new Dictionary<object, float>();

        public int Size { get => Elements.Count; }
        public object[] Keys { get => Elements.Keys.ToArray(); }
        public float[] Items { get => Elements.Values.ToArray(); }

        public float this[object key]
        {
            get => Elements[key];
            set
            {
                if (Elements.ContainsKey(key))
                    Elements[key] = value;
                else
                    Elements.Add(key, value);
            }
        }

        internal enum MathOperator
        {
            Add,
            Substract,
            Multiply,
            Divide,
            Power
        }

        internal Vx Math(Vx b, MathOperator op)
        {
            if (Size != b.Size) throw new Exception("Elements are not the same size");
            if (Keys[0].GetType() != b.Keys[0].GetType()) throw new Exception("Elements don't have matching keys");

            Vx result = new Vx();

            foreach (var key in Keys)
            {
                result[key] = op switch
                {
                    MathOperator.Add => Elements[key] + b[key],
                    MathOperator.Substract => Elements[key] - b[key],
                    MathOperator.Multiply => Elements[key] * b[key],
                    MathOperator.Divide => Elements[key] / b[key],
                    MathOperator.Power => (int)Elements[key] ^ (int)b[key],
                    _ => default,
                };
            }

            return result;
        }

        public static Vx operator +(Vx a, Vx b) => a.Math(b, MathOperator.Add);
        public static Vx operator -(Vx a, Vx b) => a.Math(b, MathOperator.Substract);
        public static Vx operator *(Vx a, Vx b) => a.Math(b, MathOperator.Multiply);
        public static Vx operator /(Vx a, Vx b) => a.Math(b, MathOperator.Divide);
        public static Vx operator ^(Vx a, Vx b) => a.Math(b, MathOperator.Power);
    }
}
