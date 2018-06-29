using System.Text.RegularExpressions;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public static class DeltaHelper
    {
        private const char FlairDeltaChar = '∆';

        public static int GetDeltaCount(string flairText)
        {
            if(string.IsNullOrEmpty(flairText))
            {
                return 0;
            }

            // Find any numbers in the flair
            string numberString = Regex.Match(flairText, @"\d+").Value;

            if (int.TryParse(numberString, out int deltaCount))
            {
                return deltaCount;
            }

            return 0;
        }

        public static string GetIncrementedFlairText(string flairText)
        {
            int newDeltaCount = GetDeltaCount(flairText) + 1;
            return $"{newDeltaCount}{FlairDeltaChar}";
        }

        public static string GetDecrementedFlairText(string flairText)
        {
            int newDeltaCount = GetDeltaCount(flairText) - 1;
            return $"{newDeltaCount}{FlairDeltaChar}";
        }
    }
}
