namespace DeltaBotFour.Models
{
    public class QueueMessage
    {
        public QueueMessageType Type { get; private set; }
        public string Payload { get; private set; }

        public QueueMessage(QueueMessageType type, string payload)
        {
            Type = type;
            Payload = payload;
        }
    }
}
