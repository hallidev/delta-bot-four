namespace DeltaBotFour.Models
{
    public class QueueMessage
    {
        public QueueMessageType Type { get; private set; }
        public object Payload { get; private set; }

        public QueueMessage(QueueMessageType type, object payload)
        {
            Type = type;
            Payload = payload;
        }
    }
}
