using Microsoft.Win32.SafeHandles;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Kara.Utils.Arrays
{
	public class Vx
	{
		private Dictionary<object, float> elements = new Dictionary<object, float>();

		public int Size { get => elements.Count; }
		public object[] Keys { get => elements.Keys.ToArray(); }

		public float this[object key]
		{
			get => elements[key];
			set
			{
				if (elements.ContainsKey(key))
					elements[key] = value;
				else
					elements.Add(key, value);
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
			if (Keys != b.Keys) throw new Exception("Elements don't have matching keys");

			Vx result = new Vx();

			foreach (var key in Keys)
			{
				result[key] = op switch
				{
					MathOperator.Add => elements[key] + b[key],
					MathOperator.Substract => elements[key] - b[key],
					MathOperator.Multiply => elements[key] * b[key],
					MathOperator.Divide => elements[key] / b[key],
					MathOperator.Power => (int)elements[key] ^ (int)b[key],
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
