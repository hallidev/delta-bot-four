namespace Core.Foundation.Extensions
{
    public static class StringExtensions
    {
        public static string Ellipsis(this string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }
}
