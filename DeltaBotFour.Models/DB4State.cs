using System;

namespace DeltaBotFour.Models
{
    public class DB4State
    {
        public Guid Id { get; set; }
        public DateTimeOffset LastActivityTimeUtcKey { get; set; }
        public string LastProcessedCommentIds { get; set; }
        public string LastProcessedEditIds { get; set; }
        public string IgnoreQuotedDeltaPMUserList { get; set; }
    }
}