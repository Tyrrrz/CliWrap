using System;

namespace CliWrap.Internal
{
    internal static class Guards
    {
        public static T GuardNotNull<T>(this T o, string argName = null) where T : class
        {
            if (o == null)
                throw new ArgumentNullException(argName);

            return o;
        }

        public static T GuardGreaterThan<T>(this T o, T value, string argName = null) where T : IComparable<T>
        {
            if (o.CompareTo(value) <= 0)
                throw new ArgumentException($"Should be greater than [{value}]", argName);

            return o;
        }

        public static T GuardLessThan<T>(this T o, T value, string argName = null) where T : IComparable<T>
        {
            if (o.CompareTo(value) >= 0)
                throw new ArgumentException($"Should be less than [{value}]", argName);

            return o;
        }
    }
}