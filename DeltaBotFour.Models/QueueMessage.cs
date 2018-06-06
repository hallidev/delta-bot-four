using System;

namespace DeltaBotFour.Models
{
    public class QueueMessage
    {
        public QueueMessageType Type { get; }
        public string Payload { get; set; }
        public DateTime CreatedUtc { get; }

        public QueueMessage(QueueMessageType type, string payload, DateTime createdUtc)
        {
            Type = type;
            Payload = payload;
            CreatedUtc = createdUtc;
        }
    }
}
