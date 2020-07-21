﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Shared;
using DeltaBotFour.Shared.Interface;
using DeltaBotFour.Shared.Logging;
using Newtonsoft.Json;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DB4QueueDispatcher : IDB4QueueDispatcher
    {
        private readonly AutoRestartManager _autoRestartManager;
        private readonly ILogger _logger;
        private readonly IDB4Queue _queue;
        private readonly ICommentProcessor _commentProcessor;
        private readonly IPrivateMessageProcessor _privateMessageProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DB4QueueDispatcher(AutoRestartManager autoRestartManager,
            ILogger logger,
            IDB4Queue queue, 
            ICommentProcessor commentProcessor,
            IPrivateMessageProcessor privateMessageProcessor)
        {
            _autoRestartManager = autoRestartManager;
            _logger = logger;
            _queue = queue;
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
                            try
                            {
                                // Track in-flight requests. Bot will only restart when over restart threshold
                                // and there are no requests in flight
                                _autoRestartManager.AddInFlight();

                                switch (message.Type)
                                {
                                    case QueueMessageType.Comment:
                                    case QueueMessageType.Edit:
                                        var comment = JsonConvert.DeserializeObject<DB4Thing>(message.Payload);
                                        _commentProcessor.Process(comment);
                                        break;
                                    case QueueMessageType.PrivateMessage:
                                        var privateMessage = JsonConvert.DeserializeObject<DB4Thing>(message.Payload);
                                        _privateMessageProcessor.Process(privateMessage);
                                        break;
                                    default:
                                        throw new InvalidOperationException($"Unhandled enum value: {message.Type}");
                                }
                            }
                            finally
                            {
                                _autoRestartManager.RemoveInFlight();
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
