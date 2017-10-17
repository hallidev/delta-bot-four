namespace DeltaBotFour.ServiceImplementations
{
    public static class DeltaHelper
    {
        private const char FLAIR_DELTA_CHAR = '∆';

        public static int GetDeltaCount(string flairText)
        {
            if(string.IsNullOrEmpty(flairText) || !flairText.EndsWith(FLAIR_DELTA_CHAR))
            {
                return 0;
            }

            return int.Parse(flairText.TrimEnd(FLAIR_DELTA_CHAR));
        }

        public static string GetIncrementedFlairText(string flairText)
        {
            int newDeltaCount = GetDeltaCount(flairText) + 1;
            return $"{newDeltaCount}{FLAIR_DELTA_CHAR}";
        }

        public static string GetDecrementedFlairText(string flairText)
        {
            int newDeltaCount = GetDeltaCount(flairText) - 1;
            return $"{newDeltaCount}{FLAIR_DELTA_CHAR}";
        }
    }
}
