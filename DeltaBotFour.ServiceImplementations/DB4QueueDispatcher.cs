using Core.Foundation.Helpers;
using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaBotFour.ServiceImplementations
{
    public class DB4QueueDispatcher : IDB4QueueDispatcher
    {
        private readonly IDB4Queue _queue;
        private readonly ICommentProcessor _commentProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DB4QueueDispatcher(IDB4Queue queue, ICommentProcessor commentProcessor)
        {
            _queue = queue;
            _commentProcessor = commentProcessor;
        }

        public async void Start()
        {
            ConsoleHelper.WriteLine("DB4QueueDispatcher: Running...", ConsoleColor.Green);

            // Process messages from the queue
            // This will run as long as the application is running
            await Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    try
                    {
                        var message = _queue.Pop();

                        // If something was popped off the queue, dispatch
                        // it to the correct processor
                        if (message != null)
                        {
                            switch (message.Type)
                            {
                                case QueueMessageType.Comment:
                                    var comment = JsonConvert.DeserializeObject<DB4Comment>(message.Payload);
                                    _commentProcessor.Process(comment);
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unhandled enum value: {message.Type}");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        // Make sure no exceptions get thrown out of this loop
                        ConsoleHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
