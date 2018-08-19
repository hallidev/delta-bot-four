using System;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Shared.Interface;
using DeltaBotFour.Shared.Logging;
using Newtonsoft.Json;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DB4QueueDispatcher : IDB4QueueDispatcher
    {
        private readonly ILogger _logger;
        private readonly IDB4Queue _queue;
        private readonly IDB4Repository _repository;
        private readonly ICommentProcessor _commentProcessor;
        private readonly IPrivateMessageProcessor _privateMessageProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DB4QueueDispatcher(ILogger logger,
            IDB4Queue queue, 
            IDB4Repository repository,
            ICommentProcessor commentProcessor,
            IPrivateMessageProcessor privateMessageProcessor)
        {
            _logger = logger;
            _queue = queue;
            _repository = repository;
            _commentProcessor = commentProcessor;
            _privateMessageProcessor = privateMessageProcessor;
        }

        public async void Start()
        {
            _logger.Info("DB4QueueDispatcher: Running...");

            // Process messages from the queue
            // This will run as long as the application is running
            await Task.Factory.StartNew(() =>
            {
                while (true)
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
                                case QueueMessageType.Edit:

                                    var comment = JsonConvert.DeserializeObject<DB4Thing>(message.Payload);

                                    try
                                    {
                                        
                                        _commentProcessor.Process(comment);
                                    }
                                    finally
                                    {
                                        // If for whatever reason there's a problem during processing, we need to be
                                        // able to move on. Make sure this comment is set as processed

                                        // Mark as the last processed comment / edit so when DeltaBot starts up again, it has a place to start
                                        if (message.Type == QueueMessageType.Comment)
                                        {
                                            _repository.SetLastProcessedCommentId(comment.FullName);
                                        }
                                        else
                                        {
                                            _repository.SetLastProcessedEditId(comment.FullName);
                                        }
                                    }
                                    
                                    break;
                                case QueueMessageType.PrivateMessage:
                                    var privateMessage = JsonConvert.DeserializeObject<DB4Thing>(message.Payload);
                                    _privateMessageProcessor.Process(privateMessage);
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unhandled enum value: {message.Type}");
                            }

                            _logger.Info($"Queue remaining: ({_queue.GetPrimaryCount()} primary), ({_queue.GetNinjaEditCount()} ninja)");
                        }
                    }
                    catch(Exception ex)
                    {
                        // Make sure no exceptions get thrown out of this loop
                        _logger.Error(ex);
                    }

                    Thread.Sleep(100);
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
