using System.Linq;
using System;

namespace Rux.Utils
{
    public static class Arr
    {
        /// <summary>
        /// Maps array to delegate for quick iteration
        /// </summary>
        /// <param name="arr">Source array</param>
        /// <param name="act">Action for each element</param>
        /// <typeparam name="T"></typeparam>
        public static void Map<T>(Array arr, Action<T> act)
        {
            foreach (T item in arr)
            {
                act((T)item);
            }
        }

        /// <summary>
        /// Maps array source to a new type of array
        /// </summary>
        /// <param name="arr">Source array</param>
        /// <param name="target">Target array</param>
        /// <typeparam name="T">New type</typeparam>
        public static void Map<T>(Array arr, out T[] target)
        {
            T[] tg = new T[arr.Length];

            for (int i = 0; i < arr.Length; i++)
            {
                tg[i] = (T)arr.GetValue(i);
            }

            target = tg;
        }
    }
}