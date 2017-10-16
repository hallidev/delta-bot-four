namespace DeltaBotFour.ServiceImplementations
{
    public static class DeltaHelper
    {
        public static int GetDeltaCount(string flairText)
        {
            if(string.IsNullOrEmpty(flairText) || !flairText.EndsWith('Δ'))
            {
                return 0;
            }

            return int.Parse(flairText.TrimEnd('Δ'));
        }
    }
}
