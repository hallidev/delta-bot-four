namespace DeltaBotFour.Infrastructure.Implementation
{
    public static class DeltaHelper
    {
        private const char FlairDeltaChar = '∆';

        public static string GetFlairText(int deltaCount)
        {
            return $"{deltaCount}{FlairDeltaChar}";
        }
    }
}
