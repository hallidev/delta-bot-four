namespace DeltaBotFour.Infrastructure.Implementation
{
    internal class PrivateMessageParseResult
    {
        public bool IsDirectChat { get; init; }
        public string Command { get; init; }
        public string Argument { get; init; }
    }
}