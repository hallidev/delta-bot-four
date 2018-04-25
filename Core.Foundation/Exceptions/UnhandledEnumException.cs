using System;

namespace Core.Foundation.Exceptions
{
    public class UnhandledEnumException<T> : InvalidOperationException where T : IComparable
    {
        public UnhandledEnumException(T enumValue)
            : base(GetExceptionMessage(enumValue))
        {

        }

        private static string GetExceptionMessage(T enumValue)
        {
            return $"Unhandled {typeof(T)}: {enumValue.ToString()}";
        }
    }
}
