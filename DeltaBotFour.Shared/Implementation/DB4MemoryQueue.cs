using System;
using System.Collections;
using DeltaBotFour.Models;
using DeltaBotFour.Shared.Interface;
using Newtonsoft.Json;

namespace DeltaBotFour.Shared.Implementation
{
    public class DB4MemoryQueue : IDB4Queue
    {
        // The goal here is to catch "ninja edits" which don't show up as edits until 3 minutes
        // after the initial comment
        private const int NinjaEditReprocessSeconds = 180;

        // Main queue for comments / edits and private messages
        private readonly Queue _primaryQueue = new Queue();
        // Second queue to re-process comments a second time after 3 minutes
        private readonly Queue _ninjaEditQueue = new Queue();
        // Count how many messages have be popped
        private int _popCount;

        public int GetPrimaryCount()
        {
            return _primaryQueue.Count;
        }

        public int GetNinjaEditCount()
        {
            return _ninjaEditQueue.Count;
        }

        public void Push(QueueMessage message)
        {
            _primaryQueue.Enqueue(JsonConvert.SerializeObject(message));
        }

        public QueueMessage Pop()
        {
            if (_primaryQueue.Count > 0 || _ninjaEditQueue.Count > 0)
            {
                _popCount++;
            }
            else
            {
                return null;
            }

            // Every other time a message is popped, give the ninja
            // queue priority over the primary queue
            if (_popCount % 2 == 0)
            {
                return getFromNinjaQueue() ?? getFromPrimaryQueue();
            }

            return getFromPrimaryQueue() ?? getFromNinjaQueue();
        }

        private QueueMessage getFromPrimaryQueue()
        {
            if (_primaryQueue.Count > 0)
            {
                // Get thing to process (comment / edit or private message)
                string messageString = _primaryQueue.Dequeue().ToString();
                var queueMessage = JsonConvert.DeserializeObject<QueueMessage>(messageString);

                // Comments need to be re-processed for ninja edits
                if (queueMessage.Type == QueueMessageType.Comment)
                {
                    _ninjaEditQueue.Enqueue(messageString);
                }

                return queueMessage;
            }

            return null;
        }

        private QueueMessage getFromNinjaQueue()
        {
            if (_ninjaEditQueue.Count > 0)
            {
                var oldestOnNinjaQueue = JsonConvert.DeserializeObject<QueueMessage>(_ninjaEditQueue.Peek().ToString());
                int ageInSeconds = (int)(DateTime.UtcNow - oldestOnNinjaQueue.CreatedUtc).TotalSeconds;

                // If older than 3 minutes, re-process
                if (ageInSeconds > NinjaEditReprocessSeconds)
                {
                    var ninjaQueuMessage = JsonConvert.DeserializeObject<QueueMessage>(_ninjaEditQueue.Dequeue().ToString());
                    var db4Thing = JsonConvert.DeserializeObject<DB4Thing>(ninjaQueuMessage.Payload);
                    db4Thing.NeedsRefresh = true;
                    ninjaQueuMessage.Payload = JsonConvert.SerializeObject(db4Thing);
                    return ninjaQueuMessage;
                }
            }

            return null;
        }
    }
}
