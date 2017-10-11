namespace DeltaBotFour.Models
{
    public class QueueMessage<TPayload>
    {
        public TPayload Payload { get; private set; }

        public QueueMessage(TPayload payload)
        {
            Payload = payload;
        }
    }
}
