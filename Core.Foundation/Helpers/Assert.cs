namespace Core.Foundation.Helpers
{
    public static class Assert
    {
        public static void That(bool condition)
        {
            That(condition, string.Empty);
        }

        public static void That(bool condition, string failMessage)
        {
            if (!condition)
            {
                throw new AssertionException(failMessage);
            }
        }
    }
}
